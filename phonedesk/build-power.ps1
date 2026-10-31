param([Parameter(Mandatory=$true)][string]$AndroidSdk,[Parameter(Mandatory=$true)][string]$JavaHome)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$env:JAVA_HOME=$JavaHome
$classes=Join-Path $root 'build\power-classes'
New-Item -ItemType Directory -Path $classes,(Join-Path $root 'runtime-assets') -Force | Out-Null
& "$JavaHome\bin\javac.exe" --release 8 -d $classes "$root\src\ScreenPower.java" "$root\src\AudioControl.java" "$root\src\ClipboardWatch.java" "$root\src\RotationControl.java"
if($LASTEXITCODE -ne 0){throw 'Power helper compilation failed'}
& "$AndroidSdk\build-tools\36.0.0\d8.bat" --min-api 26 --output "$root\runtime-assets\phonedesk-power.jar" "$classes\com\phonedesk\ScreenPower.class" "$classes\com\phonedesk\AudioControl.class" "$classes\com\phonedesk\ClipboardWatch.class" "$classes\com\phonedesk\ClipboardWatch`$1.class" "$classes\com\phonedesk\ClipboardWatch`$2.class" "$classes\com\phonedesk\RotationControl.class"
if($LASTEXITCODE -ne 0){throw 'Power helper dex compilation failed'}
