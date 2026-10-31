param([Parameter(Mandatory=$true)][string]$Serial,[string]$AndroidSdk=$env:ANDROID_HOME,[string]$JavaHome=$env:JAVA_HOME)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$env:JAVA_HOME=$JavaHome
$adb="$env:LOCALAPPDATA/PhoneDesk/runtime/adb.exe"
& "$JavaHome/bin/javac.exe" --release 8 -d "$root/build" "$PSScriptRoot/ClipboardFixture.java"
if($LASTEXITCODE -ne 0){throw 'Clipboard fixture compilation failed'}
& "$AndroidSdk/build-tools/36.0.0/d8.bat" --min-api 26 --output "$root/build/clipboard-fixture.jar" "$root/build/ClipboardFixture.class"
if($LASTEXITCODE -ne 0){throw 'Clipboard fixture dex compilation failed'}
& $adb -s $Serial push "$root/build/clipboard-fixture.jar" /data/local/tmp/phonedesk-clipboard-fixture.jar | Out-Null
& $adb -s $Serial push "$env:LOCALAPPDATA/PhoneDesk/runtime/scrcpy-server" /data/local/tmp/phonedesk-clipboard-server.jar | Out-Null
Add-Type -AssemblyName System.Windows.Forms
$snapshot=[Windows.Forms.DataObject]::new()
$existing=[Windows.Forms.Clipboard]::GetDataObject()
if($existing){foreach($format in $existing.GetFormats($false)){$snapshot.SetData($format,$false,$existing.GetData($format,$false))}}
$fixture=[Diagnostics.Process]::new()
$fixture.StartInfo=[Diagnostics.ProcessStartInfo]::new($adb,('-s '+$Serial+' shell -T CLASSPATH=/data/local/tmp/phonedesk-clipboard-server.jar:/data/local/tmp/phonedesk-clipboard-fixture.jar app_process / ClipboardFixture'))
$fixture.StartInfo.UseShellExecute=$false;$fixture.StartInfo.CreateNoWindow=$true
$fixture.StartInfo.RedirectStandardInput=$true;$fixture.StartInfo.RedirectStandardOutput=$true;$fixture.StartInfo.RedirectStandardError=$true
try{
 $fixture.Start()|Out-Null
 $errors=$fixture.StandardError.ReadToEndAsync()
 $ready=$fixture.StandardOutput.ReadLineAsync()
 if(!$ready.Wait(10000) -or $ready.Result -ne 'READY'){throw 'Android clipboard fixture is unavailable'}
 $samples=@('PhoneDesk copy test',"日本語のコピー`nPhoneDesk clipboard")
 for($i=0;$i -lt $samples.Count;$i++){
  $fixture.StandardInput.Write([char](49+$i));$fixture.StandardInput.Flush()
  $reply=$fixture.StandardOutput.ReadLineAsync()
  if(!$reply.Wait(5000) -or $reply.Result -ne 'COPIED'){throw 'Android copy failed'}
  $matched=$false
  for($attempt=0;$attempt -lt 50;$attempt++){
   Start-Sleep -Milliseconds 100
   if([Windows.Forms.Clipboard]::GetText().Replace("`r`n","`n") -ceq $samples[$i]){$matched=$true;break}
  }
  if(!$matched){throw 'Android text did not reach PC clipboard'}
  "PASS: Android -> PC clipboard sample $($i+1)"
  if($i -eq 0){
   [Windows.Forms.Clipboard]::SetText('PhoneDesk PC-only test')
   Start-Sleep -Milliseconds 500
   $fixture.StandardInput.Write('3');$fixture.StandardInput.Flush()
   $check=$fixture.StandardOutput.ReadLineAsync()
   if(!$check.Wait(3000) -or $check.Result -ne 'UNCHANGED'){throw 'PC copy unexpectedly changed Android clipboard'}
   'PASS: PC copy does not change Android clipboard'
  }
 }
}finally{
 if($fixture.Id){$fixture.StandardInput.Close();if(!$fixture.WaitForExit(5000)){$fixture.Kill()}}
 Start-Sleep -Milliseconds 500
 [Windows.Forms.Clipboard]::SetDataObject($snapshot,$true)
 $fixture.Dispose()
}
