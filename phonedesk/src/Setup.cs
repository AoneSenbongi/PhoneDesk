// SPDX-License-Identifier: Apache-2.0
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Win32;
[assembly: AssemblyTitle("PhoneDesk Setup")]
[assembly: AssemblyVersion("1.0.23.0")]
class Setup {
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
 static extern int MessageBox(IntPtr window,string text,string caption,uint type);
 [STAThread] static void Main() {
  try {Install();}catch(Exception ex) {File.WriteAllText(Path.Combine(Path.GetTempPath(),"PhoneDesk-Setup-error.txt"),ex.ToString());MessageBox(IntPtr.Zero,ex.Message,"PhoneDesk セットアップ",0x10);Environment.ExitCode=1;}
 }
 static void Install() {
  string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneDesk");
  string exe=Path.Combine(root,"PhoneDesk.exe");
  foreach(var p in Process.GetProcessesByName("PhoneDesk")) {
   using(p) {try {if(string.Equals(p.MainModule.FileName,exe,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("通知領域のPhoneDeskを終了してから、セットアップを再実行してください。");}catch(System.ComponentModel.Win32Exception){}}
  }
  Directory.CreateDirectory(root);
  RemoveObsoleteFiles(root);
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
  using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)) {
   ExtractPayload(zip,root);
  }
  using(var k=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))k.SetValue("PhoneDesk","\""+exe+"\" --background");
  Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"PhoneDesk.lnk"),exe,root);
  string programs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"PhoneDesk");Directory.CreateDirectory(programs);
  Shortcut(Path.Combine(programs,"PhoneDesk.lnk"),exe,root);
  Process.Start(new ProcessStartInfo(exe){UseShellExecute=true,WorkingDirectory=root});
 }
 static void Shortcut(string path,string exe,string root) {
  dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic shortcut=shell.CreateShortcut(path);
  shortcut.TargetPath=exe;shortcut.WorkingDirectory=root;shortcut.Description="Android USB画面表示・操作";shortcut.Save();
 }
 internal static void ExtractPayload(ZipArchive zip,string root) {
   foreach(var entry in zip.Entries) {
    string dest=Path.GetFullPath(Path.Combine(root,entry.FullName));
    if(!dest.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Invalid archive path");
    if(entry.Name.Length==0){Directory.CreateDirectory(dest);continue;}
    if(SameFile(entry,dest))continue;
    Directory.CreateDirectory(Path.GetDirectoryName(dest));entry.ExtractToFile(dest,true);
   }
 }
 internal static void RemoveObsoleteFiles(string root){
  string remote=Path.Combine(root,"remote");
  if(Directory.Exists(remote))Directory.Delete(remote,true);
  string state=Path.Combine(root,"remote-session.json");
  if(File.Exists(state))File.Delete(state);
 }
 internal static bool SameFile(ZipArchiveEntry entry,string path) {
  if(!File.Exists(path)||new FileInfo(path).Length!=entry.Length)return false;
  using(var sha=SHA256.Create())
  using(var incoming=entry.Open())
  using(var existing=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) {
   var a=sha.ComputeHash(incoming);var b=sha.ComputeHash(existing);
   for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;
   return true;
  }
 }
}
