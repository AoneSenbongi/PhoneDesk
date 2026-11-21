// SPDX-License-Identifier: Apache-2.0
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using PhoneDesk;
class ShellTests {
 [DllImport("user32.dll")]static extern uint GetSysColor(int index);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleBitmap(IntPtr dc,int w,int h);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")]static extern uint GetPixel(IntPtr dc,int x,int y);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr h);
 [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr h,IntPtr dc);
 [DllImport("user32.dll")]static extern IntPtr GetDlgItem(IntPtr h,int id);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 static IntPtr Field(App app,string name){return (IntPtr)typeof(App).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(app);}
 static void Check(bool ok,string msg){if(!ok){Console.WriteLine("FAIL: "+msg);Environment.Exit(1);}}
 static void Main(){
  TestDesktop.Enter();
  var app=new App();app.InitializeWindow();var shell=Field(app,"window");var panel=Field(app,"detailsPanel");
  try{
   Native.ShowWindow(shell,3);Native.SendMessage(shell,5,IntPtr.Zero,IntPtr.Zero);
   Native.Rect client,details;Native.GetClientRect(shell,out client);Native.GetClientRect(panel,out details);
   Check(details.Right==client.Right&&details.Bottom==client.Bottom,"Waiting panel must cover maximized shell");
   var display=GetDC(shell);var dc=CreateCompatibleDC(display);var bitmap=CreateCompatibleBitmap(display,client.Right,client.Bottom);var old=SelectObject(dc,bitmap);
   try{
    foreach(var h in new[]{shell,panel}){
     Check(Native.SendMessage(h,0x14,dc,IntPtr.Zero)==new IntPtr(1),"Background erase must be handled");
     Check(GetPixel(dc,client.Right-2,client.Bottom-2)==GetSysColor(15),"Background must fill with system panel color");
    }
   }finally{SelectObject(dc,old);Native.DeleteObject(bitmap);DeleteDC(dc);ReleaseDC(shell,display);}
   var rotate=GetDlgItem(panel,15);var shot=GetDlgItem(panel,16);
   Check(rotate!=IntPtr.Zero&&shot!=IntPtr.Zero,"rotation and screenshot buttons exist in the details panel");
   Native.Rect rotateRect,shotRect;Native.GetWindowRect(rotate,out rotateRect);Native.GetWindowRect(shot,out shotRect);
   Check(shotRect.Top==rotateRect.Top&&rotateRect.Right<=shotRect.Left,"screenshot button shares the rotation row");
    var wifiConnect=GetDlgItem(panel,18);var wifiEndpoint=GetDlgItem(panel,19);var wifiPairing=GetDlgItem(panel,20);
    Check(wifiConnect!=IntPtr.Zero&&wifiEndpoint!=IntPtr.Zero&&wifiPairing!=IntPtr.Zero,"wifi debug controls exist in the details panel");
    Check(!IsWindowVisible(wifiConnect)&&!IsWindowVisible(wifiEndpoint)&&!IsWindowVisible(wifiPairing),"wifi debug controls hidden while off");
    var tether=GetDlgItem(panel,21);
    Check(tether!=IntPtr.Zero&&IsWindowVisible(tether),"usb tethering switch exists in the details panel");
    Check(!Program.UsbTethering,"usb tethering starts off");
    var wifiSwitch=GetDlgItem(panel,22);
    Check(wifiSwitch!=IntPtr.Zero&&IsWindowVisible(wifiSwitch),"wifi debug switch exists in the details panel");
    Check(!Program.WifiDebug,"wifi debug starts off");
   var splitter=Field(app,"splitter");
   Check(splitter!=IntPtr.Zero&&!IsWindowVisible(splitter),"details splitter stays hidden without a viewer");
   Native.ShowWindow(shell,9);
   var stream=Native.CreateWindowEx(0,"STATIC","stream-test",0x00CF0000,0,0,300,700,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
   var viewer=new Viewer(shell,stream,Field(app,"font"),delegate(string mode){},delegate{});
   var session=new Session();session.Viewer=viewer;
   var activeField=typeof(App).GetField("active",BindingFlags.Instance|BindingFlags.NonPublic);activeField.SetValue(app,session);
   viewer.Fit(420);viewer.SetDetails(true);
   var layout=typeof(App).GetMethod("LayoutShell",BindingFlags.Instance|BindingFlags.NonPublic);layout.Invoke(app,null);
    Program.WifiDebug=true;layout.Invoke(app,null);
    Check(IsWindowVisible(GetDlgItem(panel,19)),"wifi ip field shown while on");
    Program.WifiDebug=false;layout.Invoke(app,null);
    Check(!IsWindowVisible(GetDlgItem(panel,19)),"wifi ip field hidden while off");
   Native.Rect panelScreen,splitterScreen;Native.GetWindowRect(panel,out panelScreen);Native.GetWindowRect(splitter,out splitterScreen);
   Check(IsWindowVisible(splitter)&&splitterScreen.Right==panelScreen.Right&&splitterScreen.Right-splitterScreen.Left==6,"details splitter sits on the panel edge");
   int start=viewer.DetailsWidth;
   Native.SendMessage(splitter,0x0201,new IntPtr(1),new IntPtr((start-3)&0xFFFF));
   Native.SendMessage(splitter,0x0200,IntPtr.Zero,new IntPtr((start+97)&0xFFFF));
   Check(viewer.DetailsWidth==start+100,"dragging the splitter resizes the details panel");
   Native.SendMessage(splitter,0x0200,IntPtr.Zero,new IntPtr(1));
   Check(viewer.DetailsWidth==Viewer.MinimumDetailsWidth,"dragging the splitter clamps the details width");
   Native.SendMessage(splitter,0x0202,IntPtr.Zero,IntPtr.Zero);
   Check(Native.GetCapture()==IntPtr.Zero,"splitter drag releases the mouse capture");
   Native.ShowWindow(shell,3);layout.Invoke(app,null);
   int maximized=viewer.DetailsWidth;
   Check((Native.GetStyle(shell,-16).ToInt64()&0x01000000)!=0&&IsWindowVisible(splitter),"splitter is visible while maximized");
   Native.SendMessage(splitter,0x0201,new IntPtr(1),new IntPtr((maximized-3)&0xFFFF));
   Check((Native.GetStyle(shell,-16).ToInt64()&0x01000000)==0,"dragging while maximized restores the shell");
   Native.SendMessage(splitter,0x0200,IntPtr.Zero,new IntPtr((maximized+47)&0xFFFF));
   Native.SendMessage(splitter,0x0202,IntPtr.Zero,IntPtr.Zero);
   Check((Native.GetStyle(shell,-16).ToInt64()&0x01000000)!=0,"finishing the drag re-maximizes the shell");
   Check(viewer.DetailsWidth==maximized+50,"maximized drag still changes the details width");
   activeField.SetValue(app,null);viewer.Dispose();Native.DestroyWindow(stream);
   Console.WriteLine("PASS: waiting/maximized shell background (25 checks)");
  }finally{Native.DestroyWindow(shell);var font=Field(app,"font");Native.DeleteObject(font);GC.KeepAlive(app);}
 }
}
