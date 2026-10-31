// SPDX-License-Identifier: Apache-2.0
using System;using PhoneDesk;
class PcVolumeProbe{static int Main(string[]args){int result=PcPlayback.MasterVolume(args.Length>0?(int?)int.Parse(args[0]):null);Console.WriteLine("VOLUME "+result+" 0 100");return 0;}}
