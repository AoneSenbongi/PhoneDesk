// SPDX-License-Identifier: Apache-2.0
using System;using PhoneDesk;
class PcAlarmEndpointTests {
 static void Main(){
  var original=PcPlayback.Capture();
  try{
   PcPlayback.Apply(original);var after=PcPlayback.Capture();
   if(after.Volume!=original.Volume||after.Muted!=original.Muted)throw new Exception("Endpoint state roundtrip failed");
   Console.WriteLine("PASS: Windows endpoint volume and mute snapshot/restore");
  }finally{PcPlayback.Apply(original);}
 }
}
