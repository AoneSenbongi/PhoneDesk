// SPDX-License-Identifier: Apache-2.0
using System;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PhoneDesk {
 sealed partial class App {
  static string FieldText(IntPtr handle){
   var buffer=new StringBuilder(512);
   Native.GetWindowText(handle,buffer,buffer.Capacity);
   return buffer.ToString().Trim();
  }

  internal static string GatewayAddress(){
   foreach(var nic in NetworkInterface.GetAllNetworkInterfaces()){
    if(nic.OperationalStatus!=OperationalStatus.Up)continue;
    try{
     var properties=nic.GetIPProperties();
     if(properties==null)continue;
     foreach(var gateway in properties.GatewayAddresses){
      if(gateway.Address!=null&&gateway.Address.AddressFamily==AddressFamily.InterNetwork)return gateway.Address.ToString();
     }
    }catch{}
   }
   return "";
  }

  void ToggleWifiDebugSwitch(){
   Program.WifiDebug=!Program.WifiDebug;
   AudioSwitch.Refresh(controls[22]);
   LayoutShell();
   Notice("WiFiデバッグ: "+(Program.WifiDebug?"ON":"OFF"));
  }

  void ConnectWifiDebug(){
   string endpointText=FieldText(controls[19]);
   string pairingText=FieldText(controls[20]);
   if(endpointText.Length==0){
    string gateway=GatewayAddress();
    if(gateway.Length>0){endpointText=gateway;Native.SetWindowText(controls[19],gateway);}
   }
   string host;int port;
   if(!WifiDebug.TryEndpoint(endpointText,out host,out port)){Error("接続先を IP:ポート の形式で入力してください");return;}
   string endpoint=host+":"+port.ToString(CultureInfo.InvariantCulture);
   string pairEndpoint=null,pairCode=null;
   if(pairingText.Length>0&&!WifiDebug.TryPairing(pairingText,out pairEndpoint,out pairCode)){
    Error("ペアリングを IP:ポート 6桁コード の形式で入力してください");return;
   }
   Notice(pairEndpoint!=null?"WiFiデバッグをペアリング中...":"WiFiデバッグに接続中...");
   Task.Run(delegate{
    try{
     if(pairEndpoint!=null){
      var pair=Adb("pair "+pairEndpoint+" "+pairCode);
      if(pair.Code!=0)throw new IOException((pair.Error+pair.Output).Trim());
     }
     var connect=Adb("connect "+endpoint);
     if(connect.Code!=0)throw new IOException((connect.Error+connect.Output).Trim());
     string message=connect.Output.Trim();
     if(message.IndexOf("connected",StringComparison.OrdinalIgnoreCase)<0&&message.IndexOf("already",StringComparison.OrdinalIgnoreCase)<0)
      throw new IOException(message.Length>0?message:"接続に失敗しました");
     Dispatch(delegate{Notice("WiFi接続: "+endpoint);Poll();});
    }catch(Exception ex){Dispatch(delegate{Error("WiFi接続失敗: "+ex.Message);});}
   });
  }
 }
}
