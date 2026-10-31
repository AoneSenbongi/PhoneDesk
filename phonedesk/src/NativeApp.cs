// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
[assembly: System.Reflection.AssemblyTitle("PhoneDesk")]
[assembly: System.Reflection.AssemblyVersion("1.0.17.0")]
namespace PhoneDesk {
 static class Native {
  internal delegate IntPtr WndProc(IntPtr h,uint m,IntPtr w,IntPtr l);
  [StructLayout(LayoutKind.Sequential)] internal struct Point { internal int x,y; }
  [StructLayout(LayoutKind.Sequential)] internal struct Msg { internal IntPtr h; internal uint m; internal IntPtr w,l; internal uint time; internal Point pt; internal uint extra; }
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct NotifyIcon {
   internal uint size; internal IntPtr h; internal uint id,flags,callback; internal IntPtr icon;
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] internal string tip;
   internal uint state,stateMask;
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] internal string info;
   internal uint timeout;
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)] internal string title;
   internal uint infoFlags; internal Guid guid; internal IntPtr balloon;
  }
  [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern IntPtr CreateWindowEx(int ex,string cls,string name,int style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] internal static extern IntPtr Subclass(IntPtr h,int index,WndProc proc);
  [StructLayout(LayoutKind.Sequential)] internal struct Rect {internal int Left,Top,Right,Bottom;}
  [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo {internal int Size;internal Rect Monitor,Work;internal uint Flags;}
  [DllImport("user32.dll")] internal static extern IntPtr MonitorFromWindow(IntPtr h,uint flags);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
  [DllImport("user32.dll")] internal static extern int FillRect(IntPtr dc,ref Rect rect,IntPtr brush);
  [DllImport("user32.dll")] internal static extern IntPtr BeginPaint(IntPtr h,IntPtr paint);
  [DllImport("user32.dll")] internal static extern bool EndPaint(IntPtr h,IntPtr paint);
  [DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SetParent(IntPtr child,IntPtr parent);
  [DllImport("user32.dll")] internal static extern IntPtr GetParent(IntPtr h);
  [DllImport("user32.dll")] internal static extern IntPtr SetFocus(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr h,out Rect rect);
  [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr h,out Rect rect);
  [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] internal static extern IntPtr GetStyle(IntPtr h,int index);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] internal static extern IntPtr SetStyle(IntPtr h,int index,IntPtr value);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr DefWindowProc(IntPtr h,uint m,IntPtr w,IntPtr l);
  [DllImport("user32.dll")] internal static extern int GetMessage(out Msg msg,IntPtr h,uint min,uint max);
  [DllImport("user32.dll")] internal static extern bool TranslateMessage(ref Msg msg);
  [DllImport("user32.dll")] internal static extern IntPtr DispatchMessage(ref Msg msg);
  [DllImport("user32.dll")] internal static extern bool IsDialogMessage(IntPtr h,ref Msg msg);
  [DllImport("user32.dll")] internal static extern void PostQuitMessage(int code);
  [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="SendMessageW")] internal static extern IntPtr SendText(IntPtr h,uint m,IntPtr w,string l);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern bool SetWindowText(IntPtr h,string text);
  [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr h,int cmd);
  [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool DestroyWindow(IntPtr h);
  [DllImport("user32.dll")] internal static extern UIntPtr SetTimer(IntPtr h,UIntPtr id,uint ms,IntPtr callback);
  [DllImport("user32.dll")] internal static extern bool KillTimer(IntPtr h,UIntPtr id);
  [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
  [DllImport("gdi32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr CreateFont(int height,int width,int escape,int orientation,int weight,uint italic,uint underline,uint strike,uint charset,uint precision,uint clip,uint quality,uint pitch,string face);
  [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr h);
  [DllImport("user32.dll")] internal static extern IntPtr LoadIcon(IntPtr instance,IntPtr name);
  [DllImport("user32.dll")] internal static extern IntPtr CreatePopupMenu();
  [DllImport("user32.dll")] internal static extern IntPtr GetSystemMenu(IntPtr h,bool revert);
  [DllImport("user32.dll")] internal static extern uint CheckMenuItem(IntPtr menu,uint item,uint flags);
  [StructLayout(LayoutKind.Sequential)] internal struct CommonControls {internal int Size,Classes;}
  [DllImport("comctl32.dll")] internal static extern bool InitCommonControlsEx(ref CommonControls controls);
  [DllImport("user32.dll")] internal static extern bool EnableWindow(IntPtr h,bool enable);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern bool AppendMenu(IntPtr menu,uint flags,UIntPtr id,string text);
  [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point pt);
  [DllImport("user32.dll")] internal static extern int TrackPopupMenu(IntPtr menu,uint flags,int x,int y,int reserved,IntPtr h,IntPtr rect);
  [DllImport("user32.dll")] internal static extern bool DestroyMenu(IntPtr menu);
  [DllImport("shell32.dll",CharSet=CharSet.Unicode)] internal static extern bool Shell_NotifyIcon(uint action,ref NotifyIcon icon);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string name);
 }
 static class Program {
  internal const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
  internal static readonly string Exe=System.Reflection.Assembly.GetExecutingAssembly().Location;
  internal static bool PcAudio {
   get {using(var k=Registry.CurrentUser.OpenSubKey(@"Software\PhoneDesk"))return k==null||Convert.ToInt32(k.GetValue("PcAudio",1))!=0;}
   set {using(var k=Registry.CurrentUser.CreateSubKey(@"Software\PhoneDesk"))k.SetValue("PcAudio",value?1:0,RegistryValueKind.DWord);}
  }
  internal static bool Startup {
   get {using(var k=Registry.CurrentUser.OpenSubKey(RunKey)) return k!=null && k.GetValue("PhoneDesk")!=null;}
   set {using(var k=Registry.CurrentUser.CreateSubKey(RunKey)) {if(value) k.SetValue("PhoneDesk",DeviceParser.Quote(Exe)+" --background");else k.DeleteValue("PhoneDesk",false);}}
  }
  [STAThread] static void Main(string[] args) {
   bool first; using(var mutex=new Mutex(true,@"Local\PhoneDesk.App.1",out first)) {
    if(!first) {
     string signal=args.Contains("--lock")?"Lock":args.Contains("--screen-off")?"Off":args.Contains("--screen-on")?"On":"Show";
     using(var e=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\PhoneDesk."+signal+".1"))e.Set();return;
    }
    try {Native.SetProcessDPIAware();new App().Run(args.Contains("--background"));}
    catch(Exception ex) {File.WriteAllText(Path.Combine(Path.GetTempPath(),"PhoneDesk-error.txt"),ex.ToString());}
   }
  }
 }
 sealed class Session {
  internal readonly SessionState State=new SessionState(); internal Process Process,AudioProcess,ClipboardProcess,RotationProcess;
  internal readonly StringBuilder Errors=new StringBuilder();
  internal Viewer Viewer;
  internal bool AudioVolumeReady;
  internal int FrameWidth,FrameHeight;
 }
 sealed class Result {internal int Code; internal string Output,Error;}
 sealed partial class App {
  readonly string runtime=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"runtime");
  readonly string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneDesk");
  readonly Dictionary<string,Session> sessions=new Dictionary<string,Session>();
  readonly ConcurrentQueue<Action> queue=new ConcurrentQueue<Action>();
  readonly EventWaitHandle show=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\PhoneDesk.Show.1");
  readonly EventWaitHandle screenOff=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\PhoneDesk.Off.1");
  readonly EventWaitHandle screenOn=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\PhoneDesk.On.1");
  readonly EventWaitHandle screenLock=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\PhoneDesk.Lock.1");
  List<Device> devices=new List<Device>(); IntPtr window,list,status,startup,font,detailsPanel;
  readonly Dictionary<int,IntPtr> controls=new Dictionary<int,IntPtr>();
  Session active;
  Native.WndProc callback,panelCallback; Native.NotifyIcon tray; uint taskbarCreated;
  bool busy,powerBusy,exiting; string lastError="";
  internal void Run(bool background) {
   Directory.CreateDirectory(data);
   window=Native.CreateWindowEx(0,"STATIC","PhoneDesk",unchecked((int)0x02CF0000),200,150,610,442,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
   if(window==IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   callback=WindowProc; Native.Subclass(window,-4,callback);
   var systemMenu=Native.GetSystemMenu(window,false);Native.AppendMenu(systemMenu,0x0800,UIntPtr.Zero,"");Native.AppendMenu(systemMenu,0,new UIntPtr(0x1F00),"詳細");
   detailsPanel=Native.CreateWindowEx(0,"STATIC","",0x52000000,0,0,594,251,window,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
   panelCallback=delegate(IntPtr h,uint m,IntPtr w,IntPtr l){if(m==0x0111||m==0x0114||m==0x002B)return Native.SendMessage(window,m,w,l);return Native.DefWindowProc(h,m,w,l);};
   Native.Subclass(detailsPanel,-4,panelCallback);
   font=Native.CreateFont(-18,0,0,0,400,0,0,0,1,0,0,5,0,"Yu Gothic UI");
   var common=new Native.CommonControls{Size=8,Classes=4};Native.InitCommonControlsEx(ref common);
   list=Control("LISTBOX","",20,18,560,110,1,0x00800101|0x00200000);
   Control("BUTTON","表示",20,140,80,34,2,0x00010000);
   Control("BUTTON","点灯",110,140,80,34,3,0x00010000);
   Control("BUTTON","消灯",200,140,80,34,8,0x00010000);
   Control("BUTTON","ロック",290,140,95,34,9,0x00010000);
   Control("BUTTON","キーボード設定",395,140,185,34,4,0x00010000);
   startup=Control("BUTTON","ログオン時に起動",20,190,550,28,5,0x00010003);
   status=Control("STATIC","確認中",20,228,560,24,0,0);
   Control("BUTTON","詳細を閉じる",12,8,296,32,10,0x00010000);
   Control("BUTTON","音量−",12,136,145,32,11,0x00010000);
   Control("BUTTON","音量＋",163,136,145,32,12,0x00010000);
   Control("msctls_trackbar32","",12,176,296,32,13,0x00010001);
   Control("BUTTON","右回転",12,136,296,32,15,0x00010000);
   Control("BUTTON","PCから音を出す",12,208,296,28,14,0x0001000B);
   foreach(int id in new[]{11,12,13})Native.EnableWindow(controls[id],false);
   LayoutShell();
   if(!File.Exists(Path.Combine(data,"initialized"))) {Program.Startup=true;File.WriteAllText(Path.Combine(data,"initialized"),"1");}
   Native.SendMessage(startup,0x00F1,new IntPtr(Program.Startup?1:0),IntPtr.Zero);
   taskbarCreated=Native.RegisterWindowMessage("TaskbarCreated");
   tray=new Native.NotifyIcon {size=(uint)Marshal.SizeOf(typeof(Native.NotifyIcon)),h=window,id=1,flags=7,callback=0x8002,icon=Native.LoadIcon(IntPtr.Zero,new IntPtr(32512)),tip="PhoneDesk — USB接続待ち"};
   Native.Shell_NotifyIcon(0,ref tray);Native.SetTimer(window,new UIntPtr(1),2000,IntPtr.Zero);
   if(!background) Reveal(); Poll();
   Native.Msg msg; while(Native.GetMessage(out msg,IntPtr.Zero,0,0)>0) {
    if(!Native.IsDialogMessage(window,ref msg)) {Native.TranslateMessage(ref msg);Native.DispatchMessage(ref msg);}
   }
   show.Dispose();screenOff.Dispose();screenOn.Dispose();screenLock.Dispose();if(font!=IntPtr.Zero) Native.DeleteObject(font);
  }
  IntPtr Control(string cls,string text,int x,int y,int w,int h,int id,int extra) {
   var result=Native.CreateWindowEx(0,cls,text,unchecked((int)0x50000000)|extra,x,y,w,h,detailsPanel,new IntPtr(id),IntPtr.Zero,IntPtr.Zero);
   if(result==IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   controls[id]=result;Native.SendMessage(result,0x0030,font,new IntPtr(1));return result;
  }
  IntPtr WindowProc(IntPtr h,uint m,IntPtr w,IntPtr l) {
   try {
    if(m==0x002B&&w.ToInt32()==14){AudioSwitch.Draw(l,Program.PcAudio,font);return new IntPtr(1);}
    if(m==0x0114&&l==controls[13]){QueueAudio(Selected(),Native.SendMessage(controls[13],0x0400,IntPtr.Zero,IntPtr.Zero).ToInt32().ToString());if((w.ToInt32()&65535)==8)FocusStream();return IntPtr.Zero;}
    if(m==0x0007&&active!=null&&active.Viewer!=null){active.Viewer.Focus();return IntPtr.Zero;}
    if(m==0x0112&&(w.ToInt64()&0xFFF0)==0x1F00){ToggleDetails();return IntPtr.Zero;}
    if(m==0x0005){LayoutShell();return IntPtr.Zero;}
    if(active!=null&&active.Viewer!=null&&(m==0x0024||m==0x0214))return active.Viewer.Geometry(m,w,l);
    if(m==taskbarCreated&&taskbarCreated!=0)Native.Shell_NotifyIcon(0,ref tray);
    if(m==0x0010){Native.ShowWindow(window,0);return IntPtr.Zero;}
    if(m==0x0113){if(show.WaitOne(0))Reveal();if(screenLock.WaitOne(0))DevicePowerAction("lock");else if(screenOff.WaitOne(0))ScreenPower(false);else if(screenOn.WaitOne(0))ScreenPower(true);Poll();return IntPtr.Zero;}
    if(m==0x8001){Action action;while(queue.TryDequeue(out action))action();return IntPtr.Zero;}
    if(m==0x8002){if(l.ToInt32()==0x0203||l.ToInt32()==0x0202)Reveal();if(l.ToInt32()==0x0205)Menu();return IntPtr.Zero;}
    if(m==0x0111){
     int id=w.ToInt32()&65535;int notification=(w.ToInt32()>>16)&65535;
     if(id==2||(id==1&&notification==2))OpenSelected();
     if(id==3)ScreenPower(true);
     if(id==8)ScreenPower(false);
     if(id==9)DevicePowerAction("lock");
     if(id==4)DeviceCommand("shell am start -a android.settings.HARD_KEYBOARD_SETTINGS");
     if(id==5)Program.Startup=Native.SendMessage(startup,0x00F0,IntPtr.Zero,IntPtr.Zero).ToInt32()==1;
     if(id==10)ToggleDetails();
     if(id==11)VolumeStep(-1);
     if(id==12)VolumeStep(1);
     if(id==14)ChangeAudioOutput();
     if(id==15&&active!=null&&active.Viewer!=null)active.Viewer.ToggleRotation();
     return IntPtr.Zero;
    }
    if(m==0x0002){Native.PostQuitMessage(0);return IntPtr.Zero;}
   }catch(Exception ex){Error(ex.Message);}
   return Native.DefWindowProc(h,m,w,l);
  }
  void Reveal(){Native.ShowWindow(window,9);Native.SetForegroundWindow(window);FocusStream();}
  void FocusStream(){if(active!=null&&active.Viewer!=null)active.Viewer.Focus();}
  void LayoutShell(){
   if(detailsPanel==IntPtr.Zero)return;
   Native.Rect r;Native.GetClientRect(window,out r);
   var viewer=active==null?null:active.Viewer;
   if(viewer!=null)viewer.Resize();
   bool expanded=viewer==null||viewer.DetailsWidth>0;
   Native.CheckMenuItem(Native.GetSystemMenu(window,false),0x1F00,viewer!=null&&viewer.DetailsWidth>0?8u:0u);
   Native.ShowWindow(detailsPanel,expanded?5:0);
   if(!expanded)return;
   int panelWidth=viewer==null?r.Right:viewer.DetailsWidth;
   Native.SetWindowPos(detailsPanel,IntPtr.Zero,0,0,panelWidth,viewer==null?396:Math.Max(436,r.Bottom),0x0010);
   int width=Math.Max(1,panelWidth-24);
   bool connected=viewer!=null;
   Native.ShowWindow(controls[10],connected?5:0);
   Native.EnableWindow(controls[15],connected);
   Native.SetWindowText(controls[15],connected&&viewer.Rotated?"元に戻す":"右回転");
   int offset=connected?40:0;
   PlaceControl(10,12,8,width,32);PlaceControl(1,12,8+offset,width,80);
   int[] ids={2,3,8,9};
   for(int i=0;i<ids.Length;i++)PlaceControl(ids[i],12+i*(width+6)/4,96+offset,(width+6)/4-6,32);
   PlaceControl(15,12,136+offset,width,32);PlaceControl(11,12,176+offset,(width-6)/2,32);PlaceControl(12,12+(width+6)/2,176+offset,(width-6)/2,32);
   PlaceControl(13,12,212+offset,width,36);
   PlaceControl(14,12,248+offset,width,28);PlaceControl(4,12,284+offset,width,32);PlaceControl(5,12,324+offset,width,28);PlaceControl(0,12,360+offset,width,24);
  }
  void PlaceControl(int id,int x,int y,int width,int height){IntPtr h;if(controls.TryGetValue(id,out h))Native.SetWindowPos(h,IntPtr.Zero,x,y,width,height,0x0014);}
  void ToggleDetails(){if(active==null||active.Viewer==null)return;active.Viewer.SetDetails(active.Viewer.DetailsWidth==0);LayoutShell();}
  void ShowSession(Session s,Device d){
   if(active!=null&&active!=s&&active.Viewer!=null)Native.ShowWindow(active.Viewer.Window,0);
   active=s;SelectDevice(d);Native.SetWindowText(window,"PhoneDesk | "+d.Model);
   s.Viewer.Fit(420);LayoutShell();s.Viewer.Reveal();
  }
  void RemoveViewer(Session s){
   if(active==s){active=null;Native.SetWindowText(window,"PhoneDesk");if(!exiting){Native.ShowWindow(window,9);Native.SetWindowPos(window,IntPtr.Zero,0,0,610,442,0x0016);LayoutShell();}}
   if(s.Viewer!=null){s.Viewer.Dispose();s.Viewer=null;}
  }
  void Menu(){
   var menu=Native.CreatePopupMenu();Native.AppendMenu(menu,0,new UIntPtr(1),"PhoneDeskを開く");Native.AppendMenu(menu,0,new UIntPtr(2),"終了");
   Native.Point pt;Native.GetCursorPos(out pt);Native.SetForegroundWindow(window);
   int command=Native.TrackPopupMenu(menu,0x100|0x2,pt.x,pt.y,0,window,IntPtr.Zero);Native.DestroyMenu(menu);
   if(command==1)Reveal();if(command==2)Quit();Native.PostMessage(window,0,IntPtr.Zero,IntPtr.Zero);
  }
  void Quit(){exiting=true;Native.KillTimer(window,new UIntPtr(1));foreach(var s in sessions.Values)Stop(s);Native.Shell_NotifyIcon(2,ref tray);Native.DestroyWindow(window);}
  void Dispatch(Action action){if(exiting)return;queue.Enqueue(action);Native.PostMessage(window,0x8001,IntPtr.Zero,IntPtr.Zero);}
  void Error(string message){lastError=message;Native.SetWindowText(status,message);}
  Result Adb(string args){
   using(var p=new Process()){
    p.StartInfo=new ProcessStartInfo(Path.Combine(runtime,"adb.exe"),args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
    p.Start();var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();
    if(!p.WaitForExit(6000)){try{p.Kill();}catch{}throw new TimeoutException("ADB応答なし");}
    return new Result{Code=p.ExitCode,Output=stdout.Result,Error=stderr.Result};
   }
  }
  void Poll(){
   if(busy||exiting)return;busy=true;
   Task.Run(delegate{try{var r=Adb("devices -l");Dispatch(delegate{try{Update(r);}finally{busy=false;}});}catch(Exception ex){Dispatch(delegate{busy=false;Error(ex.Message);});}});
  }
  Device Selected(){int index=Native.SendMessage(list,0x0188,IntPtr.Zero,IntPtr.Zero).ToInt32();return index>=0&&index<devices.Count?devices[index]:null;}
  void Update(Result r){
   if(r.Code!=0){Error("ADB接続エラー: "+r.Error.Trim());return;}
   var previous=Selected();string serial=previous==null?"":previous.Serial;devices=DeviceParser.Parse(r.Output).Where(d=>d.Usb).ToList();
   var present=new HashSet<string>(devices.Select(d=>d.Serial));
   foreach(var key in sessions.Keys.Where(k=>!present.Contains(k)).ToArray()){Stop(sessions[key]);sessions.Remove(key);}
   Native.SendMessage(list,0x000B,IntPtr.Zero,IntPtr.Zero);Native.SendMessage(list,0x0184,IntPtr.Zero,IntPtr.Zero);
   int selected=0;for(int i=0;i<devices.Count;i++){Native.SendText(list,0x0180,IntPtr.Zero,devices[i].ToString());if(devices[i].Serial==serial)selected=i;}
   Native.SendMessage(list,0x0186,new IntPtr(selected),IntPtr.Zero);Native.SendMessage(list,0x000B,new IntPtr(1),IntPtr.Zero);
   foreach(var d in devices.Where(x=>x.State=="device")){Session s;if(!sessions.TryGetValue(d.Serial,out s)){s=new Session();sessions.Add(d.Serial,s);}if(s.State.ShouldStart(DateTime.UtcNow))Start(d,s);}
   string message;
   if(devices.Any(d=>d.State=="unauthorized"))message="未認証";
   else if(devices.Any(d=>d.State=="offline"))message="オフライン";
   else if(devices.Count==0){message="未接続";lastError="";}
   else message=lastError.Length>0?lastError:"接続中";
   Native.SetWindowText(status,message);
   if(active==null||active.Viewer.DetailsWidth>0||Program.PcAudio)QueueAudio(Selected(),"get");
  }
  void Start(Device d,Session s){
   try{
    s.Errors.Clear();s.FrameWidth=s.FrameHeight=0;var p=new Process();
    p.StartInfo=new ProcessStartInfo(Path.Combine(runtime,"scrcpy.exe"),StreamOptions.Arguments(d)){WorkingDirectory=runtime,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardErrorEncoding=Encoding.UTF8};
    p.StartInfo.EnvironmentVariables["ADB"]=Path.Combine(runtime,"adb.exe");
    p.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)lock(s.Errors){if(s.Errors.Length<6000)s.Errors.AppendLine(e.Data);}};
    p.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){
     var match=System.Text.RegularExpressions.Regex.Match(e.Data??"",@"^INFO: Texture(?: \([^)]*\))?: (\d+)x(\d+)$");
     int width,height;
     if(match.Success&&int.TryParse(match.Groups[1].Value,out width)&&int.TryParse(match.Groups[2].Value,out height))
      Dispatch(delegate{if(s.Process!=p)return;s.FrameWidth=width;s.FrameHeight=height;if(s.Viewer!=null){s.Viewer.UpdateFrameSize(width,height);if(active==s)LayoutShell();}});
    };
    p.EnableRaisingEvents=true;p.Exited+=delegate{int code;try{code=p.ExitCode;}catch{code=1;}Dispatch(delegate{
     if(s.Process!=p)return;s.State.Exited(code,DateTime.UtcNow);
     StopRotation(s);StopClipboard(s);StopAudio(s);RemoveViewer(s);
     if(code!=0){string diagnostic;lock(s.Errors)diagnostic=s.Errors.ToString();Error("表示失敗（"+s.State.Failures+"/3）");try{File.WriteAllText(Path.Combine(data,"last-error.txt"),diagnostic,Encoding.UTF8);}catch{}}
     p.Dispose();
    });};
    s.Process=p;s.State.Started();p.Start();p.BeginErrorReadLine();p.BeginOutputReadLine();lastError="";
    Task.Run(delegate {
     try {
      IntPtr child=IntPtr.Zero;
      for(int i=0;i<80&&!p.HasExited;i++){p.Refresh();child=p.MainWindowHandle;if(child!=IntPtr.Zero)break;Thread.Sleep(100);}
      if(child==IntPtr.Zero) {Dispatch(delegate{if(s.Process==p){Stop(s);s.State.Exited(1,DateTime.UtcNow);Error("表示ウィンドウの取得失敗");}});return;}
      Dispatch(delegate {if(s.Process==p&&!p.HasExited){s.Viewer=new Viewer(window,child,font,delegate(string mode){if(mode=="rotate")ToggleDeviceRotation(d);else DevicePowerAction(mode,d);},ToggleDetails);s.Viewer.UpdateFrameSize(s.FrameWidth,s.FrameHeight);s.Viewer.SetDeviceRotation(s.RotationProcess!=null);ShowSession(s,d);StartClipboard(d,s);if(Program.PcAudio)StartAudio(d,s);}});
     }catch(Exception ex){Dispatch(delegate{Error(ex.Message);});}
    });
   }catch(Exception ex){s.State.Exited(1,DateTime.UtcNow);Error(ex.Message);}
  }
  void OpenSelected(){
   var d=Selected();if(d==null||d.State!="device"){Error("操作可能な端末なし");return;}
   Session s;if(!sessions.TryGetValue(d.Serial,out s)){s=new Session();sessions.Add(d.Serial,s);}
   if(s.State.Running&&s.Process!=null){if(s.Viewer!=null)ShowSession(s,d);return;}
   s.State.Manual();Start(d,s);
  }
  void DeviceCommand(string command){
   var d=Selected();if(d==null||d.State!="device"){Error("操作可能な端末なし");return;}
    Task.Run(delegate{try{var r=Adb("-s "+DeviceParser.Quote(d.Serial)+" "+command);if(r.Code!=0)Dispatch(delegate{Error(r.Error.Trim());});else Dispatch(FocusStream);}catch(Exception ex){Dispatch(delegate{Error(ex.Message);});}});
  }
  readonly Dictionary<string,Process> powerGuards=new Dictionary<string,Process>();
  void SelectDevice(Device d){int i=devices.FindIndex(x=>x.Serial==d.Serial);if(i>=0)Native.SendMessage(list,0x0186,new IntPtr(i),IntPtr.Zero);}
  void CancelPowerGuard(string serial) {
   Process p;if(!powerGuards.TryGetValue(serial,out p))return;
   if(!p.HasExited) {
    p.StandardInput.Write('0');p.StandardInput.Flush();
    if(!p.WaitForExit(6000))throw new TimeoutException("画面復帰監視の停止失敗");
    if(p.ExitCode!=0)throw new InvalidOperationException("画面復帰監視の停止失敗");
   }
   p.Dispose();powerGuards.Remove(serial);
  }
  void ScreenPower(bool on) {DevicePowerAction(on?"on":"off");}
  void DevicePowerAction(string mode) {DevicePowerAction(mode,Selected());}
  void DevicePowerAction(string mode,Device d) {
   if(powerBusy)return;
   if(d==null||!devices.Any(x=>x.Serial==d.Serial&&x.State=="device")) {Error("操作可能な端末なし");return;}
   powerBusy=true;
   Task.Run(delegate {
    try {
     string prefix="-s "+DeviceParser.Quote(d.Serial)+" ";
     CancelPowerGuard(d.Serial);
     if(mode=="on") {
      var wake=Adb(prefix+"shell input keyevent KEYCODE_WAKEUP");
      if(wake.Code!=0)throw new InvalidOperationException("点灯失敗");
     }
     foreach(var file in new[]{new[]{"scrcpy-server","phonedesk-scrcpy.jar"},new[]{"phonedesk-power.jar","phonedesk-power.jar"}}) {
      var push=Adb(prefix+"push "+DeviceParser.Quote(Path.Combine(runtime,file[0]))+" /data/local/tmp/"+file[1]);
      if(push.Code!=0)throw new InvalidOperationException("画面電源の準備失敗");
     }
     string helper="shell -T CLASSPATH=/data/local/tmp/phonedesk-scrcpy.jar:/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.ScreenPower ";
     if(mode=="off") {
      var guard=new Process();
      guard.StartInfo=new ProcessStartInfo(Path.Combine(runtime,"adb.exe"),prefix+helper+"watch-off"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
      try {
       guard.Start();guard.StandardError.ReadToEndAsync();
       var ready=guard.StandardOutput.ReadLineAsync();
       if(!ready.Wait(6000)||ready.Result!="OK watch-off")throw new InvalidOperationException("画面復帰監視の開始失敗");
       powerGuards.Add(d.Serial,guard);
      } catch {try{guard.StandardInput.Close();}catch{}guard.Dispose();throw;}
      Dispatch(delegate{powerBusy=false;lastError="";Native.SetWindowText(status,"接続中");FocusStream();});return;
     }
     var result=Adb(prefix+helper+mode);
     if(result.Code!=0||!result.Output.Contains("OK "+mode))throw new InvalidOperationException(mode=="lock"?"ロック失敗":"画面電源操作失敗");
     if(mode=="lock") {
      var sleep=Adb(prefix+"shell input keyevent KEYCODE_SLEEP");
      if(sleep.Code!=0)throw new InvalidOperationException("スリープ操作失敗");
     }
     Dispatch(delegate{powerBusy=false;lastError="";Native.SetWindowText(status,"接続中");FocusStream();});
    }catch(Exception ex){Dispatch(delegate{powerBusy=false;Error(ex.Message);});}
   });
  }
  void Stop(Session s){StopRotation(s);StopClipboard(s);StopAudio(s);var p=s.Process;s.Process=null;RemoveViewer(s);if(p!=null){try{if(!p.HasExited)p.Kill();}catch{}p.Dispose();}}
 }
}
