param([string]$Executable = "$PSScriptRoot\..\build\package\runtime\scrcpy.exe")
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\..\PeImports.ps1"
$imports = @(Get-PeImports ([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Executable))))
if ($imports.Name -contains 'SDL_StartTextInput') { throw 'Regression: SDL_StartTextInput still activates the Windows IME for UHID input.' }
if (@($imports | Where-Object { $_.Module -eq 'SDL3.dll' -and $_.Name -eq 'SDL_StopTextInput' }).Count -ne 1) { throw 'SDL_StopTextInput import is missing.' }
Write-Output 'PASS: UHID does not activate the Windows IME'
