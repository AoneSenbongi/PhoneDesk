param([switch]$Text,[Parameter(Mandatory=$true)][string]$Serial,[string]$Adb="$env:LOCALAPPDATA\PhoneDesk\runtime\adb.exe")
$ErrorActionPreference='Stop'
Add-Type @'
using System;using System.Text;using System.Runtime.InteropServices;
public static class BackspaceDevice {
 public delegate bool EnumProc(IntPtr h,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc p,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumChildWindows(IntPtr h,EnumProc p,IntPtr l);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")]static extern uint MapVirtualKey(uint key,uint map);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 public static IntPtr Find(int pid){IntPtr result=IntPtr.Zero;EnumProc scan=(h,l)=>{uint p;GetWindowThreadProcessId(h,out p);var b=new StringBuilder(128);GetClassName(h,b,128);if(p==pid&&b.ToString()=="SDL_app")result=h;return true;};EnumWindows((h,l)=>{scan(h,l);EnumChildWindows(h,scan,l);return true;},IntPtr.Zero);return result;}
 public static void Key(IntPtr h,int key,bool up,bool repeat=false){long flags=1L|((long)MapVirtualKey((uint)key,0)<<16);if(up)flags|=0xc0000000L;else if(repeat)flags|=0x40000000L;PostMessage(h,up?0x101u:0x100u,new IntPtr(key),new IntPtr(flags));}
}
'@
$state=(& $Adb -s $Serial get-state 2>$null)-join ''
if($state -ne 'device'){throw 'Target is not connected to ADB'}
$policy=(& $Adb -s $Serial shell dumpsys window policy)-join "`n"
if($policy -notmatch 'mKeyguardUnlocked=\s*true'){throw 'Unlock required; no credentials are entered by this test'}
$processes=Get-CimInstance Win32_Process -Filter "name='scrcpy.exe'" | Where-Object {$_.CommandLine -match '--keyboard=uhid' -and $_.CommandLine -match [regex]::Escape($Serial)}
$stream=@($processes)[0];if(!$stream){throw 'UHID viewer not found'}
$child=[BackspaceDevice]::Find($stream.ProcessId);if($child -eq [IntPtr]::Zero){throw 'SDL window not found'}
function Read-Length {
 & $Adb -s $Serial shell uiautomator dump --windows /data/local/tmp/phonedesk-backspace.xml | Out-Null
 [xml]$ui=(& $Adb -s $Serial shell cat /data/local/tmp/phonedesk-backspace.xml)-join "`n"
 $editor=$ui.SelectSingleNode('//node[@content-desc="fixture-editor"]');if(!$editor){throw 'Dedicated editor missing'}
 return $editor.text.Length
}
try {
 & $Adb -s $Serial shell am force-stop jp.aonesenbongi.meltype.hardwarefixture | Out-Null
 & $Adb -s $Serial shell am start -W -n jp.aonesenbongi.meltype.hardwarefixture/.ConversionActivity --ez numeric $(!$Text) | Out-Null
 Start-Sleep -Milliseconds 1200
 for($i=0;$i -lt 30;$i++){[BackspaceDevice]::Key($child,$(if($Text){65}else{49}),$false);[BackspaceDevice]::Key($child,$(if($Text){65}else{49}),$true);Start-Sleep -Milliseconds 30}
 [BackspaceDevice]::Key($child,13,$false);[BackspaceDevice]::Key($child,13,$true)
 Start-Sleep -Milliseconds 1000
 $before=Read-Length
 if($before -lt 25){throw "Fixture setup failed (length=$before)"}
 [BackspaceDevice]::Key($child,8,$false)
 Start-Sleep -Milliseconds 1200
 for($i=0;$i -lt 20;$i++){[BackspaceDevice]::Key($child,8,$false,$true);Start-Sleep -Milliseconds 50}
 [BackspaceDevice]::Key($child,8,$true)
 Start-Sleep -Milliseconds 300
 $after=Read-Length
 if($after -ne [Math]::Max(0,$before-21)){throw "Held Backspace did not repeat (before=$before, after=$after)"}
 Start-Sleep -Milliseconds 500
 if((Read-Length) -ne $after){throw 'Deletion continued after release'}
 "PASS: held Backspace deletes repeatedly and stops on release ($before -> $after)"
}finally{
 [BackspaceDevice]::Key($child,8,$true)
 & $Adb -s $Serial shell am force-stop jp.aonesenbongi.meltype.hardwarefixture | Out-Null
 & $Adb -s $Serial shell rm -f /data/local/tmp/phonedesk-backspace.xml | Out-Null
 & $Adb -s $Serial shell input keyevent KEYCODE_HOME | Out-Null
}
