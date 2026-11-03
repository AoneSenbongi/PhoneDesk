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
   Console.WriteLine("PASS: waiting/maximized shell background (5 checks)");
  }finally{Native.DestroyWindow(shell);var font=Field(app,"font");Native.DeleteObject(font);GC.KeepAlive(app);}
 }
}
