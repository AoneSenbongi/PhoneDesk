// SPDX-License-Identifier: Apache-2.0
using System;
using PhoneDesk;
class AlarmTests {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static void Main(){
  var speaker=new SpeakerState{Volume=23,Muted=true};int writes=0;
  var policy=new AlarmVolumeOverride(delegate{return new SpeakerState{Volume=speaker.Volume,Muted=speaker.Muted};},delegate(SpeakerState state){speaker=state;writes++;});
  policy.Set("a",false);Check(writes==0,"idle does not change audio");
  policy.Set("a",true);Check(policy.Active&&speaker.Volume==100&&!speaker.Muted,"alarm maximizes and unmutes");
  policy.Set("a",true);policy.Set("b",true);Check(writes==1,"second alarm preserves original snapshot");
  policy.Set("a",false);Check(policy.Active&&speaker.Volume==100,"overlapping alarm remains loud");
  speaker.Volume=8;speaker.Muted=true;policy.Refresh();Check(speaker.Volume==100&&!speaker.Muted,"active alarm stays loud");
  policy.Set("b",false);Check(!policy.Active&&speaker.Volume==23&&speaker.Muted,"last alarm restores exact state");
  int before=writes;policy.Set("b",false);Check(writes==before,"duplicate end is harmless");
  speaker=new SpeakerState{Volume=41,Muted=false};policy.Set("c",true);policy.Clear();Check(!policy.Active&&speaker.Volume==41&&!speaker.Muted,"disconnect or exit restores state");
  var device=new Device{Serial="test",State="device",Model="Test",Usb=true};
  Check(StreamOptions.AudioArguments(device,true).Contains("--audio-dup"),"alarm duplicates to phone");
  Check(!StreamOptions.AudioArguments(device).Contains("--audio-dup"),"normal routing preserved");
  Check(!StreamOptions.Arguments(device).Contains("--audio-dup")&&StreamOptions.Arguments(device).Contains("--no-audio"),"video and keyboard unaffected");
  Console.WriteLine("PASS: alarm policy ("+checks+" checks)");
 }
}
