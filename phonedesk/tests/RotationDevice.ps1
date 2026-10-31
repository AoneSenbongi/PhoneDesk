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
$Adb="$env:LOCALAPPDATA/PhoneDesk/runtime/adb.exe"
$app=Get-Process PhoneDesk
[ViewerDevice]::Find($app.Id)
$original=(& $Adb shell wm user-rotation).Trim()
$originalFixed=(& $Adb shell wm fixed-to-user-rotation).Trim()
function Assert-Aspect($landscape,$details,$name){
 Start-Sleep -Seconds 2
 [ViewerDevice]::Find($app.Id)
 $r=New-Object ViewerDevice+Rect
 [ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$r)|Out-Null
 $w=$r.R-$(if($details){320}else{0});$h=$r.B-$(if($details){0}else{44})
 $ratio=if($landscape){2412.0/1084}else{1084.0/2412}
 if([Math]::Abs($w-$h*$ratio) -gt 4){throw "${name}: aspect mismatch ${w}x${h}"}
 "PASS: $name (${w}x${h})"
}
try{
 & $Adb shell wm fixed-to-user-rotation enabled|Out-Null
 & $Adb shell wm user-rotation lock 0|Out-Null
 Start-Sleep -Seconds 2
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x112,[IntPtr]0xF120,[IntPtr]::Zero)|Out-Null
 $initial=New-Object ViewerDevice+Rect;$streamRect=New-Object ViewerDevice+Rect
 [ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$initial)|Out-Null
 [ViewerDevice]::GetClientRect([ViewerDevice]::Child,[ref]$streamRect)|Out-Null
 if($initial.R-$streamRect.R -gt 200){[ViewerDevice]::SendMessage([ViewerDevice]::Host,0x111,[IntPtr]10,[IntPtr]::Zero)|Out-Null}
 & $Adb shell wm user-rotation lock 1|Out-Null
 Assert-Aspect $true $false 'real landscape frame resizes shell'
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x112,[IntPtr]0x1F00,[IntPtr]::Zero)|Out-Null
 Assert-Aspect $true $true 'landscape left details'
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x112,[IntPtr]0xF030,[IntPtr]::Zero)|Out-Null
 & $Adb shell wm user-rotation lock 0|Out-Null
 Assert-Aspect $false $true 'maximized returns portrait'
 & $Adb shell wm user-rotation lock 1|Out-Null
 Assert-Aspect $true $true 'maximized landscape'
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x112,[IntPtr]0xF120,[IntPtr]::Zero)|Out-Null
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x111,[IntPtr]10,[IntPtr]::Zero)|Out-Null
 Assert-Aspect $true $false 'collapse landscape details'
}finally{
 if($originalFixed -match "^(enabled|disabled|default|enabled_if_no_auto_rotation)$"){& $Adb shell wm fixed-to-user-rotation $originalFixed|Out-Null}
 if($original -match '^lock (\d)'){& $Adb shell wm user-rotation lock $Matches[1]|Out-Null}else{& $Adb shell wm user-rotation free|Out-Null}
}
