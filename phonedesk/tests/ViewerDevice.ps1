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
$app=Get-Process PhoneDesk | Where-Object {$_.Path -eq "$env:LOCALAPPDATA\PhoneDesk\PhoneDesk.exe"}
[ViewerDevice]::Find($app.Id)
if([ViewerDevice]::Host -eq [IntPtr]::Zero -or [ViewerDevice]::Child -eq [IntPtr]::Zero){throw 'Embedded stream not found'}
if([ViewerDevice]::GetParent([ViewerDevice]::Container) -ne [ViewerDevice]::Host -or [ViewerDevice]::TopLevels -ne 1){throw 'Stream is not in the sole PhoneDesk shell'}
'PASS: real SDL stream embedded in the only PhoneDesk window'
$initialClient=New-Object ViewerDevice+Rect;$initialStream=New-Object ViewerDevice+Rect
[ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$initialClient)|Out-Null
[ViewerDevice]::GetWindowRect([ViewerDevice]::Child,[ref]$initialStream)|Out-Null
if($initialClient.R-($initialStream.R-$initialStream.L) -gt 200){[ViewerDevice]::SendMessage([ViewerDevice]::Host,0x0111,[IntPtr]10,[IntPtr]::Zero)|Out-Null}
$original=New-Object ViewerDevice+Rect
[ViewerDevice]::GetWindowRect([ViewerDevice]::Host,[ref]$original)|Out-Null
try {
 $resize=New-Object ViewerDevice+Rect
 $resize.L=240;$resize.T=100;$resize.R=600;$resize.B=900
 $sizing=[Runtime.InteropServices.Marshal]::AllocHGlobal(16)
 try {
  [Runtime.InteropServices.Marshal]::StructureToPtr($resize,$sizing,$false)
  [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x0214,[IntPtr]2,$sizing)|Out-Null
  $resize=[Runtime.InteropServices.Marshal]::PtrToStructure($sizing,[type][ViewerDevice+Rect])
 }finally{[Runtime.InteropServices.Marshal]::FreeHGlobal($sizing)}
 [ViewerDevice]::SetWindowPos([ViewerDevice]::Host,[IntPtr]::Zero,$resize.L,$resize.T,$resize.R-$resize.L,$resize.B-$resize.T,0x0014)|Out-Null
 Start-Sleep -Milliseconds 500
 $client=New-Object ViewerDevice+Rect;$stream=New-Object ViewerDevice+Rect
 [ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$client)|Out-Null
 [ViewerDevice]::GetWindowRect([ViewerDevice]::Child,[ref]$stream)|Out-Null
 if($stream.R-$stream.L -ne $client.R -or $stream.B-$stream.T -ne $client.B-44){throw 'Real SDL stream resize mismatch'}
 'PASS: real SDL stream follows host resize'
 $beforeExpand=New-Object ViewerDevice+Rect
 [ViewerDevice]::GetWindowRect([ViewerDevice]::Child,[ref]$beforeExpand)|Out-Null
 [ViewerDevice]::SendMessage([ViewerDevice]::Container,0x0111,[IntPtr]23,[IntPtr]::Zero)|Out-Null
 $expanded=New-Object ViewerDevice+Rect;$expandedStream=New-Object ViewerDevice+Rect
 [ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$expanded)|Out-Null
 [ViewerDevice]::GetWindowRect([ViewerDevice]::Child,[ref]$expandedStream)|Out-Null
 $expandedContainer=New-Object ViewerDevice+Rect
 [ViewerDevice]::GetWindowRect([ViewerDevice]::Container,[ref]$expandedContainer)|Out-Null
 if($expandedStream.B-$expandedStream.T -ne $expanded.B -or $expandedStream.R-$expandedStream.L -ne $expanded.R-320 -or $expandedStream.L -ne $expandedContainer.L+320){throw 'Details did not expand left'}
 if($beforeExpand.R-$beforeExpand.L -ne $expandedStream.R-$expandedStream.L -or $beforeExpand.B-$beforeExpand.T -ne $expandedStream.B-$expandedStream.T){throw 'Opening details shrank the stream'}
 if([ViewerDevice]::Host -ne [ViewerDevice]::Panel){throw 'Details opened another shell'}
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x0111,[IntPtr]10,[IntPtr]::Zero)|Out-Null
 'PASS: details expands and collapses inside the same window'
 if($LifecycleOnly) {
  $originalShell=[ViewerDevice]::Host
  [uint32]$streamPid=0
  [ViewerDevice]::GetWindowThreadProcessId([ViewerDevice]::Child,[ref]$streamPid)|Out-Null
  Stop-Process -Id $streamPid -Force
  Start-Sleep -Seconds 1
  [ViewerDevice]::Find($app.Id)
  if([ViewerDevice]::Host -ne $originalShell -or [ViewerDevice]::Child -ne [IntPtr]::Zero -or [ViewerDevice]::TopLevels -ne 1){throw 'Stream stop did not return to original control panel'}
  'PASS: stream stop returns to control panel in original HWND'
  [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x0111,[IntPtr]2,[IntPtr]::Zero)|Out-Null
  for($i=0;$i -lt 40;$i++){Start-Sleep -Milliseconds 250;[ViewerDevice]::Find($app.Id);if([ViewerDevice]::Child -ne [IntPtr]::Zero){break}}
  if([ViewerDevice]::Host -ne $originalShell -or [ViewerDevice]::Child -eq [IntPtr]::Zero -or [ViewerDevice]::TopLevels -ne 1){throw 'Stream restart opened a different shell'}
  'PASS: stream restart embeds into the same original HWND'
  return
 }
 # Use the dedicated no-learning fixture only, never a personal app.
 $policy=(& $Adb -s $Serial shell dumpsys window policy)-join "`n"
 if($policy -notmatch 'mKeyguardUnlocked=\s*true'){throw 'Device is locked; do not type credentials'}
 & $Adb -s $Serial shell am force-stop jp.aonesenbongi.meltype.hardwarefixture | Out-Null
 & $Adb -s $Serial shell am start --display 0 -W -n jp.aonesenbongi.meltype.hardwarefixture/.ConversionActivity --ez text true | Out-Null
 & $Adb -s $Serial shell uiautomator dump --windows /sdcard/phonedesk-viewer-test.xml | Out-Null
 [xml]$ui=(& $Adb -s $Serial shell cat /sdcard/phonedesk-viewer-test.xml)-join "`n"
 $editor=$ui.SelectSingleNode('//node[@content-desc="fixture-editor"]')
 $bounds=[regex]::Matches($editor.bounds,'\d+') | ForEach-Object {[int]$_.Value}
 & $Adb -s $Serial shell input -d 0 tap ([int](($bounds[0]+$bounds[2])/2)) ([int](($bounds[1]+$bounds[3])/2))
 Start-Sleep -Milliseconds 500
 [ViewerDevice]::Keys('nihongo')
 Start-Sleep -Seconds 1
 & $Adb -s $Serial shell uiautomator dump --windows /sdcard/phonedesk-viewer-test.xml | Out-Null
 [xml]$ui=(& $Adb -s $Serial shell cat /sdcard/phonedesk-viewer-test.xml)-join "`n"
 $actual=$ui.SelectSingleNode('//node[@content-desc="fixture-editor"]').text
 if($actual -ne '日本語'){throw "Embedded keyboard route: expected 日本語, got '$actual'"}
 'PASS: embedded SDL window keys -> UHID -> Meltype -> 日本語'
} finally {
 [ViewerDevice]::SetWindowPos([ViewerDevice]::Host,[IntPtr]::Zero,$original.L,$original.T,$original.R-$original.L,$original.B-$original.T,0x0014)|Out-Null
 & $Adb -s $Serial shell am force-stop jp.aonesenbongi.meltype.hardwarefixture | Out-Null
 & $Adb -s $Serial shell input -d 0 keyevent KEYCODE_HOME | Out-Null
}
