// SPDX-License-Identifier: Apache-2.0
namespace PhoneDesk {
using System;using System.Runtime.InteropServices;
[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]class PcDeviceEnumerator{}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface PcEnumerator{
 void EnumAudioEndpoints(int flow,uint state,out IntPtr list);void GetDefaultAudioEndpoint(int flow,int role,out PcDevice device);
}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface PcDevice{
 void Activate(ref Guid iid,uint ctx,IntPtr param,[MarshalAs(UnmanagedType.IUnknown)]out object value);
}
[ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface PcSessions{
 void GetAudioSessionControl(IntPtr g,uint flags,out IntPtr c);void GetSimpleAudioVolume(IntPtr g,uint flags,out IntPtr c);void GetSessionEnumerator(out PcSessionList list);
}
[ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface PcSessionList{void GetCount(out int count);void GetSession(int index,out PcSession session);}
[ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface PcSession{
 void GetState(out int state);void GetDisplayName(out IntPtr s);void SetDisplayName(IntPtr s,IntPtr g);void GetIconPath(out IntPtr s);void SetIconPath(IntPtr s,IntPtr g);void GetGroupingParam(out Guid g);void SetGroupingParam(ref Guid g,IntPtr ctx);void RegisterAudioSessionNotification(IntPtr p);void UnregisterAudioSessionNotification(IntPtr p);void GetSessionIdentifier(out IntPtr s);void GetSessionInstanceIdentifier(out IntPtr s);void GetProcessId(out uint pid);
}
[ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface PcSimpleVolume{
 void SetMasterVolume(float value,ref Guid context);void GetMasterVolume(out float value);void SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);void GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
}
[ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface PcEndpointVolume{
 void RegisterControlChangeNotify(IntPtr callback);void UnregisterControlChangeNotify(IntPtr callback);void GetChannelCount(out uint count);
 void SetMasterVolumeLevel(float value,ref Guid context);void SetMasterVolumeLevelScalar(float value,ref Guid context);
 void GetMasterVolumeLevel(out float value);void GetMasterVolumeLevelScalar(out float value);
}
static class PcPlayback {
 internal static int MasterVolume(int? target){
  object enumerator=null,device=null,volume=null;
  try{
   enumerator=new PcDeviceEnumerator();PcDevice endpoint;((PcEnumerator)enumerator).GetDefaultAudioEndpoint(0,0,out endpoint);device=endpoint;
   Guid iid=typeof(PcEndpointVolume).GUID;endpoint.Activate(ref iid,23,IntPtr.Zero,out volume);
   var control=(PcEndpointVolume)volume;Guid context=Guid.Empty;
   if(target.HasValue)control.SetMasterVolumeLevelScalar(Math.Max(0,Math.Min(100,target.Value))/100f,ref context);
   float value;control.GetMasterVolumeLevelScalar(out value);return (int)Math.Round(value*100);
  }finally{foreach(var obj in new object[]{volume,device,enumerator})if(obj!=null)Marshal.ReleaseComObject(obj);}
 }
 internal static bool NormalizeSession(int pid){return SessionVolume(pid,100).HasValue;}

 static int? SessionVolume(int pid,int? target) {
  object enumerator=null,device=null,manager=null;PcSessionList list=null;
  try{
   enumerator=new PcDeviceEnumerator();PcDevice endpoint;((PcEnumerator)enumerator).GetDefaultAudioEndpoint(0,0,out endpoint);device=endpoint;
   Guid iid=typeof(PcSessions).GUID;endpoint.Activate(ref iid,23,IntPtr.Zero,out manager);
   ((PcSessions)manager).GetSessionEnumerator(out list);int count;list.GetCount(out count);
   for(int i=0;i<count;i++){
    PcSession session;list.GetSession(i,out session);
    try{uint owner;session.GetProcessId(out owner);if(owner!=(uint)pid)continue;
     var volume=(PcSimpleVolume)session;Guid context=Guid.Empty;
     if(target.HasValue){volume.SetMasterVolume(Math.Max(0,Math.Min(100,target.Value))/100f,ref context);volume.SetMute(target.Value==0,ref context);}
     float value;bool mute;volume.GetMasterVolume(out value);volume.GetMute(out mute);return mute?0:(int)Math.Round(value*100);
    }finally{Marshal.ReleaseComObject(session);}
   }
   return null;
  }finally{foreach(var obj in new object[]{list,manager,device,enumerator})if(obj!=null)Marshal.ReleaseComObject(obj);}
 }
}

}
