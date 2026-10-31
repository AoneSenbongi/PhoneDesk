param([string]$Adb = "$env:LOCALAPPDATA\PhoneDesk\runtime\adb.exe",[Parameter(Mandatory=$true)][string]$Serial)
$ErrorActionPreference='Stop'
function Start-Off {
    $p=New-Object Diagnostics.Process
    $p.StartInfo=New-Object Diagnostics.ProcessStartInfo
    $p.StartInfo.FileName=$Adb
    $p.StartInfo.Arguments="-s $Serial shell -T CLASSPATH=/data/local/tmp/phonedesk-scrcpy.jar:/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.ScreenPower watch-off"
    $p.StartInfo.UseShellExecute=$false
    $p.StartInfo.CreateNoWindow=$true
    $p.StartInfo.RedirectStandardInput=$true
    $p.StartInfo.RedirectStandardOutput=$true
    $p.StartInfo.RedirectStandardError=$true
    [void]$p.Start()
    $ready=$p.StandardOutput.ReadLineAsync()
    if(!$ready.Wait(8000) -or $ready.Result -ne 'OK watch-off'){throw 'Off guardian did not start'}
    return $p
}
function Assert-Physical([string]$Mode) {
    $dump=(& $Adb -s $Serial shell dumpsys SurfaceFlinger) -join "`n"
    $physical=[regex]::Match($dump,'(?m)^Display (\d+) \(HWC display \d+\):').Groups[1].Value
    if(!$physical -or $dump -notmatch ('(?m)^Display '+$physical+'\r?\n(?:[ \t]+[^\n]*\n)*?[ \t]+powerMode='+$Mode+'\b')){throw "Physical display is not $Mode"}
}
try {
    $p=Start-Off
    Assert-Physical Off
    $p.StandardInput.Close()
    if(!$p.WaitForExit(8000)){throw 'EOF did not end guardian'}
    Start-Sleep -Seconds 1
    Assert-Physical On
    'PASS: loss of ADB input restores screen power'
    $p=Start-Off
    $p.Kill();$p.WaitForExit()
    Start-Sleep -Seconds 2
    Assert-Physical On
    'PASS: abrupt ADB client exit restores screen power'
    $p=Start-Off
    $p.StandardInput.Write('0');$p.StandardInput.Flush()
    if(!$p.WaitForExit(8000)){throw 'Guardian cancellation failed'}
    Assert-Physical Off
    'PASS: cancellation preserves screen off for lock'
} finally {
    if($p -and !$p.HasExited){$p.StandardInput.Close();$p.WaitForExit(8000)|Out-Null}
    & $Adb -s $Serial shell 'CLASSPATH=/data/local/tmp/phonedesk-scrcpy.jar:/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.ScreenPower on'
}
