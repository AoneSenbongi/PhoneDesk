// SPDX-License-Identifier: Apache-2.0
using System;using System.Runtime.InteropServices;using System.Diagnostics;using System.Threading;
[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]class DeviceEnumerator{}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Enumerator{
 void EnumAudioEndpoints(int flow,uint state,out IntPtr list);void GetDefaultAudioEndpoint(int flow,int role,out Device device);
}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Device{
 void Activate(ref Guid iid,uint ctx,IntPtr param,[MarshalAs(UnmanagedType.IUnknown)]out object value);
}
[ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Sessions{
 void GetAudioSessionControl(IntPtr g,uint flags,out IntPtr c);void GetSimpleAudioVolume(IntPtr g,uint flags,out IntPtr c);void GetSessionEnumerator(out SessionList list);
}
[ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface SessionList{void GetCount(out int count);void GetSession(int index,out Session session);}
[ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Session{
 void GetState(out int state);void GetDisplayName(out IntPtr s);void SetDisplayName(IntPtr s,IntPtr g);void GetIconPath(out IntPtr s);void SetIconPath(IntPtr s,IntPtr g);void GetGroupingParam(out Guid g);void SetGroupingParam(ref Guid g,IntPtr ctx);void RegisterAudioSessionNotification(IntPtr p);void UnregisterAudioSessionNotification(IntPtr p);void GetSessionIdentifier(out IntPtr s);void GetSessionInstanceIdentifier(out IntPtr s);void GetProcessId(out uint pid);
}
[ComImport,Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Meter{void GetPeakValue(out float value);}
class AudioPlaybackProbe{
 static int Main(string[] args){uint target=uint.Parse(args[0]);int seconds=args.Length>1?int.Parse(args[1]):5;float max=0;bool found=false;
 var enumerator=(Enumerator)new DeviceEnumerator();Device device;enumerator.GetDefaultAudioEndpoint(0,0,out device);Guid iid=typeof(Sessions).GUID;object obj;device.Activate(ref iid,23,IntPtr.Zero,out obj);var sessions=(Sessions)obj;
 for(int tick=0;tick<seconds*20;tick++){SessionList list;sessions.GetSessionEnumerator(out list);int count;list.GetCount(out count);for(int i=0;i<count;i++){Session session;list.GetSession(i,out session);uint pid;session.GetProcessId(out pid);if(pid==target){found=true;float peak;((Meter)session).GetPeakValue(out peak);max=Math.Max(max,peak);}Marshal.ReleaseComObject(session);}Marshal.ReleaseComObject(list);Thread.Sleep(50);}
 Console.WriteLine("PC_AUDIO session="+found+" peak="+max.ToString(System.Globalization.CultureInfo.InvariantCulture));return found&&max>0.00001?0:1;
 }}
