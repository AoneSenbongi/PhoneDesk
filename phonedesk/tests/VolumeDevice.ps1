param([Parameter(Mandatory=$true)][string]$Serial)
$ErrorActionPreference='Stop'
$adb="$env:LOCALAPPDATA\PhoneDesk\runtime\adb.exe"
$code=[IO.File]::ReadAllText("$PSScriptRoot\ViewerDevice.ps1")
Invoke-Expression $code.Substring($code.IndexOf("Add-Type @'"),$code.IndexOf('$app=Get-Process')-$code.IndexOf("Add-Type @'"))
$app=Get-Process PhoneDesk | Where-Object {$_.Path -eq "$env:LOCALAPPDATA\PhoneDesk\PhoneDesk.exe"}
[ViewerDevice]::Find($app.Id)
$script:slider=[IntPtr]::Zero
[ViewerDevice]::EnumChildWindows([ViewerDevice]::Host,[ViewerDevice+EnumProc]{param($h,$l)
 $name=New-Object Text.StringBuilder 100
 [ViewerDevice]::GetClassName($h,$name,100)|Out-Null
 if($name.ToString() -eq 'msctls_trackbar32'){$script:slider=$h}
 return $true
},[IntPtr]::Zero)|Out-Null
if($slider -eq [IntPtr]::Zero){throw 'Volume slider missing'}
if(![ViewerDevice]::IsWindowVisible($slider)){[ViewerDevice]::SendMessage([ViewerDevice]::Container,0x0111,[IntPtr]23,[IntPtr]::Zero)|Out-Null}
function Volume {
 $output=(& $adb -s $Serial shell 'CLASSPATH=/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.AudioControl get') -join "`n"
 if($output -notmatch 'VOLUME (\d+) (\d+) (\d+)'){throw 'Volume query failed'}
 return @([int]$Matches[1],[int]$Matches[2],[int]$Matches[3])
}
Start-Sleep -Seconds 3
$initial=Volume
try {
 if([ViewerDevice]::SendMessage($slider,0x0402,[IntPtr]::Zero,[IntPtr]::Zero).ToInt32() -ne $initial[2]){throw 'Slider does not match device range'}
 $first=if($initial[0] -lt $initial[2]){12}else{11}
 $second=if($first -eq 12){11}else{12}
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x0111,[IntPtr]$first,[IntPtr]::Zero)|Out-Null
 Start-Sleep -Seconds 1
 $changed=Volume
 if([Math]::Abs($changed[0]-$initial[0]) -ne 1){throw 'Volume button did not adjust one step'}
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x0111,[IntPtr]$second,[IntPtr]::Zero)|Out-Null
 Start-Sleep -Seconds 1
 if((Volume)[0] -ne $initial[0]){throw 'Opposite button did not restore volume'}
 'PASS: plus/minus buttons adjust phone media volume by one step'
 $target=if($initial[0] -lt $initial[2]){$initial[0]+1}else{$initial[0]-1}
 [ViewerDevice]::SendMessage($slider,0x0405,[IntPtr]1,[IntPtr]$target)|Out-Null
 [ViewerDevice]::SendMessage([ViewerDevice]::Host,0x0114,[IntPtr]4,$slider)|Out-Null
 Start-Sleep -Seconds 1
 if((Volume)[0] -ne $target){throw 'Slider value did not reach phone'}
 'PASS: slider changes phone media volume; range matches physical device'
}finally {
 & $adb -s $Serial shell "CLASSPATH=/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.AudioControl $($initial[0])" | Out-Null
}
