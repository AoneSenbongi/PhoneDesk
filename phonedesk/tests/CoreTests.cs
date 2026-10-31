// SPDX-License-Identifier: Apache-2.0
using System;
using PhoneDesk;
class CoreTests {
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static void Main() {
        var ds = DeviceParser.Parse("* daemon started successfully *\nList of devices attached\nabc device usb:1-2 product:x model:Pixel_7 transport_id:1\n192.168.0.4:5555 device product:x model:Remote\nxyz unauthorized usb:2\nzzz offline usb:3\n");
        Check(ds.Count == 4, "parse count");
        Check(ds[0].Usb && ds[0].State == "device" && ds[0].Model == "Pixel 7", "USB model");
        Check(!ds[1].Usb, "exclude wireless");
        Check(DeviceParser.Parse("USB-DEMO-001 device product:PacmanJPN model:A142 device:Pacman transport_id:214")[0].Usb, "Windows USB without usb attribute");
        Check(!DeviceParser.Parse("adb-abc._adb-tls-connect._tcp device model:Remote")[0].Usb, "exclude mDNS wireless");
        Check(!DeviceParser.Parse("emulator-5554 device model:Emulator")[0].Usb, "exclude emulator");
        Check(ds[2].State == "unauthorized" && ds[3].State == "offline", "not ready");
        Check(DeviceParser.Parse("List of devices attached\n").Count == 0, "unplug");
        var s = new SessionState(); var t = DateTime.UtcNow;
        Check(s.ShouldStart(t), "new connect"); s.Started();
        Check(!s.ShouldStart(t), "no duplicates"); s.Exited(0,t);
        Check(!s.ShouldStart(t.AddMinutes(2)), "manual close suppressed"); s.Manual();
        Check(s.ShouldStart(t), "manual reopen");
        for(int i=0;i<3;i++) { s.Started(); s.Exited(1,t); Check(!s.ShouldStart(t.AddSeconds(9)), "backoff"); t=t.AddSeconds(11); }
        Check(!s.ShouldStart(t.AddDays(1)), "bounded retries");
        Check(new SessionState().ShouldStart(t), "reconnect resets");
        Check(DeviceParser.Quote("abc:123") == "\"abc:123\"", "serial quoting");
        bool rejected=false; try { DeviceParser.Quote("bad\"serial"); } catch(ArgumentException) { rejected=true; }
        Check(rejected,"reject argument injection");
        Console.WriteLine("PASS: " + checks + " checks");
        var volume=VolumeState.Parse("diagnostic\nVOLUME 7 0 16\n");
        Check(volume.Value==7&&volume.Min==0&&volume.Max==16,"volume parser ignores diagnostics");
        foreach(var bad in new[]{"VOLUME 17 0 16","VOLUME -1 0 16","VOLUME 3 4 2","VOLUME 3 0 999999","invalid"}) {
            bool invalid=false;try{VolumeState.Parse(bad);}catch(FormatException){invalid=true;}Check(invalid,"invalid volume range rejected");
        }
        Console.WriteLine("PASS: volume validation (6 checks)");
        string video=StreamOptions.Arguments(ds[0]),audio=StreamOptions.AudioArguments(ds[0]);
        Check(video.Contains("--no-audio")&&video.Contains("--keyboard=uhid"),"video and UHID are independent from audio");
        Check(audio.Contains("--no-window")&&audio.Contains("--audio-source=playback")&&!audio.Contains("--audio-dup"),"audio only redirects instead of duplicating");
        Check(!audio.Contains("--keyboard"),"audio toggle does not reconnect keyboard");
        Check(video.Contains("--no-clipboard-autosync"),"PC clipboard must not be sent to Android");
        Console.WriteLine("PASS: audio and clipboard options (4 checks)");
    }
}
