$ErrorActionPreference='Stop'
$adb="$env:LOCALAPPDATA/PhoneDesk/runtime/adb.exe"
$original=(& $adb shell CLASSPATH=/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.AudioControl get)-join ''
if($original -notmatch 'VOLUME (\d+)'){throw 'Initial Android volume unavailable'}
$level=[int]$Matches[1]
try{
 & $adb shell CLASSPATH=/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.AudioControl 0
 & "$PSScriptRoot/AudioPlaybackDevice.ps1"
 if($LASTEXITCODE -ne 0){throw 'PC playback depends on Android volume'}
}finally{
 & $adb shell CLASSPATH=/data/local/tmp/phonedesk-power.jar app_process / com.phonedesk.AudioControl $level
}
