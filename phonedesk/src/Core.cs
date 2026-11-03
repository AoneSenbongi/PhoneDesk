// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneDesk {
    public static class StreamOptions {
        public static string Arguments(Device d) {
            return "--serial="+DeviceParser.Quote(d.Serial)+" --keyboard=uhid --no-audio --no-clipboard-autosync --max-size=1920 --max-fps=60 --window-width=420 --window-x=-10000 --window-y=-10000 --window-title="+DeviceParser.Quote("PhoneDesk | "+d.Model);
        }
        public static string AudioArguments(Device d,bool duplicate=false) {
            return "--serial="+DeviceParser.Quote(d.Serial)+" --no-window --audio-source=playback"+(duplicate?" --audio-dup":"");
        }
    }
    public sealed class SpeakerState { public int Volume; public bool Muted; }
    public sealed class AlarmVolumeOverride {
        readonly Func<SpeakerState> read;readonly Action<SpeakerState> write;
        readonly HashSet<string> alarms=new HashSet<string>();SpeakerState previous;
        public bool Active {get{return alarms.Count>0;}}
        public AlarmVolumeOverride(Func<SpeakerState> capture,Action<SpeakerState> apply){read=capture;write=apply;}
        public void Set(string device,bool active){
            if(active){
                if(alarms.Contains(device))return;
                if(!Active){previous=read();write(new SpeakerState{Volume=100,Muted=false});}
                alarms.Add(device);
            }else if(alarms.Remove(device)&&!Active){write(previous);previous=null;}
        }
        public void Refresh(){if(Active){var now=read();if(now.Volume!=100||now.Muted)write(new SpeakerState{Volume=100,Muted=false});}}
        public void Clear(){if(!Active)return;write(previous);alarms.Clear();previous=null;}
    }
    public sealed class VolumeState {
        public int Value, Min, Max;
        public static VolumeState Parse(string output) {
            foreach(var line in output.Split('\n')) {
                var p=line.Trim().Split(' ');int value,min,max;
                if(p.Length==4&&p[0]=="VOLUME"&&int.TryParse(p[1],out value)&&int.TryParse(p[2],out min)&&int.TryParse(p[3],out max)&&min>=0&&max>=min&&max<=65535&&value>=min&&value<=max)return new VolumeState{Value=value,Min=min,Max=max};
            }
            throw new FormatException("音量情報の取得失敗");
        }
    }
    public sealed class Device {
        public string Serial, State, Model;
        public bool Usb;
        public override string ToString() { return Model + "  (" + Serial + ")"; }
    }
    public static class DeviceParser {
        public static List<Device> Parse(string text) {
            var result = new List<Device>();
            foreach (var line in text.Split('\n')) {
                var p = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 2 || p[0] == "List" || p[0].StartsWith("*")) continue;
                if (p[1] != "device" && p[1] != "unauthorized" && p[1] != "offline" && p[1] != "no") continue;
                var d = new Device { Serial = p[0], State = p[1], Model = p[0],
                    Usb = p[0].IndexOf(':') < 0 && p[0].IndexOf("_tcp", StringComparison.OrdinalIgnoreCase) < 0 && !p[0].StartsWith("emulator-") };
                foreach (var token in p.Skip(2)) {
                    if (token.StartsWith("usb:")) d.Usb = true;
                    if (token.StartsWith("model:")) d.Model = token.Substring(6).Replace('_', ' ');
                }
                result.Add(d);
            }
            return result;
        }
        public static string Quote(string s) {
            if (s.IndexOfAny(new char[] { '"', '\r', '\n' }) >= 0) throw new ArgumentException("Invalid argument");
            return "\"" + s + "\"";
        }
    }
    public sealed class SessionState {
        public bool Running, Dismissed;
        public int Failures;
        public DateTime RetryAt;
        public bool ShouldStart(DateTime now) { return !Running && !Dismissed && Failures < 3 && now >= RetryAt; }
        public void Started() { Running = true; }
        public void Exited(int code, DateTime now) {
            Running = false;
            if (code == 0) Dismissed = true;
            else { Failures++; RetryAt = now.AddSeconds(10); }
        }
        public void Manual() { Dismissed = false; Failures = 0; RetryAt = DateTime.MinValue; }
    }
}
