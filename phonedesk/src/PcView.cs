// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneDesk {
 // Log sink is injected by the app so this file stays testable on its own.
 static class PcViewLog {
  internal static Action<string> Write=delegate{};
 }
 // Shared parsing/conversion helpers. Kept free of UI and network dependencies for tests.
 static class PcViewProtocol {
  internal const int Port=8796; // local verification port
  internal static string NewToken() {
   byte[] bytes=new byte[8];
   using(var rng=new RNGCryptoServiceProvider())rng.GetBytes(bytes);
   var sb=new StringBuilder(16);
   foreach(byte b in bytes)sb.Append(b.ToString("x2",CultureInfo.InvariantCulture));
   return sb.ToString();
  }
  internal static bool TokenMatches(string expected,string supplied) {
   if(expected==null||supplied==null||expected.Length!=supplied.Length)return false;
   int difference=0;
   for(int i=0;i<expected.Length;i++)difference|=expected[i]^supplied[i];
   return difference==0;
  }
  internal static string QueryValue(string query,string name) {
   if(string.IsNullOrEmpty(query))return null;
   foreach(string part in query.TrimStart('?').Split('&')) {
    int index=part.IndexOf('=');
    if(index<0)continue;
    if(string.Equals(part.Substring(0,index),name,StringComparison.OrdinalIgnoreCase))return Uri.UnescapeDataString(part.Substring(index+1));
   }
   return null;
  }
  internal static bool OriginAllowed(string origin) {
   if(string.IsNullOrEmpty(origin))return true;
   return string.Equals(origin,"http://localhost:"+Port,StringComparison.OrdinalIgnoreCase)
       || string.Equals(origin,"http://127.0.0.1:"+Port,StringComparison.OrdinalIgnoreCase);
  }
  // Messages are a short comma-separated line so no JSON dependency is needed.
  // touch:<action>,<id>,<u>,<v>  move:<dx>,<dy>  key:<base64 utf8>  special:<code>  wheel:<notches>  click:<button>
  internal static bool TryParse(string line,out PcViewAction action) {
   action=null;
   if(string.IsNullOrEmpty(line)||line.Length>8192)return false;
   int colon=line.IndexOf(':');
   if(colon<1)return false;
   string kind=line.Substring(0,colon),body=line.Substring(colon+1);
   string[] fields=body.Split(',');
   double u,v;int number;long id;
   if(kind=="touch") {
    if(fields.Length!=4)return false;
    if(!int.TryParse(fields[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out number)||number<0||number>2)return false;
    if(!long.TryParse(fields[1],NumberStyles.Integer,CultureInfo.InvariantCulture,out id)||id<0||id>100)return false;
    if(!double.TryParse(fields[2],NumberStyles.Float,CultureInfo.InvariantCulture,out u)||u<0||u>1)return false;
    if(!double.TryParse(fields[3],NumberStyles.Float,CultureInfo.InvariantCulture,out v)||v<0||v>1)return false;
    action=new PcViewAction{PointerAction=number,PointerId=id,U=u,V=v};return true;
   }
   if(kind=="move") {
    if(fields.Length!=2)return false;
    int dy;
    if(!int.TryParse(fields[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out number)||number<-512||number>512)return false;
    if(!int.TryParse(fields[1],NumberStyles.Integer,CultureInfo.InvariantCulture,out dy)||dy<-512||dy>512)return false;
    action=new PcViewAction{MoveX=number,MoveY=dy};return true;
   }
   if(kind=="key") {
    if(fields.Length!=1||fields[0].Length==0||fields[0].Length>4096)return false;
    byte[] bytes;
    try{bytes=Convert.FromBase64String(fields[0]);}catch(FormatException){return false;}
    if(bytes.Length==0||bytes.Length>1024)return false;
    string text;
    try{text=new UTF8Encoding(false,true).GetString(bytes);}catch(DecoderFallbackException){return false;}
    if(text.Length==0||text.Length>32)return false;
    action=new PcViewAction{Text=text};return true;
   }
   if(kind=="special") {
    if(fields.Length!=1||!int.TryParse(fields[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out number))return false;
    if(!PcViewInject.SpecialKeys.ContainsKey(number))return false;
    action=new PcViewAction{Special=number};return true;
   }
   if(kind=="wheel") {
    if(fields.Length!=1||!int.TryParse(fields[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out number)||number<-10||number>10)return false;
    action=new PcViewAction{Wheel=number};return true;
   }
   if(kind=="click") {
    if(fields.Length!=1||!int.TryParse(fields[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out number)||number<1||number>2)return false;
    action=new PcViewAction{Click=number};return true;
   }
   return false;
  }
  internal static Point Map(double u,double v,int screenWidth,int screenHeight) {
   if(screenWidth<1||screenHeight<1)return new Point(0,0);
   return new Point(
    Math.Max(0,Math.Min(screenWidth-1,(int)Math.Round(u*(screenWidth-1)))),
    Math.Max(0,Math.Min(screenHeight-1,(int)Math.Round(v*(screenHeight-1)))));
  }
 }
 sealed class PcViewAction {
  internal int PointerAction=-1,Special,Wheel,Click,MoveX,MoveY;
  internal long PointerId;
  internal double U,V;
  internal string Text;
 }
 sealed class PcViewAdbResult {
  internal int Code;
  internal string Output,Error;
 }

 // Injects mouse and Unicode keyboard input into the active Windows desktop.
 static class PcViewInject {
  [StructLayout(LayoutKind.Sequential)] struct INPUT {internal uint type;internal INPUTUNION data;}
  [StructLayout(LayoutKind.Explicit)] struct INPUTUNION {
   [FieldOffset(0)]internal MOUSEINPUT mouse;
   [FieldOffset(0)]internal KEYBDINPUT key;
   [FieldOffset(0)]internal HARDWAREINPUT hardware;
  }
  [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT {internal int dx,dy;internal uint mouseData,flags,time;internal IntPtr extra;}
  [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT {internal ushort vk,scan;internal uint flags,time;internal IntPtr extra;}
  [StructLayout(LayoutKind.Sequential)] struct HARDWAREINPUT {internal uint msg;internal ushort low,high;}
  const uint INPUT_MOUSE=0,INPUT_KEYBOARD=1;
  const uint MOUSEEVENTF_MOVE=0x0001,MOUSEEVENTF_ABSOLUTE=0x8000,MOUSEEVENTF_WHEEL=0x0800;
  const uint MOUSEEVENTF_LEFTDOWN=0x0002,MOUSEEVENTF_LEFTUP=0x0004,MOUSEEVENTF_RIGHTDOWN=0x0008,MOUSEEVENTF_RIGHTUP=0x0010;
  const uint KEYEVENTF_KEYUP=0x0002,KEYEVENTF_UNICODE=0x0004;
  [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,INPUT[] inputs,int size);
  internal static int ScreenWidth{get{return GetSystemMetrics(0);}}
  internal static int ScreenHeight{get{return GetSystemMetrics(1);}}
  [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
  [DllImport("user32.dll")] static extern bool GetCursorPos(out Point point);
  [StructLayout(LayoutKind.Sequential)] struct CURSORINFO {
   internal int cbSize;internal int flags;internal IntPtr hCursor;internal POINT ptScreenPos;
  }
  [StructLayout(LayoutKind.Sequential)] struct POINT {internal int X;internal int Y;}
  [DllImport("user32.dll")] static extern bool GetCursorInfo(ref CURSORINFO info);
  [DllImport("user32.dll")] static extern bool DrawIconEx(IntPtr hdc,int x,int y,IntPtr icon,int cx,int cy,uint step,IntPtr flicker,uint flags);
  const uint DI_NORMAL=0x0003;
  [DllImport("user32.dll",SetLastError=true)] static extern bool SystemParametersInfo(uint action,uint param,int[] values,uint flags);
  const uint SPI_GETMOUSE=0x0003,SPI_SETMOUSE=0x0004;
  const uint SPIF_UPDATEINIFILE=0x0001,SPIF_SENDCHANGE=0x0002;
  static int[] savedMouse;
  internal static void DisableAcceleration(){
   if(savedMouse!=null)return;
   var current=new int[3];
   if(!SystemParametersInfo(SPI_GETMOUSE,0,current,0))return;
   savedMouse=current;
   SystemParametersInfo(SPI_SETMOUSE,0,new int[]{0,0,0},SPIF_UPDATEINIFILE|SPIF_SENDCHANGE);
  }
  internal static void RestoreAcceleration(){
   if(savedMouse==null)return;
   SystemParametersInfo(SPI_SETMOUSE,0,savedMouse,SPIF_UPDATEINIFILE|SPIF_SENDCHANGE);
   savedMouse=null;
  }
  internal static readonly Dictionary<int,ushort> SpecialKeys=new Dictionary<int,ushort>{
   {8,0x08},{9,0x09},{13,0x0D},{27,0x1B},{37,0x25},{38,0x26},{39,0x27},{40,0x28},{46,0x2E}
  };
  static void Send(params INPUT[] inputs){SendInput((uint)inputs.Length,inputs,Marshal.SizeOf(typeof(INPUT)));}
  static INPUT Mouse(int dx,int dy,uint data,uint flags){
   var input=new INPUT();input.type=INPUT_MOUSE;
   input.data.mouse=new MOUSEINPUT{dx=dx,dy=dy,mouseData=data,flags=flags};return input;
  }
  static INPUT Key(ushort vk,ushort scan,uint flags){
   var input=new INPUT();input.type=INPUT_KEYBOARD;
   input.data.key=new KEYBDINPUT{vk=vk,scan=scan,flags=flags};return input;
  }
  internal static void Move(double u,double v){
   int width=GetSystemMetrics(0),height=GetSystemMetrics(1);
   var point=PcViewProtocol.Map(u,v,width,height);
   int x=(int)((long)point.X*65535/Math.Max(1,width-1));
   int y=(int)((long)point.Y*65535/Math.Max(1,height-1));
   Send(Mouse(x,y,0,MOUSEEVENTF_MOVE|MOUSEEVENTF_ABSOLUTE));
  }
  internal static void MoveBy(int dx,int dy){
   Send(Mouse(dx,dy,0,MOUSEEVENTF_MOVE));
  }
  internal static void DrawCursor(Graphics graphics,double scale){
   var info=new CURSORINFO();info.cbSize=Marshal.SizeOf(typeof(CURSORINFO));
   if(!GetCursorInfo(ref info)||info.hCursor==IntPtr.Zero||(info.flags&1)==0)return;
   IntPtr hdc=graphics.GetHdc();
   try{DrawIconEx(hdc,(int)Math.Round(info.ptScreenPos.X*scale),(int)Math.Round(info.ptScreenPos.Y*scale),info.hCursor,0,0,0,IntPtr.Zero,DI_NORMAL);}
   finally{graphics.ReleaseHdc(hdc);}
  }
  internal static void Click(int button){
   uint down=button==2?MOUSEEVENTF_RIGHTDOWN:MOUSEEVENTF_LEFTDOWN;
   uint up=button==2?MOUSEEVENTF_RIGHTUP:MOUSEEVENTF_LEFTUP;
   Send(Mouse(0,0,0,down),Mouse(0,0,0,up));
  }
  internal static void Wheel(int notches){Send(Mouse(0,0,unchecked((uint)(notches*120)),MOUSEEVENTF_WHEEL));}
  internal static void Special(int code){
   ushort vk;if(!SpecialKeys.TryGetValue(code,out vk))return;
   Send(Key(vk,0,0),Key(vk,0,KEYEVENTF_KEYUP));
  }
  internal static void Text(string text){
   foreach(char ch in text){
    Send(Key(0,ch,KEYEVENTF_UNICODE),Key(0,ch,KEYEVENTF_UNICODE|KEYEVENTF_KEYUP));
   }
  }
 }

 // Serves a downscaled JPEG view of the PC desktop over USB (adb reverse) and applies
 // touch/keyboard input received on a WebSocket. Binds to loopback only.
 sealed class PcViewServer : IDisposable {
  readonly string token,adbPath,serial;
  readonly HttpListener listener=new HttpListener();
  readonly CancellationTokenSource stop=new CancellationTokenSource();
  readonly object sync=new object();
  HttpListenerWebSocketContext socket;
  Task acceptTask;
  bool running,reversed;
  internal string Url{get{return "http://localhost:"+PcViewProtocol.Port+"/?t="+token;}}
  internal bool Running{get{lock(sync)return running;}}
  internal PcViewServer(string adbPath,string serial){
   this.token=PcViewProtocol.NewToken();this.adbPath=adbPath;this.serial=serial;
  }
  internal string Start(){return Start(true);}
  internal string Start(bool reverseAdb){
   lock(sync){if(running)throw new InvalidOperationException("already running");}
   string prefix="http://127.0.0.1:"+PcViewProtocol.Port+"/";
   listener.Prefixes.Add(prefix);
   try{
    listener.Start();
    PcViewInject.DisableAcceleration();
    if(reverseAdb){
     var adb=Adb("reverse tcp:"+PcViewProtocol.Port+" tcp:"+PcViewProtocol.Port);
     if(adb.Code!=0)throw new IOException(adb.Error.Trim());
     reversed=true;
    }
    running=true;
    acceptTask=Task.Run((Action)AcceptLoop);
    return Url;
   }catch{
    if(reversed)Adb("reverse --remove tcp:"+PcViewProtocol.Port);
    reversed=false;listener.Stop();throw;
   }
  }
  PcViewAdbResult Adb(string args){
   using(var process=new Process()){
    process.StartInfo=new ProcessStartInfo(adbPath,"-s "+DeviceParser.Quote(serial)+" "+args){
     UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
    process.Start();
    string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();
    if(!process.WaitForExit(6000)){try{process.Kill();}catch{}throw new TimeoutException("ADB応答なし");}
    return new PcViewAdbResult{Code=process.ExitCode,Output=output,Error=error};
   }
  }
  bool Authorized(HttpListenerRequest request){
   return PcViewProtocol.TokenMatches(token,request.QueryString["t"])
       && PcViewProtocol.OriginAllowed(request.Headers["Origin"]);
  }
  async void AcceptLoop(){
   while(!stop.IsCancellationRequested){
    HttpListenerContext context;
    try{context=await listener.GetContextAsync();}
    catch(HttpListenerException){return;}catch(ObjectDisposedException){return;}
    if(!Authorized(context.Request)){PcViewLog.Write("pcview denied "+context.Request.Url.AbsolutePath+" token="+(context.Request.QueryString["t"]??"-"));context.Response.StatusCode=403;context.Response.Close();continue;}
    string path=context.Request.Url.AbsolutePath;
    if(context.Request.Headers["Upgrade"]=="websocket"&&path=="/input"){
     try{
      Interlocked.Exchange(ref socket,null);
      var accepted=await context.AcceptWebSocketAsync(null,64*1024,TimeSpan.FromHours(1));
      Interlocked.Exchange(ref socket,accepted);
      PcViewLog.Write("pcview ws accepted");
      var unusedInput=Task.Run(delegate{var ignored=ReadInput(accepted.WebSocket);});
     }catch(Exception ex){PcViewLog.Write("pcview accept "+ex.Message);try{context.Response.Close();}catch{}}
     continue;
    }
    if(path=="/view"){var unusedFrames=Task.Run(delegate{StreamFrames(context.Response);});continue;}
    byte[] body=Encoding.UTF8.GetBytes(PcViewPage.Html);
    context.Response.ContentType="text/html; charset=utf-8";
    context.Response.Headers["Cache-Control"]="no-store";
    context.Response.ContentLength64=body.Length;
    try{await context.Response.OutputStream.WriteAsync(body,0,body.Length);}catch{}
    context.Response.Close();
   }
  }
  async Task ReadInput(WebSocket webSocket){
   var buffer=new byte[4096];var text=new StringBuilder();
   try{
    while(!stop.IsCancellationRequested&&webSocket.State==WebSocketState.Open){
     WebSocketReceiveResult result;
     try{result=await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer),stop.Token);}
     catch(OperationCanceledException){PcViewLog.Write("pcview input canceled");break;}
     catch(WebSocketException ex){PcViewLog.Write("pcview input websocket "+ex.Message);break;}
     catch(ObjectDisposedException){PcViewLog.Write("pcview input disposed");break;}
     if(result.MessageType==WebSocketMessageType.Close){PcViewLog.Write("pcview input close "+result.CloseStatus+" "+result.CloseStatusDescription);break;}
     if(result.Count>0)text.Append(Encoding.UTF8.GetString(buffer,0,result.Count));
     if(!result.EndOfMessage||text.Length>8192)continue;
     PcViewAction action;
     if(PcViewProtocol.TryParse(text.ToString(),out action))Dispatch(action);
     text.Length=0;
    }
    }catch(Exception ex){PcViewLog.Write("pcview input "+ex.GetType().Name+" "+ex.Message);}
    PcViewLog.Write("pcview input end");
   }
  void Dispatch(PcViewAction action){
   try{
    if(action.PointerAction>=0){PcViewInject.Move(action.U,action.V);if(action.PointerAction==1)PcViewInject.Click(1);}
    else if(action.Text!=null)PcViewInject.Text(action.Text);
    else if(action.Special!=0)PcViewInject.Special(action.Special);
    else if(action.Wheel!=0)PcViewInject.Wheel(action.Wheel);
    else if(action.Click!=0)PcViewInject.Click(action.Click);
    else if(action.MoveX!=0||action.MoveY!=0)PcViewInject.MoveBy(action.MoveX,action.MoveY);
   }catch(Exception ex){PcViewLog.Write("pcview inject "+ex.Message);}
  }
  async void StreamFrames(HttpListenerResponse response){
   response.StatusCode=200;
   response.ContentType="multipart/x-mixed-replace; boundary=phonedesk";
   response.Headers["Cache-Control"]="no-store";
   var output=response.OutputStream;
   var header=Encoding.ASCII.GetBytes("--phonedesk\r\nContent-Type: image/jpeg\r\nContent-Length: ");
   try{
    while(!stop.IsCancellationRequested&&ResponseOpen(response)){
     byte[] jpeg=PcViewCapture.Jpeg();
     if(jpeg==null)break;
     var length=Encoding.ASCII.GetBytes(jpeg.Length.ToString(CultureInfo.InvariantCulture)+"\r\n\r\n");
     await output.WriteAsync(header,0,header.Length);
     await output.WriteAsync(length,0,length.Length);
     await output.WriteAsync(jpeg,0,jpeg.Length);
     await output.WriteAsync(new byte[]{13,10},0,2);
     await output.FlushAsync();
     await Task.Delay(40,stop.Token);
    }
   }catch(Exception ex){PcViewLog.Write("pcview frame "+ex.Message);}
   try{response.Close();}catch{}
  }
  static bool ResponseOpen(HttpListenerResponse response){
   try{return response.OutputStream.CanWrite;}catch{return false;}
  }
  public void Dispose(){
   stop.Cancel();
   PcViewInject.RestoreAcceleration();
   try{
    var current=socket;
    if(current!=null&&current.WebSocket.State==WebSocketState.Open)
      current.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,"",CancellationToken.None).Wait(1000);
   }catch{}
   try{listener.Stop();listener.Close();}catch{}
   try{if(reversed)Adb("reverse --remove tcp:"+PcViewProtocol.Port);}catch{}
   lock(sync){running=false;reversed=false;}
  }
 }
 static class PcViewCapture {
  static readonly ImageCodecInfo JpegCodec=FindJpeg();
  static ImageCodecInfo FindJpeg(){
   foreach(var codec in ImageCodecInfo.GetImageEncoders())
    if(codec.MimeType=="image/jpeg")return codec;
   return null;
  }
  internal static byte[] Jpeg(){
   if(JpegCodec==null)return null;
   try{
    int width=PcViewInject.ScreenWidth;
    int height=PcViewInject.ScreenHeight;
    if(width<1||height<1)return null;
    double scale=Math.Min(1,Math.Min(1440.0/width,810.0/height));
    int targetWidth=Math.Max(1,(int)Math.Round(width*scale));
    int targetHeight=Math.Max(1,(int)Math.Round(height*scale));
    using(var full=new Bitmap(width,height,PixelFormat.Format24bppRgb))
    using(var graphics=Graphics.FromImage(full)){
     graphics.CopyFromScreen(new Point(0,0),new Point(0,0),new Size(width,height));
     using(var small=new Bitmap(targetWidth,targetHeight,PixelFormat.Format24bppRgb))
     using(var scaled=Graphics.FromImage(small)){
      scaled.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
      scaled.DrawImage(full,0,0,targetWidth,targetHeight);
      PcViewInject.DrawCursor(scaled,scale);
      using(var stream=new MemoryStream()){
       var parameters=new EncoderParameters(1);
       parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,75L);
       small.Save(stream,JpegCodec,parameters);
       return stream.ToArray();
      }
     }
    }
   }catch{return null;}
  }
 }
 static class PcViewPage {
  internal const string Html=@"<!doctype html><html lang='ja'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1,viewport-fit=cover'><title>PC画面</title><style>
  *{box-sizing:border-box}body{margin:0;background:#111;color:#eee;font:16px/1.4 system-ui,sans-serif;overflow:hidden}
  #screen{position:fixed;left:0;right:0;top:52px;bottom:68px;width:100%;height:calc(100% - 120px);object-fit:contain;display:block;touch-action:none;background:#000}
  #bar{position:fixed;left:0;right:0;bottom:0;display:flex;gap:8px;padding:8px;background:rgba(0,0,0,.75)}
  #bar button{flex:1;padding:12px;font-size:16px;color:#fff;background:#333;border:1px solid #666;border-radius:6px}
  #keys{position:fixed;left:8px;right:8px;top:8px;width:calc(100% - 16px);padding:10px;font-size:16px;background:#222;color:#fff;border:1px solid #666;border-radius:6px}
  #message{position:fixed;left:8px;top:56px;color:#ffcf6b;font-size:14px;text-shadow:0 1px 2px #000}
  </style></head><body>
  <img id='screen' alt='PC画面'><input id='keys' type='text' autocomplete='off' autocapitalize='off' spellcheck='false' placeholder='PCへ入力'>
  <div id='message'></div><div id='bar'><button id='left'>左クリック</button><button id='wheelup'>↑スクロール</button><button id='wheeldown'>↓スクロール</button><button id='right2'>右クリック</button></div>
  <script>
  'use strict';
  const q=new URLSearchParams(location.search).get('t')||'';
  const img=document.getElementById('screen'),keys=document.getElementById('keys'),message=document.getElementById('message');
  let ws,composing=false,sent='',lastX=null,lastY=null,zoom=1,pinchStart=0,pinchZoom=1,pendingX=0,pendingY=0;
  function flushMove(){
   if(pendingX||pendingY){send('move:'+pendingX+','+pendingY);pendingX=pendingY=0;}
   setTimeout(flushMove,8);
  }
  setTimeout(flushMove,8);
  const pointers=new Map();
  function send(value){if(ws&&ws.readyState===1)ws.send(value);}
  function b64(text){return btoa(unescape(encodeURIComponent(text)));}
  function applyZoom(){img.style.transform='scale('+zoom+')';}
  function pinchDistance(){
   const p=Array.from(pointers.values());
   if(p.length<2)return 0;
   return Math.hypot(p[0].x-p[1].x,p[0].y-p[1].y);
  }
  img.onpointerdown=e=>{
   pointers.set(e.pointerId,{x:e.clientX,y:e.clientY});
   img.setPointerCapture(e.pointerId);
   if(pointers.size===2){pinchStart=pinchDistance();pinchZoom=zoom;}
   lastX=null;e.preventDefault();
  };
  img.onpointermove=e=>{
   if(!pointers.has(e.pointerId))return;
   pointers.set(e.pointerId,{x:e.clientX,y:e.clientY});
   if(pointers.size>=2){
    const d=pinchDistance();
    if(pinchStart>0&&d>0){zoom=Math.max(1,Math.min(4,pinchZoom*d/pinchStart));applyZoom();}
    lastX=null;e.preventDefault();return;
   }
   if(lastX===null){lastX=e.clientX;lastY=e.clientY;e.preventDefault();return;}
   const dx=Math.round(e.clientX-lastX),dy=Math.round(e.clientY-lastY);
   lastX=e.clientX;lastY=e.clientY;
   if(dx||dy){pendingX+=Math.round(dx/zoom);pendingY+=Math.round(dy/zoom);}
   e.preventDefault();
  };
  function endPointer(e){
   pointers.delete(e.pointerId);
   if(pointers.size<2)pinchStart=0;
   lastX=null;e.preventDefault();
  }
  img.onpointerup=endPointer;img.onpointercancel=endPointer;
  document.getElementById('left').onclick=()=>send('click:1');
  document.getElementById('wheelup').onclick=()=>send('wheel:3');
  document.getElementById('wheeldown').onclick=()=>send('wheel:-3');
  document.getElementById('right2').onclick=()=>send('click:2');
  keys.addEventListener('compositionstart',()=>{composing=true;});
  keys.addEventListener('compositionend',e=>{composing=false;if(e.data){send('key:'+b64(e.data));}keys.value='';});
  keys.addEventListener('beforeinput',e=>{
   if(composing||!e.data||e.inputType!=='insertText')return;
   send('key:'+b64(e.data));e.preventDefault();keys.value='';
  });
  keys.addEventListener('keydown',e=>{
   const map={Backspace:8,Enter:13,Tab:9,Escape:27,ArrowLeft:37,ArrowUp:38,ArrowRight:39,ArrowDown:40,Delete:46};
   if(map[e.key]){send('special:'+map[e.key]);e.preventDefault();}
  });
  img.onerror=()=>{setTimeout(()=>{img.src='/view?t='+encodeURIComponent(q);},500);};
   img.src='/view?t='+encodeURIComponent(q);
  function connect(){
   ws=new WebSocket((location.protocol==='https:'?'wss://':'ws://')+location.host+'/input?t='+encodeURIComponent(q));
   ws.onopen=()=>{message.textContent='';};
   ws.onclose=e=>{console.log('pcview ws close',e.code,e.reason,e.wasClean);message.textContent='接続が切れました';setTimeout(connect,500);};
  }
  connect();
  setInterval(()=>{if(ws&&ws.readyState===1)ws.send('ping');},10000);
  </script></body></html>";
 }
}
