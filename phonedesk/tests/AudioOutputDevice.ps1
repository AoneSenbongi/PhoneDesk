param([string]$Adb="$env:LOCALAPPDATA\PhoneDesk\runtime\adb.exe",[Parameter(Mandatory=$true)][string]$Serial,[switch]$LifecycleOnly)
$ErrorActionPreference='Stop'
Add-Type @'
using System;using System.Text;using System.Runtime.InteropServices;
public class ViewerDevice {
public delegate bool EnumProc(IntPtr h,IntPtr l);
[StructLayout(LayoutKind.Sequential)]public struct Rect{public int L,T,R,B;}
[DllImport("user32.dll")]public static extern bool EnumWindows(EnumProc p,IntPtr l);
[DllImport("user32.dll")]public static extern bool EnumChildWindows(IntPtr h,EnumProc p,IntPtr l);
[DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
[DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern int GetWindowText(IntPtr h,StringBuilder b,int n);
[DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern int GetClassName(IntPtr h,StringBuilder b,int n);
[DllImport("user32.dll")]public static extern IntPtr GetParent(IntPtr h);
[DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr h,out Rect r);
[DllImport("user32.dll")]public static extern bool GetClientRect(IntPtr h,out Rect r);
[DllImport("user32.dll")]public static extern bool IsWindowVisible(IntPtr h);
[DllImport("user32.dll")]public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int z,uint flags);
[DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
[DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
[DllImport("user32.dll")]public static extern uint MapVirtualKey(uint code,uint type);
public static IntPtr Host,Child,Panel,Container;
public static int TopLevels;
public static void Find(int pid){Host=Child=Panel=Container=IntPtr.Zero;TopLevels=0;EnumWindows((h,l)=>{uint p;GetWindowThreadProcessId(h,out p);if(p==pid){var b=new StringBuilder(256);GetWindowText(h,b,256);if(b.ToString().StartsWith("PhoneDesk | ")||b.ToString()=="PhoneDesk"){Host=h;Panel=h;TopLevels++;}}return true;},IntPtr.Zero);if(Host!=IntPtr.Zero)EnumChildWindows(Host,(h,l)=>{var b=new StringBuilder(256);GetClassName(h,b,256);if(b.ToString()=="SDL_app"){Child=h;Container=GetParent(h);}return true;},IntPtr.Zero);}
public static void Keys(string text){foreach(char c in text){uint key=(uint)char.ToUpperInvariant(c);uint scan=MapVirtualKey(key,0);PostMessage(Child,0x100,new IntPtr(key),new IntPtr(1L|(scan<<16)));PostMessage(Child,0x101,new IntPtr(key),new IntPtr(0xc0000001L|(scan<<16)));System.Threading.Thread.Sleep(100);}}
}
'@
$app=Get-Process PhoneDesk
for($ready=0;$ready -lt 100;$ready++){
 [ViewerDevice]::Find($app.Id)
 if([ViewerDevice]::Child -ne [IntPtr]::Zero){break}
 Start-Sleep -Milliseconds 200
}
if([ViewerDevice]::Child -eq [IntPtr]::Zero){throw 'Initial video is not ready'}
$hostWindow=[ViewerDevice]::Host
$root=New-Object ViewerDevice+Rect;$video=New-Object ViewerDevice+Rect
[ViewerDevice]::GetClientRect($hostWindow,[ref]$root)|Out-Null
[ViewerDevice]::GetClientRect([ViewerDevice]::Child,[ref]$video)|Out-Null
if($root.R-$video.R -lt 200){[ViewerDevice]::SendMessage($hostWindow,0x112,[IntPtr]0x1F00,[IntPtr]::Zero)|Out-Null}
$script:check=[IntPtr]::Zero
[ViewerDevice]::EnumChildWindows($hostWindow,{param($h,$l);$b=[Text.StringBuilder]::new(100);[ViewerDevice]::GetWindowText($h,$b,100)|Out-Null;if($b.ToString() -eq 'PCから音を出す'){$script:check=$h};return $true},[IntPtr]::Zero)|Out-Null
if($script:check -eq [IntPtr]::Zero){throw 'PC audio switch missing'}
$initialChild=[ViewerDevice]::Child
$videoProcess=Get-CimInstance Win32_Process -Filter "name='scrcpy.exe'" | Where-Object {$_.ExecutablePath -like "$env:LOCALAPPDATA\PhoneDesk\*" -and $_.CommandLine -like '*--keyboard=uhid*'}
$videoPid=$videoProcess.ProcessId
function Wait-Output($enabled){
 for($i=0;$i -lt 50;$i++){
  Start-Sleep -Milliseconds 200
  [ViewerDevice]::Find($app.Id)
  $all=Get-CimInstance Win32_Process -Filter "name='scrcpy.exe'" | Where-Object {$_.ExecutablePath -like "$env:LOCALAPPDATA\PhoneDesk\*"}
  $video=$all | Where-Object {$_.CommandLine -like '*--keyboard=uhid*'}
  $audio=$all | Where-Object {$_.CommandLine -like '*--no-window*'}
  if([bool]$audio -eq $enabled){break}
 }
 if([bool]$audio -ne $enabled){throw 'Audio-only output did not switch'}
 if(!$video -or $video.ProcessId -ne $videoPid -or [ViewerDevice]::Child -ne $initialChild){throw "Audio toggle reconnected video or UHID: PID $videoPid -> $($video.ProcessId), HWND $initialChild -> $([ViewerDevice]::Child)"}
 if([ViewerDevice]::Host -ne $hostWindow){throw 'Audio setting opened a different shell'}
 $saved=(Get-ItemProperty HKCU:\Software\PhoneDesk).PcAudio
 if([bool]$saved -ne $enabled){throw 'Audio setting not saved'}
 "PASS: audio=$enabled, video PID and SDL HWND unchanged, preference saved"
}
try{
 [ViewerDevice]::SendMessage($script:check,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null
 Wait-Output $false
}finally{
 if((Get-ItemProperty HKCU:\Software\PhoneDesk).PcAudio -eq 0){[ViewerDevice]::SendMessage($script:check,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null}
 Wait-Output $true
}
