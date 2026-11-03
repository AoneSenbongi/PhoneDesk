// SPDX-License-Identifier: Apache-2.0
using System;
using System.Diagnostics;
using System.IO;

namespace PhoneDesk {
 sealed partial class App {
  readonly AlarmVolumeOverride alarmVolume=new AlarmVolumeOverride(PcPlayback.Capture,PcPlayback.Apply);
  void StartAlarmWatch(Device device,Session session){
   if(session.AlarmProcess!=null)return;
   var video=session.Process;
   System.Threading.Tasks.Task.Run(delegate{try{
    var result=Adb("-s "+DeviceParser.Quote(device.Serial)+" push "+DeviceParser.Quote(Path.Combine(runtime,"phonedesk-power.jar"))+" /data/local/tmp/phonedesk-alarm.jar");
    if(result.Code!=0)throw new IOException("アラーム監視の準備失敗");
    Dispatch(delegate{
     if(session.Process!=video||video.HasExited||session.AlarmProcess!=null)return;
     var process=new Process();
     process.StartInfo=new ProcessStartInfo(Path.Combine(runtime,"adb.exe"),"-s "+DeviceParser.Quote(device.Serial)+" shell -T CLASSPATH=/data/local/tmp/phonedesk-alarm.jar app_process / com.phonedesk.AlarmWatch"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
     process.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data=="ALARM 1"||e.Data=="ALARM 0"){bool active=e.Data=="ALARM 1";Dispatch(delegate{if(session.AlarmProcess==process&&session.Process==video)SetAlarm(device,session,active);});}};
     process.ErrorDataReceived+=delegate{};
     process.EnableRaisingEvents=true;process.Exited+=delegate{Dispatch(delegate{if(session.AlarmProcess!=process)return;session.AlarmProcess=null;SetAlarm(device,session,false);process.Dispose();Error("アラーム監視が停止しました");});};
     session.AlarmProcess=process;
     try{process.Start();process.BeginOutputReadLine();process.BeginErrorReadLine();}catch{session.AlarmProcess=null;process.Dispose();Error("アラーム監視の開始失敗");}
    });
   }catch(Exception ex){Dispatch(delegate{Error(ex.Message);});}});
  }
  void SetAlarm(Device device,Session session,bool active){
   if(session.AlarmActive==active)return;
   session.AlarmActive=active;
   try{alarmVolume.Set(device.Serial,active);}catch{Error("アラーム音量の変更失敗");}
   StopAudio(session);
   if(session.State.Running&&(Program.PcAudio||active))StartAudio(device,session);
   QueueAudio(Selected(),"get");
  }
  void StopAlarmWatch(Session session){
   var process=session.AlarmProcess;session.AlarmProcess=null;
   if(session.AlarmActive){session.AlarmActive=false;try{alarmVolume.Set(session.Serial,false);}catch{Error("アラーム終了時の音量復元失敗");}}
   if(process!=null){try{process.StandardInput.Close();if(!process.WaitForExit(2000))process.Kill();}catch{}process.Dispose();}
  }
 }
}
