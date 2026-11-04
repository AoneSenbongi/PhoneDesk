// SPDX-License-Identifier: Apache-2.0
using System;
using System.IO;
using System.IO.Compression;
class SetupTests {
 static void Main(string[] args) {
  if(args.Length>0)ActualPayload(args[0]);
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
     var license=created.CreateEntry("licenses/nested/LICENSE.txt");using(var w=new StreamWriter(license.Open()))w.Write("license-test");
    }
    memory.Position=0;
    using(var zip=new ZipArchive(memory,ZipArchiveMode.Read)) {
    Setup.ExtractPayload(zip,install);
    if(!File.Exists(Path.Combine(install,"adb.exe")))throw new Exception("Payload files were not extracted");
    if(File.ReadAllText(Path.Combine(install,"licenses","nested","LICENSE.txt"))!="license-test")throw new Exception("Nested license was not installed");
    var same=zip.GetEntry("adb.exe");var different=zip.GetEntry("changed.exe");
    using(var locked=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read)) {
     if(!Setup.SameFile(same,file))throw new Exception("Identical locked file must be skipped");
     if(Setup.SameFile(different,file))throw new Exception("Different content must be updated");
    }
    if(Setup.SameFile(same,file+".missing"))throw new Exception("Missing file must be extracted");
    }
   }
   Console.WriteLine("PASS: installer skips identical locked files and removes obsolete remote backend (6 checks)");
  } finally {File.Delete(file);if(Directory.Exists(install))Directory.Delete(install,true);}
 }
 static void ActualPayload(string executable){
  var target=Path.Combine(Path.GetTempPath(),"PhoneDesk-payload-test-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(target);
  try{
   var assembly=System.Reflection.Assembly.LoadFile(Path.GetFullPath(executable));
   using(var stream=assembly.GetManifestResourceStream("payload.zip"))
   using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)){
    assembly.GetType("Setup").GetMethod("ExtractPayload",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{zip,target});
    foreach(var entry in zip.Entries)if(entry.Name.Length>0&&!Setup.SameFile(entry,Path.Combine(target,entry.FullName)))throw new Exception("Extracted payload mismatch");
   }
   if(Directory.GetFiles(Path.Combine(target,"licenses")).Length<10||!File.Exists(Path.Combine(target,"runtime","scrcpy.exe"))||!File.Exists(Path.Combine(target,"FFMPEG-BUILD.json"))||Directory.Exists(Path.Combine(target,"remote")))throw new Exception("Required payload content missing");
   Console.WriteLine("PASS: actual Setup.exe extracts all files and licenses with matching hashes");
  }finally{Directory.Delete(target,true);}
 }
}
