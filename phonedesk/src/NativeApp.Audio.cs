// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PhoneDesk {
 sealed partial class App {
  readonly object audioSync=new object();
  readonly HashSet<string> audioReady=new HashSet<string>();
  bool audioBusy;Device audioTarget;string audioRequest;long audioVersion;
  int audioMin,audioMax;string audioSerial="";

  void ChangeAudioOutput() {
   lock(audioSync){audioRequest=null;audioVersion++;}
   Program.PcAudio=!Program.PcAudio;AudioSwitch.Refresh(controls[14]);
   foreach(var d in devices) {
    Session session;if(!sessions.TryGetValue(d.Serial,out session)||!session.State.Running)continue;
    if(Program.PcAudio||session.AlarmActive)StartAudio(d,session);else StopAudio(session);
   }
   foreach(int id in new[]{11,12,13})Native.EnableWindow(controls[id],false);
   QueueAudio(Selected(),"get");FocusStream();
  }
  void StartAudio(Device d,Session session) {
   if(session.AudioProcess!=null||session.Viewer==null)return;
   session.AudioVolumeReady=false;var p=new Process();
   p.StartInfo=new ProcessStartInfo(Path.Combine(runtime,"scrcpy.exe"),StreamOptions.AudioArguments(d,session.AlarmActive)){WorkingDirectory=runtime,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
   p.StartInfo.EnvironmentVariables["ADB"]=Path.Combine(runtime,"adb.exe");
   p.OutputDataReceived+=delegate{};p.ErrorDataReceived+=delegate{};
   p.EnableRaisingEvents=true;
   p.Exited+=delegate{Dispatch(delegate{if(session.AudioProcess!=p)return;session.AudioProcess=null;int code=p.ExitCode;p.Dispose();if(code!=0)Error("PC音声出力の開始失敗");});};
   try{session.AudioProcess=p;p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();}
   catch{session.AudioProcess=null;p.Dispose();Error("PC音声出力の開始失敗");}
  }
  void StopAudio(Session session) {
   var p=session.AudioProcess;session.AudioProcess=null;
   if(p!=null){try{if(!p.HasExited){p.Kill();p.WaitForExit(3000);}}catch{}p.Dispose();}
  }
  void VolumeStep(int direction) {
   var d=Selected();if(d==null||d.Serial!=audioSerial)return;
   int value=Native.SendMessage(controls[13],0x0400,IntPtr.Zero,IntPtr.Zero).ToInt32();
   value=Math.Max(audioMin,Math.Min(audioMax,value+direction));
   Native.SendMessage(controls[13],0x0405,new IntPtr(1),new IntPtr(value));
   QueueAudio(d,value.ToString());FocusStream();
  }
  void QueueAudio(Device d,string request) {
   if(d==null||d.State!="device"||exiting)return;
   Session current;bool alarm=sessions.TryGetValue(d.Serial,out current)&&current.AlarmActive;
   if(Program.PcAudio||alarm) {
    Session session;if(!sessions.TryGetValue(d.Serial,out session)||session.AudioProcess==null)return;
    try{
     if(!session.AudioVolumeReady)session.AudioVolumeReady=PcPlayback.NormalizeSession(session.AudioProcess.Id);
     int value=PcPlayback.MasterVolume(request=="get"?(int?)null:int.Parse(request));
     audioSerial=d.Serial;audioMin=0;audioMax=100;
     Native.SendMessage(controls[13],0x0406,new IntPtr(1),new IntPtr(100<<16));
     Native.SendMessage(controls[13],0x0405,new IntPtr(1),new IntPtr(value));
     foreach(int id in new[]{11,12,13})Native.EnableWindow(controls[id],true);
    }catch(Exception){Error("PC音量の取得・設定失敗");}
    return;
   }
   lock(audioSync) {
    if(request=="get"&&audioBusy)return;
    audioTarget=d;audioRequest=request;audioVersion++;
    if(audioBusy)return;audioBusy=true;
   }
   Task.Run(delegate {
    while(true) {
     Device target;string mode;long version;
     lock(audioSync) {
      if(audioRequest==null){audioBusy=false;return;}
      target=audioTarget;mode=audioRequest;version=audioVersion;audioRequest=null;
     }
     try {
      string prefix="-s "+DeviceParser.Quote(target.Serial)+" ";
      if(!audioReady.Contains(target.Serial)) {
       var push=Adb(prefix+"push "+DeviceParser.Quote(Path.Combine(runtime,"phonedesk-power.jar"))+" /data/local/tmp/phonedesk-power.jar");
       if(push.Code!=0)throw new InvalidOperationException("音量操作の準備失敗");
       audioReady.Add(target.Serial);
      }
      var result=Adb(prefix+"shell CLASSPATH=/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.AudioControl "+mode);
      if(result.Code!=0)throw new InvalidOperationException("音量操作失敗");
      var state=VolumeState.Parse(result.Output);
      Dispatch(delegate {
       lock(audioSync){if(version!=audioVersion)return;}
       var selected=Selected();if(selected==null||selected.Serial!=target.Serial)return;
       audioSerial=target.Serial;audioMin=state.Min;audioMax=state.Max;
       Native.SendMessage(controls[13],0x0406,new IntPtr(1),new IntPtr((state.Max<<16)|state.Min));
       Native.SendMessage(controls[13],0x0405,new IntPtr(1),new IntPtr(state.Value));
       foreach(int id in new[]{11,12,13})Native.EnableWindow(controls[id],true);
      });
     }catch(Exception ex){Dispatch(delegate{Error(ex.Message);});}
    }
   });
  }
 }
}
