// SPDX-License-Identifier: Apache-2.0
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace PhoneDesk {
 sealed partial class App {
  const string PhoneViewVersion="1.0"; // keep in sync with phone-view/AndroidManifest.xml
  PcViewServer pcView;
  static App(){PcViewLog.Write=Program.Log;}

  void TogglePcView(){
   if(pcView!=null){StopPcView();Notice("PC画面の共有を停止しました");return;}
   Device device=Selected();
   if(device==null||device.State!="device"){Error("端末を選択してください。");return;}
   try{
    var server=new PcViewServer(Path.Combine(runtime,"adb.exe"),device.Serial);
    string url=server.Start();
    pcView=server;
    Native.SetWindowText(controls[17],"共有を停止");
    try{
     LaunchPhoneView(device.Serial,url);
     Notice("スマホで PhoneDesk View が開きます");
    }catch(Exception ex){
     Notice("スマホ側の起動に失敗: "+ex.Message+" URL: "+url);
    }
   }catch(Exception ex){Error("PC画面の共有を開始できません: "+ex.Message);}
  }

  void LaunchPhoneView(string serial,string url){
   string adb=Path.Combine(runtime,"adb.exe");
   string apk=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"phone-desk-view.apk");
   if(!File.Exists(apk))throw new FileNotFoundException("APKが見つかりません",apk);
   string token=PcViewProtocol.QueryValue(new Uri(url).Query,"t")??"";
   if(!IsPhoneViewCurrent(adb,serial,apk))RunAdb(adb,serial,"install -r \""+apk+"\"");
   RunAdb(adb,serial,"shell am force-stop com.phonedesk.view");
   RunAdb(adb,serial,"shell am start -n com.phonedesk.view/.MainActivity --es token "+token);
  }

  static bool IsPhoneViewCurrent(string adb,string serial,string apk){
   string path=RunAdb(adb,serial,"shell pm path com.phonedesk.view");
   if(!path.StartsWith("package:",StringComparison.Ordinal))return false;
   path=path.Substring(8).Trim();
   if(path.Length==0)return false;
   string remote;
   try{
    remote=RunAdb(adb,serial,"shell md5sum \""+path+"\"");
    int split=remote.IndexOf(' ');
    if(split>=0)remote=remote.Substring(0,split);
    remote=remote.Trim();
    if(remote.Length>0){
     string local;
     using(var md5=MD5.Create())
     using(var stream=File.OpenRead(apk)){
      local=BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
     }
     return string.Equals(remote,local,StringComparison.OrdinalIgnoreCase);
    }
   }catch{}
   // Fallback for devices without md5sum: compare the installed versionName.
   try{
    string dump=RunAdb(adb,serial,"shell dumpsys package com.phonedesk.view");
    foreach(string line in dump.Split('\n')){
     int index=line.IndexOf("versionName=",StringComparison.Ordinal);
     if(index<0)continue;
     string version=line.Substring(index+12).Trim();
     return string.Equals(version,PhoneViewVersion,StringComparison.Ordinal);
    }
   }catch{}
   return false;
  }

  static string RunAdb(string adb,string serial,string args){
   using(var process=new Process()){
    process.StartInfo=new ProcessStartInfo(adb,"-s "+DeviceParser.Quote(serial)+" "+args){
     UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
    process.Start();
    string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();
    if(!process.WaitForExit(15000)){try{process.Kill();}catch{}throw new TimeoutException("ADB応答なし");}
    if(process.ExitCode!=0)throw new IOException((error+output).Trim());
    return output.Trim();
   }
  }

  void StopPcView(){
   var server=pcView;pcView=null;
   if(server==null)return;
   try{server.Dispose();}catch{}
   Device device=Selected();
   if(device!=null){try{RunAdb(Path.Combine(runtime,"adb.exe"),device.Serial,"shell am force-stop com.phonedesk.view");}catch{}}
   IntPtr handle;
   if(controls.TryGetValue(17,out handle))Native.SetWindowText(handle,"PC画面を共有");
  }
 }
}
