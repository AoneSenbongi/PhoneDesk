const cp=require('child_process'),path=require('path');
const serial=process.argv[2];if(!serial)throw Error('Serial required');
const adb=process.env.LOCALAPPDATA+'/PhoneDesk/runtime/adb.exe';
let watch,fixture;
const events=[];
function until(predicate,ms=6000){const start=Date.now();return new Promise((resolve,reject)=>{const tick=()=>predicate()?resolve():Date.now()-start>ms?reject(Error('Alarm state timed out')):setTimeout(tick,50);tick();});}
(async()=>{
 watch=cp.spawn(adb,['-s',serial,'shell','-T','CLASSPATH=/data/local/tmp/phonedesk-alarm.jar','app_process','/','com.phonedesk.AlarmWatch'],{windowsHide:true});
 let buffer='',error='';watch.stdout.on('data',data=>{buffer+=data.toString();const lines=buffer.split(/\r?\n/);buffer=lines.pop();for(const line of lines)if(/^ALARM [01]$/.test(line))events.push(line);});watch.stderr.on('data',data=>error+=data);
 await until(()=>events.includes('ALARM 0')||watch.exitCode!==null);if(watch.exitCode!==null)throw Error('Monitor initialization failed: '+error);
 fixture=cp.spawn(adb,['-s',serial,'shell','CLASSPATH=/data/local/tmp/phonedesk-alarm-fixture.jar','app_process','/','AlarmFixture','media'],{windowsHide:true,stdio:'ignore'});
 await until(()=>fixture.exitCode!==null);if(events.includes('ALARM 1'))throw Error('Media playback triggered alarm');
 fixture=cp.spawn(adb,['-s',serial,'shell','CLASSPATH=/data/local/tmp/phonedesk-alarm-fixture.jar','app_process','/','AlarmFixture','alarm'],{windowsHide:true,stdio:'ignore'});
 await until(()=>events.includes('ALARM 1'));await until(()=>fixture.exitCode!==null);await until(()=>events.at(-1)==='ALARM 0');
 watch.stdin.end();await until(()=>watch.exitCode!==null);
 console.log('PASS: Android monitor idle / media ignored / alarm start / alarm end / EOF shutdown');
})().catch(e=>{console.error(e.message);process.exitCode=1;}).finally(()=>{if(watch&&watch.exitCode===null)watch.kill();if(fixture&&fixture.exitCode===null)fixture.kill();});
