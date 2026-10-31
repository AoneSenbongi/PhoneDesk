// SPDX-License-Identifier: Apache-2.0
using System;using System.Diagnostics;using System.IO;using System.Threading.Tasks;
namespace PhoneDesk {
 sealed partial class App {
  bool rotationBusy;
  void ToggleDeviceRotation(Device d){
   Session session;if(rotationBusy||d==null||!sessions.TryGetValue(d.Serial,out session)||session.Viewer==null)return;
   rotationBusy=true;var video=session.Process;
   Task.Run(delegate{
    try{
     if(session.RotationProcess!=null){if(!StopRotation(session))throw new InvalidOperationException("回転設定を戻せませんでした");}
     else{
      string prefix="-s "+DeviceParser.Quote(d.Serial)+" ";
      var push=Adb(prefix+"push "+DeviceParser.Quote(Path.Combine(runtime,"phonedesk-power.jar"))+" /data/local/tmp/phonedesk-rotation.jar");
      if(push.Code!=0)throw new InvalidOperationException("回転操作の準備失敗");
      var guard=new Process();guard.StartInfo=new ProcessStartInfo(Path.Combine(runtime,"adb.exe"),prefix+"shell -T CLASSPATH=/data/local/tmp/phonedesk-rotation.jar app_process / com.phonedesk.RotationControl"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
      try{
       guard.Start();guard.StandardError.ReadToEndAsync();var ready=guard.StandardOutput.ReadLineAsync();
       if(!ready.Wait(10000)||ready.Result!="READY")throw new InvalidOperationException("スマホの右回転に失敗しました");
       session.RotationProcess=guard;
       if(session.Process!=video)StopRotation(session);
      }catch{try{guard.StandardInput.Close();}catch{}guard.Dispose();throw;}
     }
     Dispatch(delegate{if(session.Viewer!=null)session.Viewer.SetDeviceRotation(session.RotationProcess!=null);LayoutShell();FocusStream();});
    }catch(Exception ex){Dispatch(delegate{Error(ex.Message);});}
    finally{Dispatch(delegate{rotationBusy=false;});}
   });
  }
  bool StopRotation(Session session){
   var p=session.RotationProcess;session.RotationProcess=null;
   if(p==null)return true;
   try{
    if(!p.HasExited){p.StandardInput.Close();if(!p.WaitForExit(8000)){p.Kill();return false;}}
    return p.ExitCode==0;
   }catch{return false;}finally{p.Dispose();}
  }
 }
}
