$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$package = Join-Path $build 'package-local'
$vendor = Join-Path $build 'vendor\scrcpy-win64-v5.0.1'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$core = @("$root\src\Core.cs")
$app = @(
  "$root\src\Core.cs", "$root\src\NativeApp.cs", "$root\src\NativeApp.Audio.cs",
  "$root\src\NativeApp.Alarm.cs", "$root\src\NativeApp.Rotation.cs",
  "$root\src\NativeApp.Clipboard.cs", "$root\src\PcPlayback.cs",
  "$root\src\AudioSwitch.cs", "$root\src\Viewer.cs",
  "$root\src\PcView.cs", "$root\src\NativeApp.PcView.cs",
  "$root\src\NativeApp.Wifi.cs", "$root\src\NativeApp.Tethering.cs"
)
$desktop = @("$root\tests\TestDesktop.cs")
New-Item -ItemType Directory -Path $build,$package,(Join-Path $root 'dist') -Force | Out-Null
$archive = Join-Path $build 'scrcpy-win64-v5.0.1.zip'
if (!(Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest 'https://github.com/Genymobile/scrcpy/releases/download/v5.0.1/scrcpy-win64-v5.0.1.zip' -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne 'B12A2C4EE8BE317422451FC7DCF8EE20A71B5EA7EF9AD73DDD825A25316227A5') { throw 'scrcpy SHA256 mismatch' }
if (!(Test-Path -LiteralPath $vendor)) { Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $build 'vendor') }
& $csc /nologo "/out:$build\CoreTests.exe" $core "$root\tests\CoreTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& $csc /nologo "/out:$build\AlarmTests.exe" $core "$root\tests\AlarmTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Alarm test compilation failed' }
& "$build\AlarmTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Alarm tests failed' }
& "$build\CoreTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
& $csc /nologo /target:exe /main:SetupTests "/out:$build\SetupTests.exe" /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:Microsoft.CSharp.dll "$root\src\Setup.cs" "$root\tests\SetupTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Setup test compilation failed' }
& "$build\SetupTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Setup tests failed' }
& $csc /nologo /target:exe /main:PcViewTests "/out:$build\PcViewTests.exe" /reference:System.Drawing.dll $core "$root\src\PcView.cs" "$root\tests\PcViewTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'PcView test compilation failed' }
& "$build\PcViewTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'PcView tests failed' }
& $csc /nologo /target:exe /platform:x64 /main:ViewerTests "/out:$build\ViewerTests.exe" /reference:System.Drawing.dll $app $desktop "$root\tests\ViewerTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Viewer test compilation failed' }
& "$build\ViewerTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Viewer integration tests failed' }
& $csc /nologo /target:exe /platform:x64 /main:ShellTests "/out:$build\ShellTests.exe" /reference:System.Drawing.dll $app $desktop "$root\tests\ShellTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Shell test compile failed' }
& "$build\ShellTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Shell tests failed' }
& "$root/tests/NoRemote.ps1"

& "$root\build-phone-view.ps1"
& $csc /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Drawing.dll "/out:$package\PhoneDesk.exe" $app
if ($LASTEXITCODE -ne 0) { throw 'App compilation failed' }
New-Item -ItemType Directory -Path "$package\runtime" -Force | Out-Null
Get-ChildItem -LiteralPath $vendor -File | Where-Object { $_.Extension -notin '.bat','.vbs' } | Copy-Item -Destination "$package\runtime"
Copy-Item -LiteralPath "$root\runtime-assets\phonedesk-power.jar" -Destination "$package\runtime\phonedesk-power.jar" -Force
& "$root/build-native.ps1"
Copy-Item "$build/native-client/app/scrcpy.exe" "$package/runtime/scrcpy.exe" -Force
& "$root\tests\InputRegression.ps1" -Executable "$package/runtime/scrcpy.exe"
@'
PhoneDesk modifications - 2026-11-03
scrcpy 5.0.1 client rebuilt from the source in this fork.
Windows SDL text composition is disabled for Android UHID keyboard input.
Backspace is sent as a down/up pair on every PC press and repeat.
Other keys retain the upstream Android host-repeat behavior.
Source: app/src/screen.c, app/src/uhid/keyboard_uhid.c
'@ | Set-Content -LiteralPath "$package\runtime\PHONEDESK-MODIFICATIONS.txt" -Encoding UTF8
if (!(Test-Path "$build\LICENSE-scrcpy.txt")) { Invoke-WebRequest 'https://raw.githubusercontent.com/Genymobile/scrcpy/v5.0.1/LICENSE' -OutFile "$build\LICENSE-scrcpy.txt" }
Copy-Item "$build\LICENSE-scrcpy.txt" "$package\runtime\LICENSE-scrcpy.txt" -Force
Copy-Item "$root\README.md" "$package\README.md" -Force
Copy-Item "$root\distribution-assets\*" $package -Recurse -Force
Copy-Item "$root\build\phone-view\phone-desk-view.apk" "$package\phone-desk-view.apk" -Force
Compress-Archive -Path "$package\*" -DestinationPath "$build\payload.zip" -Force
& $csc /nologo /target:winexe /platform:x64 /optimize+ "/out:$root\dist\PhoneDesk-Setup.exe" "/resource:$build\payload.zip,payload.zip" /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:Microsoft.CSharp.dll "$root\src\Setup.cs"
if ($LASTEXITCODE -ne 0) { throw 'Setup compilation failed' }
& "$build\SetupTests.exe" "$root\dist\PhoneDesk-Setup.exe"
if ($LASTEXITCODE -ne 0) { throw 'Setup payload extraction failed' }
Get-FileHash "$root\dist\PhoneDesk-Setup.exe" -Algorithm SHA256
