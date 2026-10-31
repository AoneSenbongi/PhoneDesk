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
function Physical-Landscape {
 $info=(& $Adb -s $Serial shell dumpsys display | Select-String 'mOverrideDisplayInfo=.*displayId 0,' | Select-Object -First 1).ToString()
 if($info -notmatch 'real (\d+) x (\d+)'){throw 'Physical display size unavailable'}
 return [int]$Matches[1] -gt [int]$Matches[2]
}
$app=Get-Process PhoneDesk
for($i=0;$i -lt 80;$i++){[ViewerDevice]::Find($app.Id);if([ViewerDevice]::Child -ne [IntPtr]::Zero){break};Start-Sleep -Milliseconds 200}
if([ViewerDevice]::Child -eq [IntPtr]::Zero){throw 'Initial viewer is missing'}
$originalChild=[ViewerDevice]::Child
$r=New-Object ViewerDevice+Rect;$v=New-Object ViewerDevice+Rect
[ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$r)|Out-Null
[ViewerDevice]::GetClientRect([ViewerDevice]::Child,[ref]$v)|Out-Null
if($r.R-$v.R -gt 200){[ViewerDevice]::SendMessage([ViewerDevice]::Host,0x111,[IntPtr]10,[IntPtr]::Zero)|Out-Null}
[ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$r)|Out-Null
$aspect=$r.R/($r.B-44.0)
function Wait-Aspect($expected,$details,$name){
 $okay=$false
 for($i=0;$i -lt 50;$i++){
  Start-Sleep -Milliseconds 100
  [ViewerDevice]::Find($app.Id)
  [ViewerDevice]::GetClientRect([ViewerDevice]::Host,[ref]$r)|Out-Null
  $w=$r.R-$(if($details){320}else{0});$height=$r.B-$(if($details){0}else{44})
  if($height -gt 0 -and [Math]::Abs($w/$height-$expected) -lt 0.02){$okay=$true;break}
 }
 if(!$okay){throw "${name}: frame did not follow display rotation"}
 if((Physical-Landscape) -ne ($expected -gt 1)){throw "${name}: smartphone itself did not rotate"}
 if([ViewerDevice]::Child -ne $originalChild){throw 'Rotation reconnected the stream'}
 Start-Sleep -Milliseconds 700
 "PASS: $name, physical Android display confirmed, same SDL HWND"
}
[ViewerDevice]::SendMessage([ViewerDevice]::Container,0x111,[IntPtr]24,[IntPtr]::Zero)|Out-Null
Wait-Aspect (1/$aspect) $false 'toolbar right rotation'
[ViewerDevice]::SendMessage([ViewerDevice]::Container,0x111,[IntPtr]24,[IntPtr]::Zero)|Out-Null
Wait-Aspect $aspect $false 'toolbar reset'
[ViewerDevice]::SendMessage([ViewerDevice]::Host,0x112,[IntPtr]0x1F00,[IntPtr]::Zero)|Out-Null
[ViewerDevice]::SendMessage([ViewerDevice]::Host,0x111,[IntPtr]15,[IntPtr]::Zero)|Out-Null
Wait-Aspect (1/$aspect) $true 'details right rotation'
[ViewerDevice]::SendMessage([ViewerDevice]::Host,0x111,[IntPtr]15,[IntPtr]::Zero)|Out-Null
Wait-Aspect $aspect $true 'details reset without upside-down rotation'
