// SPDX-License-Identifier: Apache-2.0
using System;using System.Diagnostics;using System.IO;using System.Text;using System.Threading;using System.Threading.Tasks;using System.Runtime.InteropServices;
namespace PhoneDesk {
 static class WindowsClipboardWriter {
  [DllImport("user32.dll")]static extern bool OpenClipboard(IntPtr owner);
  [DllImport("user32.dll")]static extern bool CloseClipboard();
  [DllImport("user32.dll")]static extern bool EmptyClipboard();
  [DllImport("user32.dll")]static extern IntPtr SetClipboardData(uint format,IntPtr memory);
  [DllImport("kernel32.dll")]static extern IntPtr GlobalAlloc(uint flags,UIntPtr size);
  [DllImport("kernel32.dll")]static extern IntPtr GlobalLock(IntPtr memory);
  [DllImport("kernel32.dll")]static extern bool GlobalUnlock(IntPtr memory);
  [DllImport("kernel32.dll")]static extern IntPtr GlobalFree(IntPtr memory);
  internal static bool TrySet(IntPtr owner,string text){
   byte[] bytes=Encoding.Unicode.GetBytes(text+"\0");IntPtr memory=GlobalAlloc(2,new UIntPtr((uint)bytes.Length));
   if(memory==IntPtr.Zero)return false;
   try{
    IntPtr data=GlobalLock(memory);if(data==IntPtr.Zero)return false;
    try{Marshal.Copy(bytes,0,data,bytes.Length);}finally{GlobalUnlock(memory);}
    if(!OpenClipboard(owner))return false;
    try{if(!EmptyClipboard()||SetClipboardData(13,memory)==IntPtr.Zero)return false;memory=IntPtr.Zero;return true;}
    finally{CloseClipboard();}
   }finally{if(memory!=IntPtr.Zero)GlobalFree(memory);}
  }
 }
 sealed partial class App {
  void StartClipboard(Device d,Session session){
   var video=session.Process;
   Task.Run(delegate{
    try{
     string prefix="-s "+DeviceParser.Quote(d.Serial)+" ";
     foreach(var file in new[]{new[]{"scrcpy-server","phonedesk-clipboard-server.jar"},new[]{"phonedesk-power.jar","phonedesk-clipboard-helper.jar"}}){
      var result=Adb(prefix+"push "+DeviceParser.Quote(Path.Combine(runtime,file[0]))+" /data/local/tmp/"+file[1]);
      if(result.Code!=0)throw new InvalidOperationException("コピー共有の準備失敗");
     }
     Dispatch(delegate{
      if(session.Process!=video||video.HasExited||session.ClipboardProcess!=null)return;
      var p=new Process();
      p.StartInfo=new ProcessStartInfo(Path.Combine(runtime,"adb.exe"),prefix+"shell -T CLASSPATH=/data/local/tmp/phonedesk-clipboard-server.jar:/data/local/tmp/phonedesk-clipboard-helper.jar app_process / com.phonedesk.ClipboardWatch"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
      p.ErrorDataReceived+=delegate{};
      p.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){
       if(e.Data==null||!e.Data.StartsWith("CLIP "))return;
       try{
        string text=new UTF8Encoding(false,true).GetString(Convert.FromBase64String(e.Data.Substring(5)));
        for(int i=0;i<10&&session.ClipboardProcess==p&&session.Process==video;i++){
         if(WindowsClipboardWriter.TrySet(window,text))return;Thread.Sleep(50);
        }
       }catch(FormatException){}catch(DecoderFallbackException){}
      };
      p.EnableRaisingEvents=true;p.Exited+=delegate{Dispatch(delegate{if(session.ClipboardProcess!=p)return;session.ClipboardProcess=null;p.Dispose();if(!exiting)Error("コピー共有が停止しました");});};
      session.ClipboardProcess=p;
      try{p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();}catch{session.ClipboardProcess=null;p.Dispose();Error("コピー共有の開始失敗");}
     });
    }catch(Exception){Dispatch(delegate{Error("コピー共有の準備失敗");});}
   });
  }
  void StopClipboard(Session session){
   var p=session.ClipboardProcess;session.ClipboardProcess=null;
   if(p!=null){try{p.StandardInput.Close();if(!p.WaitForExit(2000))p.Kill();}catch{}p.Dispose();}
  }
 }
}
