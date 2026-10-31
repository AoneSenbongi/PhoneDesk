param([Parameter(Mandatory=$true)][string]$Serial)
$ErrorActionPreference='Stop'
$adb="$env:LOCALAPPDATA/PhoneDesk/runtime/adb.exe"
function Policy {return @(((& $adb -s $Serial shell wm user-rotation)-join '').Trim(),((& $adb -s $Serial shell wm fixed-to-user-rotation)-join '').Trim())}
$initial=(Policy)-join '|'
foreach($abrupt in @($false,$true)){
 $guard=[Diagnostics.Process]::new()
 $guard.StartInfo=[Diagnostics.ProcessStartInfo]::new($adb,('-s '+$Serial+' shell -T CLASSPATH=/data/local/tmp/phonedesk-rotation.jar app_process / com.phonedesk.RotationControl'))
 $guard.StartInfo.UseShellExecute=$false;$guard.StartInfo.CreateNoWindow=$true
 $guard.StartInfo.RedirectStandardInput=$true;$guard.StartInfo.RedirectStandardOutput=$true;$guard.StartInfo.RedirectStandardError=$true
 try{
  $guard.Start()|Out-Null;$errors=$guard.StandardError.ReadToEndAsync();$ready=$guard.StandardOutput.ReadLineAsync()
  if(!$ready.Wait(10000) -or $ready.Result -ne 'READY'){throw 'Rotation helper did not become ready'}
  if((Policy)[0] -ne 'lock 1'){throw 'Android did not enter right rotation'}
  if($abrupt){$guard.Kill()}else{$guard.StandardInput.Close()}
  $guard.WaitForExit(5000)|Out-Null
  $restored=$false
  for($i=0;$i -lt 40;$i++){if(((Policy)-join '|') -eq $initial){$restored=$true;break};Start-Sleep -Milliseconds 200}
  if(!$restored){throw 'Original Android rotation policy was not restored'}
  "PASS: Android rotation policy restored after abrupt=$abrupt"
 }finally{if(!$guard.HasExited){$guard.StandardInput.Close();$guard.WaitForExit(5000)|Out-Null};$guard.Dispose()}
}
