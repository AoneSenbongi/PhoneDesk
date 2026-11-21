// SPDX-License-Identifier: Apache-2.0
using System;
using System.Runtime.InteropServices;
using PhoneDesk;

class ViewerTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok){Console.WriteLine("FAIL: "+name);Environment.Exit(1);}checks++;}
 [DllImport("user32.dll")] static extern bool PeekMessage(out Native.Msg m,IntPtr window,uint min,uint max,uint remove);
 [DllImport("user32.dll")] static extern bool IsWindow(IntPtr window);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")] static extern IntPtr GetDlgItem(IntPtr window,int id);
 [DllImport("user32.dll")] static extern IntPtr GetFocus();
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,System.Text.StringBuilder text,int length);
 static void Main() {
  TestDesktop.Enter();
  Native.SetProcessDPIAware();
  var child=Native.CreateWindowEx(0,"STATIC","stream-test",0x00CF0000,20,20,300,700,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
  var shell=Native.CreateWindowEx(0,"STATIC","PhoneDesk test shell",0x02CF0000,40,40,610,290,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);

  Native.WndProc handler=delegate(IntPtr h,uint m,IntPtr w,IntPtr l){return Native.DefWindowProc(h,m,w,l);};
  Native.Subclass(child,-4,handler);
  Native.Rect originalClient;Native.GetClientRect(child,out originalClient);
  double aspect=(double)originalClient.Right/originalClient.Bottom;
  Viewer current=null;
  Native.WndProc shellHandler=delegate(IntPtr h,uint m,IntPtr w,IntPtr l){if(current!=null){if(m==0x0005)current.Resize();if(m==0x0024||m==0x0214)return current.Geometry(m,w,l);}return Native.DefWindowProc(h,m,w,l);};
  Native.Subclass(shell,-4,shellHandler);
  using(var viewer=new Viewer(shell,child,IntPtr.Zero,delegate(string mode){},delegate{})) {
   current=viewer;viewer.Fit(420);viewer.Reveal();
   Check(viewer.VideoWidth>0,"video fills available width");
   Check(Native.GetParent(viewer.Window)==shell,"viewer reuses existing top-level shell");
   Check(Native.GetParent(child)==viewer.Window,"stream is a child of viewer");
   long style=Native.GetStyle(child,-16).ToInt64();
   Check((style&0x40000000)!=0&&(style&0x00CF0000)==0,"stream frame is removed");
   Native.Rect initial;Native.GetClientRect(shell,out initial);
   Native.Rect streamInitial,hostInitial;Native.GetWindowRect(child,out streamInitial);Native.GetWindowRect(viewer.Window,out hostInitial);
   Check(streamInitial.Top==hostInitial.Top+44,"toolbar visible above stream when details collapsed");
   Check(Math.Abs(initial.Right-(initial.Bottom-44)*aspect)<2,"initial viewport keeps stream aspect without side bars");
   var sizing=new Native.Rect{Left=40,Top=40,Right=640,Bottom=740};
   IntPtr rect=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Native.Rect)));
   try {
    Marshal.StructureToPtr(sizing,rect,false);Native.SendMessage(shell,0x0214,new IntPtr(2),rect);
    sizing=(Native.Rect)Marshal.PtrToStructure(rect,typeof(Native.Rect));
    Native.Rect outer,inner;Native.GetWindowRect(shell,out outer);Native.GetClientRect(shell,out inner);
    int frameW=outer.Right-outer.Left-inner.Right,frameH=outer.Bottom-outer.Top-inner.Bottom;
    Check(Math.Abs((sizing.Right-sizing.Left-frameW)-(sizing.Bottom-sizing.Top-frameH-44)*aspect)<2,"interactive resize keeps aspect");
   }finally{Marshal.FreeHGlobal(rect);}
   Native.ShowWindow(shell,3);
   Native.Rect maximized;Native.GetClientRect(shell,out maximized);
   Check(Math.Abs(maximized.Right-(maximized.Bottom-44)*aspect)<2,"maximize keeps aspect without side bars");
   Native.ShowWindow(shell,9);
   Native.Rect beforeDetails;Native.GetWindowRect(child,out beforeDetails);
   viewer.SetDetails(true);
   Check(!IsWindowVisible(GetDlgItem(viewer.Window,23))&&viewer.ToolbarHeight==0,"opening details hides toolbar buttons");
   Native.Rect afterDetails;Native.GetWindowRect(child,out afterDetails);
   Native.Rect expandedHost;Native.GetWindowRect(viewer.Window,out expandedHost);
   Check(afterDetails.Left==expandedHost.Left+320,"details occupies left side of video");
   Check(beforeDetails.Right-beforeDetails.Left==afterDetails.Right-afterDetails.Left&&beforeDetails.Bottom-beforeDetails.Top==afterDetails.Bottom-afterDetails.Top,"opening details preserves video size");
   Native.Rect expanded;Native.GetClientRect(shell,out expanded);
   Check(viewer.DetailsWidth==320&&Math.Abs(expanded.Right-320-(expanded.Bottom)*aspect)<2,"details expand right without changing video aspect");
   Native.ShowWindow(shell,3);Native.Rect expandedMax;Native.GetClientRect(shell,out expandedMax);
   Check(Math.Abs(expandedMax.Right-320-(expandedMax.Bottom)*aspect)<2,"maximize with sidebar keeps video aspect");
   Native.ShowWindow(shell,9);
   Native.SetFocus(GetDlgItem(viewer.Window,23));
   viewer.SetDetails(false);
   Check(GetFocus()==child,"closing details restores keyboard focus to stream");
   Check(IsWindowVisible(GetDlgItem(viewer.Window,23))&&viewer.ToolbarHeight==44,"closing details restores accessible details button");
   Check(viewer.DetailsWidth==0,"details collapse");
   Native.SetWindowPos(shell,IntPtr.Zero,40,40,600,700,0x0014);
   Native.Rect client,stream;Native.GetClientRect(viewer.Window,out client);Native.GetWindowRect(child,out stream);
   Check(stream.Right-stream.Left==client.Right&&stream.Bottom-stream.Top==client.Bottom-44,"stream follows resized client below toolbar");
   var update=typeof(Viewer).GetMethod("UpdateFrameSize",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   Check(update!=null,"stream frame changes must update viewer aspect");
   viewer.Fit(420);
   update.Invoke(viewer,new object[]{1920,864});
   Native.GetClientRect(shell,out client);
   Check(client.Right>client.Bottom&&Math.Abs(client.Right-(client.Bottom-44)/aspect)<3,"rotation fits landscape without black bars");
   viewer.SetDetails(true);Native.GetClientRect(shell,out client);
   Check(Math.Abs(client.Right-320-client.Bottom/aspect)<3,"landscape details preserves aspect");
   Native.ShowWindow(shell,3);
   update.Invoke(viewer,new object[]{864,1920});Native.GetClientRect(shell,out client);
   Check(Math.Abs(client.Right-320-client.Bottom*aspect)<3,"rotation while maximized fits portrait");
   Native.ShowWindow(shell,9);viewer.SetDetails(false);
   update.Invoke(viewer,new object[]{1920,864});
   update.Invoke(viewer,new object[]{864,1920});Native.GetClientRect(shell,out client);
   Check(Math.Abs(client.Right-(client.Bottom-44)*aspect)<3,"repeat rotation restores portrait aspect");
   Check(GetDlgItem(viewer.Window,24)!=IntPtr.Zero,"right rotation button exists");
   var rotationCaption=new System.Text.StringBuilder(30);GetWindowText(GetDlgItem(viewer.Window,24),rotationCaption,30);
   Check(rotationCaption.ToString()=="回転","compact rotation label fits narrow toolbar");
   viewer.SetDeviceRotation(true);Native.GetClientRect(shell,out client);
   Check(viewer.Rotated&&Math.Abs(client.Right-(client.Bottom-44)*aspect)<3,"rotation button does not rotate PC alone");
   viewer.UpdateFrameSize(1920,864);Native.GetClientRect(shell,out client);
   Check(Math.Abs(client.Right-(client.Bottom-44)/aspect)<3,"viewer follows physical landscape frame");
   viewer.SetDeviceRotation(false);viewer.UpdateFrameSize(864,1920);Native.GetClientRect(shell,out client);
   Check(!viewer.Rotated&&Math.Abs(client.Right-(client.Bottom-44)*aspect)<3,"reset follows physical portrait frame");
   viewer.SetDetails(true);
   Native.Rect resizedBefore;Native.GetWindowRect(child,out resizedBefore);
   viewer.SetDetailsWidth(420);
   Check(viewer.DetailsWidth==420,"details width accepts a manual resize");
   Native.Rect resizedAfter;Native.GetWindowRect(child,out resizedAfter);
   Check(resizedAfter.Right-resizedAfter.Left==resizedBefore.Right-resizedBefore.Left&&resizedAfter.Bottom-resizedAfter.Top==resizedBefore.Bottom-resizedBefore.Top,"resizing details preserves video size");
   Native.GetClientRect(shell,out client);
   Check(Math.Abs(client.Right-420-client.Bottom*aspect)<2,"resized details keeps video aspect");
   viewer.SetDetailsWidth(1);
   Check(viewer.DetailsWidth==Viewer.MinimumDetailsWidth,"details width clamps to the minimum");
   var monitorInfo=new Native.MonitorInfo{Size=Marshal.SizeOf(typeof(Native.MonitorInfo))};
   Native.GetMonitorInfo(Native.MonitorFromWindow(shell,2),ref monitorInfo);
   int limit=Math.Max(Viewer.MinimumDetailsWidth,Math.Min(Viewer.MaximumDetailsWidth,monitorInfo.Work.Right-monitorInfo.Work.Left-400));
   viewer.SetDetailsWidth(4000);
   Check(viewer.DetailsWidth==limit&&limit<4000,"details width clamps to the monitor");
   viewer.SetDetailsWidth(400);viewer.SetDetails(false);viewer.SetDetails(true);
   Check(viewer.DetailsWidth==400,"reopening details keeps the resized width");
   viewer.SetDetailsWidth(Viewer.DefaultDetailsWidth);
   current=null;
  }
  Check(IsWindow(shell),"disposing stream viewer keeps original shell alive");
  Native.DestroyWindow(shell);GC.KeepAlive(handler);GC.KeepAlive(shellHandler);
  Console.WriteLine("PASS: viewer window integration ("+checks+" checks)");
 }
}
