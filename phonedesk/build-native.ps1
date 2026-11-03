param([string]$Source='', [string]$Python='python',[string]$Toolchain='')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
if(!$Toolchain){$Toolchain=Split-Path (Get-Command gcc -ErrorAction Stop).Source}
if(!$Source){
    $Source=if(Test-Path "$root/../app/meson.build"){Split-Path $root}else{"$root/publication/scrcpy-fork"}
}
$work="$root/build/native-deps"
New-Item -ItemType Directory $work,"$work/lib","$work/lib/pkgconfig" -Force | Out-Null
function Download([string]$Url,[string]$Name,[string]$Hash){
    $target=Join-Path $work $Name
    if(!(Test-Path $target)){Invoke-WebRequest $Url -OutFile $target}
    if((Get-FileHash $target -Algorithm SHA256).Hash -ne $Hash){throw "Hash mismatch: $Name"}
}
Download 'https://github.com/libsdl-org/SDL/releases/download/release-3.4.18/SDL3-devel-3.4.18-mingw.tar.gz' 'sdl.tar.gz' '049B9DD711E3FB3F8D84FEB998DFEC54A75E17CF539C4A80FF4D058D839771CF'
Download 'https://ffmpeg.org/releases/ffmpeg-9.0.2.tar.xz' 'ffmpeg.tar.xz' '8C3850283EB25FA026482078A04051E0BE17347B09EF81A0849BEC15A96E002E'
if(!(Test-Path "$work/SDL3-3.4.18")){tar -xf "$work/sdl.tar.gz" -C $work;if($LASTEXITCODE){throw 'SDL extraction failed'}}
if(!(Test-Path "$work/ffmpeg-9.0.2")){tar -xf "$work/ffmpeg.tar.xz" -C $work;if($LASTEXITCODE){throw 'FFmpeg extraction failed'}}
$sdl="$work/SDL3-3.4.18/x86_64-w64-mingw32"
$ffmpeg="$work/ffmpeg-9.0.2"
# Public header configuration for Windows x64, matching the bundled DLL ABI.
$avconfig="#define AV_HAVE_BIGENDIAN 0`n#define AV_HAVE_FAST_UNALIGNED 1"
if(!(Test-Path "$ffmpeg/libavutil/avconfig.h") -or (Get-Content "$ffmpeg/libavutil/avconfig.h" -Raw).Trim() -ne $avconfig.Trim()){Set-Content "$ffmpeg/libavutil/avconfig.h" $avconfig -Encoding ascii}
$vendor="$root/build/vendor/scrcpy-win64-v5.0.1"
if(!(Test-Path "$vendor/avcodec-63.dll")){throw 'Run build.ps1 once to fetch the pinned scrcpy runtime'}
Push-Location "$work/lib"
try{
    foreach($name in @('avcodec-63','avformat-63','avutil-61','swresample-7')){
        & "$Toolchain/gendef.exe" "$vendor/$name.dll" 2>$null
        if($LASTEXITCODE){throw "gendef failed: $name"}
        & "$Toolchain/dlltool.exe" -d "$name.def" -l "lib$name.dll.a" -D "$name.dll"
        if($LASTEXITCODE){throw "dlltool failed: $name"}
        $libname=($name -split '-')[0]
        $versionHeader=Get-Content "$ffmpeg/lib$libname/version.h" -Raw
        $versionMinor=[regex]::Match($versionHeader,'VERSION_MINOR\s+(\d+)').Groups[1].Value
        $versionMicro=[regex]::Match($versionHeader,'VERSION_MICRO\s+(\d+)').Groups[1].Value
        @"
Name: lib$libname
Description: FFmpeg shared library from pinned scrcpy runtime
Version: $($name.Split('-')[1]).$versionMinor.$versionMicro
Libs: -L$work/lib -l$name
Cflags: -I$ffmpeg
"@ | Set-Content "$work/lib/pkgconfig/lib$libname.pc" -Encoding ascii
    }
}finally{Pop-Location}
$env:PKG_CONFIG_PATH="$work/lib/pkgconfig;$sdl/lib/pkgconfig"
$env:PYTHONPATH="$root/build/native-tools"
if(!(Test-Path "$root/build/native-tools/mesonbuild")){
    & $Python -m pip install --target "$root/build/native-tools" meson==1.9.1 --quiet
    if($LASTEXITCODE){throw 'Meson installation failed'}
}
$env:CC="`"$Toolchain/gcc.exe`" -B`"$Toolchain/`""
$env:PKG_CONFIG="$Toolchain/pkg-config.exe"
$env:NINJA="$Toolchain/ninja.exe"
$env:STRIP="$Toolchain/strip.exe"
$env:AR="$Toolchain/ar.exe"
$env:WINDRES="$Toolchain/windres.exe"
# w64devkit's BusyBox patch does not implement --version; Meson probes it.
$originalPath=$env:PATH
$env:PATH=($env:PATH -split ';' | Where-Object { $_ -and $_.TrimEnd('\','/') -ne $Toolchain.TrimEnd('\','/').Replace('/','\') }) -join ';'
try {
$destination="$root/build/native-client"
if(!(Test-Path "$destination/build.ninja")){
    & $Python -m mesonbuild.mesonmain setup $destination $Source --buildtype=release --strip -Dcompile_server=false -Dportable=true -Dusb=false
    if($LASTEXITCODE){throw 'Native configure failed'}
}
& "$Toolchain/ninja.exe" -C $destination
if($LASTEXITCODE){throw 'Native build failed'}
# Native regression uses the actual UHID implementation and report generator.
& "$Toolchain/gcc.exe" -B "$Toolchain/" -I "$destination/app" -I "$Source/app/src" -I $ffmpeg -I "$sdl/include" "$root/tests/BackspaceUhid.c" "$Source/app/src/uhid/keyboard_uhid.c" "$Source/app/src/hid/hid_keyboard.c" -L "$sdl/lib" -lSDL3 -o "$destination/BackspaceUhid.exe"
if($LASTEXITCODE){throw 'UHID test compile failed'}
Copy-Item "$sdl/bin/SDL3.dll" $destination -Force
& "$destination/BackspaceUhid.exe"
if($LASTEXITCODE){throw 'UHID tests failed'}

}finally{$env:PATH=$originalPath}
