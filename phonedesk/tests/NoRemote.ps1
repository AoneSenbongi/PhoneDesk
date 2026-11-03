$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$source=Get-Content -LiteralPath "$root/src/NativeApp.cs" -Raw
if($source -match 'StartRemote|StopRemote|ShowRemoteConnection|遠隔操作|遠隔終了'){throw 'Remote UI/backend remains in the application'}
$build=Get-Content -LiteralPath "$root/build.ps1" -Raw
if($build -match 'Build remote agent|build\\remote\\\*'){throw 'Remote backend is still packaged'}
Write-Output 'PASS: application has no remote UI or backend dependency'
