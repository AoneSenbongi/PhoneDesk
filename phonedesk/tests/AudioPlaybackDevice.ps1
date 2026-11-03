param([string]$AndroidSdk=$env:ANDROID_HOME,[string]$JavaHome=$env:JAVA_HOME)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$env:JAVA_HOME=$JavaHome
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo "/out:$root\build\AudioPlaybackProbe.exe" "$PSScriptRoot\AudioPlaybackProbe.cs"
if($LASTEXITCODE -ne 0){throw 'Windows audio probe compilation failed'}
& "$JavaHome/bin/javac.exe" --release 8 -cp "$AndroidSdk/platforms/android-36/android.jar" -d "$root/build" "$PSScriptRoot/TestTone.java"
if($LASTEXITCODE -ne 0){throw 'Tone compilation failed'}
& "$AndroidSdk/build-tools/36.0.0/d8.bat" --min-api 26 --output "$root/build/test-tone.jar" "$root/build/TestTone.class"
if($LASTEXITCODE -ne 0){throw 'Tone dex compilation failed'}
$adb="$env:LOCALAPPDATA/PhoneDesk/runtime/adb.exe"
& $adb push "$root/build/test-tone.jar" /data/local/tmp/phonedesk-test-tone.jar
$stream=Get-CimInstance Win32_Process -Filter "name='scrcpy.exe'" | Where-Object {$_.ExecutablePath -like "$env:LOCALAPPDATA\PhoneDesk\*" -and $_.CommandLine -like "*--no-window*"}
$tone=Start-Process $adb -ArgumentList 'shell CLASSPATH=/data/local/tmp/phonedesk-test-tone.jar app_process / TestTone' -WindowStyle Hidden -RedirectStandardOutput "$root/build/test-tone.out" -RedirectStandardError "$root/build/test-tone.err" -PassThru
& "$root/build/AudioPlaybackProbe.exe" $stream.ProcessId 5
if($LASTEXITCODE -ne 0){throw 'Phone audio did not reach Windows output session'}
$tone.WaitForExit()
if($tone.ExitCode -ne 0){throw 'Android tone failed'}
