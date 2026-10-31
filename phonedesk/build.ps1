$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$package = Join-Path $build 'package'
$vendor = Join-Path $build 'vendor\scrcpy-win64-v5.0.1'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Path $build,$package,(Join-Path $root 'dist') -Force | Out-Null
$archive = Join-Path $build 'scrcpy-win64-v5.0.1.zip'
if (!(Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest 'https://github.com/Genymobile/scrcpy/releases/download/v5.0.1/scrcpy-win64-v5.0.1.zip' -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne 'B12A2C4EE8BE317422451FC7DCF8EE20A71B5EA7EF9AD73DDD825A25316227A5') { throw 'scrcpy SHA256 mismatch' }
if (!(Test-Path -LiteralPath $vendor)) { Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $build 'vendor') }
& $csc /nologo "/out:$build\CoreTests.exe" "$root\src\Core.cs" "$root\tests\CoreTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& "$build\CoreTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
& $csc /nologo /target:exe /main:SetupTests "/out:$build\SetupTests.exe" /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:Microsoft.CSharp.dll "$root\src\Setup.cs" "$root\tests\SetupTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Setup test compilation failed' }
& "$build\SetupTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Setup tests failed' }
& $csc /nologo /target:exe /platform:x64 /main:ViewerTests "/out:$build\ViewerTests.exe" "$root\src\Core.cs" "$root\src\NativeApp.cs" "$root\src\NativeApp.Audio.cs" "$root\src\NativeApp.Rotation.cs" "$root\src\NativeApp.Clipboard.cs" "$root\src\PcPlayback.cs" "$root\src\AudioSwitch.cs" "$root\src\Viewer.cs" "$root\tests\ViewerTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Viewer test compilation failed' }
& "$build\ViewerTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Viewer integration tests failed' }
& $csc /nologo /target:winexe /platform:x64 /optimize+ "/out:$package\PhoneDesk.exe" "$root\src\Core.cs" "$root\src\NativeApp.cs" "$root\src\NativeApp.Audio.cs" "$root\src\NativeApp.Rotation.cs" "$root\src\NativeApp.Clipboard.cs" "$root\src\PcPlayback.cs" "$root\src\AudioSwitch.cs" "$root\src\Viewer.cs"
if ($LASTEXITCODE -ne 0) { throw 'App compilation failed' }
New-Item -ItemType Directory -Path "$package\runtime" -Force | Out-Null
Get-ChildItem -LiteralPath $vendor -File | Where-Object { $_.Extension -notin '.bat','.vbs' } | Copy-Item -Destination "$package\runtime"
Copy-Item -LiteralPath "$root\runtime-assets\phonedesk-power.jar" -Destination "$package\runtime\phonedesk-power.jar" -Force
. "$root\PeImports.ps1"
$scrcpyPath = "$package\runtime\scrcpy.exe"
$scrcpyBytes = [IO.File]::ReadAllBytes($scrcpyPath)
$textImports = @(Get-PeImports $scrcpyBytes | Where-Object { $_.Module -eq 'SDL3.dll' -and $_.Name -eq 'SDL_StartTextInput' })
if ($textImports.Count -ne 1) { throw 'Unexpected scrcpy SDL import; refusing to patch' }
$replacement = [Text.Encoding]::ASCII.GetBytes("SDL_StopTextInput`0`0")
[Array]::Copy($replacement,0,$scrcpyBytes,$textImports[0].Offset,$replacement.Length)
[IO.File]::WriteAllBytes($scrcpyPath,$scrcpyBytes)
& "$root\tests\InputRegression.ps1" -Executable $scrcpyPath
@'
PhoneDesk modification notice — 2026-10-26
scrcpy.exe is based on the official Genymobile/scrcpy 5.0.1 Windows x64 binary.
The SDL3 import SDL_StartTextInput has been redirected to SDL_StopTextInput.
Both functions take SDL_Window* and return bool. This build uses UHID keyboard
input only and prevents the Windows IME from consuming keys intended for Android.
Original source: https://github.com/Genymobile/scrcpy/tree/v5.0.1
Original archive and SHA-256 verification are retained by the PhoneDesk build.
No other binary code or data has been modified.
'@ | Set-Content -LiteralPath "$package\runtime\PHONEDESK-MODIFICATIONS.txt" -Encoding UTF8
if (!(Test-Path "$build\LICENSE-scrcpy.txt")) { Invoke-WebRequest 'https://raw.githubusercontent.com/Genymobile/scrcpy/v5.0.1/LICENSE' -OutFile "$build\LICENSE-scrcpy.txt" }
Copy-Item "$build\LICENSE-scrcpy.txt" "$package\runtime\LICENSE-scrcpy.txt" -Force
Copy-Item "$root\README.md" "$package\README.md" -Force
Compress-Archive -Path "$package\*" -DestinationPath "$build\payload.zip" -Force
& $csc /nologo /target:winexe /platform:x64 /optimize+ "/out:$root\dist\PhoneDesk-Setup.exe" "/resource:$build\payload.zip,payload.zip" /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:Microsoft.CSharp.dll "$root\src\Setup.cs"
if ($LASTEXITCODE -ne 0) { throw 'Setup compilation failed' }
Get-FileHash "$root\dist\PhoneDesk-Setup.exe" -Algorithm SHA256
