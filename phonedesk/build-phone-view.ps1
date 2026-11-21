# Builds the PhoneDesk View APK bundled in the Windows setup.
# Android SDK platform 36 / build-tools 36.0.0 and JDK 17 are required.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$sdk = 'E:\prog\android-sdk'
$java = 'C:\Program Files\Eclipse Adoptium\jdk-17.0.21.9-hotspot'
$env:JAVA_HOME = $java
$androidJar = Join-Path $sdk 'platforms\android-36\android.jar'
$buildTools = Join-Path $sdk 'build-tools\36.0.0'
$source = Join-Path $root 'phone-view\src'
$classes = Join-Path $root 'build\phone-view-classes'
$dex = Join-Path $root 'build\phone-view-dex'
$gen = Join-Path $root 'build\phone-view-gen'
$output = Join-Path $root 'build\phone-view\phone-desk-view.apk'
New-Item -ItemType Directory -Path $classes,$dex,$gen,(Split-Path $output) -Force | Out-Null

& "$java\bin\javac.exe" -source 8 -target 8 -bootclasspath $androidJar -d $classes (Join-Path $source 'com\phonedesk\view\MainActivity.java')
if ($LASTEXITCODE -ne 0) { throw 'Java compile failed' }
$classFiles = @(Get-ChildItem -LiteralPath $classes -Recurse -Filter '*.class' | ForEach-Object { $_.FullName })
& "$buildTools\d8.bat" --lib $androidJar --min-api 26 --output $dex $classFiles
if ($LASTEXITCODE -ne 0) { throw 'D8 failed' }
$unsigned = Join-Path $root 'build\phone-view\unsigned.apk'
& "$buildTools\aapt2.exe" link -o $unsigned -I $androidJar --manifest (Join-Path $root 'phone-view\AndroidManifest.xml') --java $gen --min-sdk-version 26 --target-sdk-version 36
if ($LASTEXITCODE -ne 0) { throw 'aapt2 link failed' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($unsigned, [System.IO.Compression.ZipArchiveMode]::Update)
try {
  [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $dex 'classes.dex'), 'classes.dex') | Out-Null
} finally { $archive.Dispose() }
$aligned = Join-Path $root 'build\phone-view\aligned.apk'
& "$buildTools\zipalign.exe" -f 4 $unsigned $aligned
if ($LASTEXITCODE -ne 0) { throw 'zipalign failed' }
$keystore = Join-Path $env:USERPROFILE '.android\debug.keystore'
& "$buildTools\apksigner.bat" sign --ks $keystore --ks-key-alias androiddebugkey --ks-pass pass:android --key-pass pass:android --out $output $aligned
if ($LASTEXITCODE -ne 0) { throw 'apksigner failed' }
Write-Output "PHONE VIEW APK OK: $output"
