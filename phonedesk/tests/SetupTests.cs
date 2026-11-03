// SPDX-License-Identifier: Apache-2.0
using System;
using System.IO;
using System.IO.Compression;
class SetupTests {
 static void Main() {
  var file=Path.GetTempFileName();
  var install=Path.Combine(Path.GetTempPath(),"PhoneDesk-setup-test-"+Guid.NewGuid().ToString("N"));
  try {
   Directory.CreateDirectory(Path.Combine(install,"remote"));
   File.WriteAllText(Path.Combine(install,"remote","PhoneDesk.Remote.exe"),"obsolete");
   File.WriteAllText(Path.Combine(install,"settings.keep"),"keep");
   Setup.RemoveObsoleteFiles(install);
   if(Directory.Exists(Path.Combine(install,"remote"))||!File.Exists(Path.Combine(install,"settings.keep")))throw new Exception("Only obsolete remote files should be removed");
   File.WriteAllBytes(file,new byte[]{1,2,3});
   using(var memory=new MemoryStream()) {
    using(var created=new ZipArchive(memory,ZipArchiveMode.Create,true)) {
     var sameEntry=created.CreateEntry("adb.exe");using(var s=sameEntry.Open()){s.Write(new byte[]{1,2,3},0,3);}
     var differentEntry=created.CreateEntry("changed.exe");using(var s=differentEntry.Open()){s.Write(new byte[]{1,2,4},0,3);}
    }
    memory.Position=0;
    using(var zip=new ZipArchive(memory,ZipArchiveMode.Read)) {
    var same=zip.GetEntry("adb.exe");var different=zip.GetEntry("changed.exe");
    using(var locked=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read)) {
     if(!Setup.SameFile(same,file))throw new Exception("Identical locked file must be skipped");
     if(Setup.SameFile(different,file))throw new Exception("Different content must be updated");
    }
    if(Setup.SameFile(same,file+".missing"))throw new Exception("Missing file must be extracted");
    }
   }
   Console.WriteLine("PASS: installer skips identical locked files and removes obsolete remote backend (4 checks)");
  } finally {File.Delete(file);if(Directory.Exists(install))Directory.Delete(install,true);}
 }
}
