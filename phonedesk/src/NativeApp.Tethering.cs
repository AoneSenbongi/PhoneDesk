// SPDX-License-Identifier: Apache-2.0
using System;
using System.IO;
using System.Threading.Tasks;

namespace PhoneDesk {
 sealed partial class App {
  const int UsbTetherButton=21;

  void ToggleUsbTethering(){
   var d=Selected();
   if(d==null||d.State!="device"){Error("操作可能な端末なし");return;}
   string prefix="-s "+DeviceParser.Quote(d.Serial)+" shell svc usb ";
   Notice("USBテザリングを切り替え中...");
   Task.Run(delegate{
    try{
     bool on;
     var current=Adb(prefix+"getFunctions");
     if(current.Code==0)on=current.Output.IndexOf("rndis",StringComparison.OrdinalIgnoreCase)>=0;
     else on=Program.UsbTethering;
     bool next=!on;
     // Keep ADB alive when the device accepts multiple USB functions.
     var r=Adb(prefix+"setFunctions "+(next?"rndis,adb":"mtp,adb"));
     if(r.Code!=0)r=Adb(prefix+"setFunctions "+(next?"rndis":"mtp"));
     if(r.Code!=0)throw new IOException((r.Error+r.Output).Trim());
     Program.UsbTethering=next;
     Dispatch(delegate{
      AudioSwitch.Refresh(controls[UsbTetherButton]);
      Notice("USBテザリング: "+(next?"ON":"OFF"));
     });
    }catch(Exception ex){Dispatch(delegate{Error("USBテザリング切替失敗: "+ex.Message);});}
   });
  }
 }
}
