// SPDX-License-Identifier: Apache-2.0
using System;
using System.Runtime.InteropServices;

namespace PhoneDesk {
 sealed class Viewer : IDisposable {
  internal IntPtr Window {get;private set;}
  readonly IntPtr child;
  internal readonly IntPtr Shell;
  readonly Native.WndProc callback;
  readonly Action<string> power;
  readonly Action details;
  readonly IntPtr[] buttons=new IntPtr[5];
  double aspect;bool frameLandscape;
  internal bool Rotated {get;private set;}
  internal int DetailsWidth {get;private set;}
  internal int ToolbarHeight {get{return DetailsWidth==0?44:0;}}
  internal int VideoWidth {get{Native.Rect r;Native.GetClientRect(Shell,out r);return Math.Max(1,r.Right-DetailsWidth);}}
  int compactWidth=420;
  internal Viewer(IntPtr shell,IntPtr stream,IntPtr font,Action<string> powerAction,Action toggleDetails) {
   Shell=shell;child=stream;power=powerAction;details=toggleDetails;
   Native.Rect source;Native.GetClientRect(child,out source);
   aspect=(double)Math.Max(1,source.Right)/Math.Max(1,source.Bottom);frameLandscape=aspect>1;
   Window=Native.CreateWindowEx(0,"STATIC","",unchecked((int)0x52000000),0,0,1,1,Shell,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
   if(Window==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   callback=WindowProc;Native.Subclass(Window,-4,callback);
   string[] labels={"点灯","消灯","ロック","回転","詳細"};int[] ids={20,21,22,24,23};
   for(int i=0;i<labels.Length;i++) {
    buttons[i]=Native.CreateWindowEx(0,"BUTTON",labels[i],0x50010000,0,0,1,1,Window,new IntPtr(ids[i]),IntPtr.Zero,IntPtr.Zero);
    Native.SendMessage(buttons[i],0x0030,font,new IntPtr(1));
   }
   // SetParent does not update WS_CHILD / WS_POPUP or remove the old frame.
   long style=Native.GetStyle(child,-16).ToInt64();
   Native.SetStyle(child,-16,new IntPtr((style&~0x80CF0000L)|0x40000000L));
   Native.SetParent(child,Window);
   if(Native.GetParent(child)!=Window){Dispose();throw new InvalidOperationException("画面の統合失敗");}
   Layout();Native.ShowWindow(child,5);
  }
  internal void Fit(int preferredWidth,int leftShift=0) {
   int fw,fh;Frame(out fw,out fh);
   var monitor=Monitor();
   if(DetailsWidth>0)preferredWidth=Math.Max(preferredWidth,(int)Math.Ceiling(476*aspect));
   preferredWidth=Math.Max(1,Math.Min(preferredWidth,monitor.Work.Right-monitor.Work.Left-fw-DetailsWidth));
   int height=Math.Min(monitor.Work.Bottom-monitor.Work.Top,(int)Math.Round(preferredWidth/aspect)+ToolbarHeight+fh);
   int width=(int)Math.Round((height-fh-ToolbarHeight)*aspect)+fw+DetailsWidth;
   Native.Rect position;Native.GetWindowRect(Shell,out position);
   Native.SetWindowPos(Shell,IntPtr.Zero,Math.Max(monitor.Work.Left,Math.Min(position.Left+leftShift,monitor.Work.Right-width)),Math.Max(monitor.Work.Top,Math.Min(position.Top,monitor.Work.Bottom-height)),width,height,0x0014);
   Resize();
  }
  internal void UpdateFrameSize(int width,int height) {
   if(width<=0||height<=0||(width>height)==frameLandscape)return;
   frameLandscape=width>height;FlipAspect();
  }
  internal void SetDeviceRotation(bool rotated){Rotated=rotated;Native.SetWindowText(buttons[3],rotated?"戻す":"回転");}
  internal void ToggleRotation(){power("rotate");}
  void FlipAspect(){
   Native.Rect r;Native.GetClientRect(Shell,out r);
   int preferred=Math.Max(1,r.Bottom-ToolbarHeight);
   bool maximized=(Native.GetStyle(Shell,-16).ToInt64()&0x01000000)!=0;
   if(maximized)Native.ShowWindow(Shell,9);
   aspect=1/aspect;
   Fit(preferred);
   compactWidth=VideoWidth;
   if(maximized)Native.ShowWindow(Shell,3);
  }
  internal void SetDetails(bool expanded) {
   Native.Rect r;Native.GetClientRect(Shell,out r);
   int videoWidth=r.Right-DetailsWidth;
   if(DetailsWidth==0)compactWidth=videoWidth;
   int previousWidth=DetailsWidth;
   DetailsWidth=expanded?320:0;
   Fit(expanded?videoWidth:compactWidth,previousWidth-DetailsWidth);
   if(!expanded)Focus();
  }
  internal void Resize() {
   Native.Rect r;Native.GetClientRect(Shell,out r);
   Native.SetWindowPos(Window,IntPtr.Zero,0,0,r.Right,r.Bottom,0x0014);Layout();
  }
  void Frame(out int width,out int height) {
   Native.Rect outer,client;Native.GetWindowRect(Shell,out outer);Native.GetClientRect(Shell,out client);
   width=outer.Right-outer.Left-client.Right;height=outer.Bottom-outer.Top-client.Bottom;
  }
  Native.MonitorInfo Monitor(){var info=new Native.MonitorInfo{Size=Marshal.SizeOf(typeof(Native.MonitorInfo))};Native.GetMonitorInfo(Native.MonitorFromWindow(Shell,2),ref info);return info;}
  void Layout() {
   if(Window==IntPtr.Zero)return;
   Native.Rect r;Native.GetClientRect(Window,out r);
   int videoWidth=Math.Max(1,r.Right-DetailsWidth);
   for(int i=0;i<buttons.Length;i++) {
    Native.ShowWindow(buttons[i],DetailsWidth==0?5:0);
    int left=6+i*(videoWidth-6)/buttons.Length,right=6+(i+1)*(videoWidth-6)/buttons.Length-6;
    Native.SetWindowPos(buttons[i],IntPtr.Zero,left,6,Math.Max(1,right-left),32,0x0014);
   }
   Native.SetWindowPos(child,IntPtr.Zero,DetailsWidth,ToolbarHeight,videoWidth,Math.Max(1,r.Bottom-ToolbarHeight),0x0034);
  }
  IntPtr WindowProc(IntPtr h,uint message,IntPtr w,IntPtr l) {
   try {
    if(message==0x0005){Layout();return IntPtr.Zero;}
    if(message==0x0007){Native.SetFocus(child);return IntPtr.Zero;}
    if(message==0x0111) {
     int id=w.ToInt32()&65535;
     if(id==20)power("on");else if(id==21)power("off");else if(id==22)power("lock");else if(id==23){details();return IntPtr.Zero;}else if(id==24)ToggleRotation();
     Native.SetFocus(child);return IntPtr.Zero;
    }
    if(message==0x0014){Native.Rect r;Native.GetClientRect(h,out r);Native.FillRect(w,ref r,new IntPtr(16));return new IntPtr(1);}
   }catch(Exception ex){Native.SetWindowText(Shell,"PhoneDesk | "+ex.Message);}
   return Native.DefWindowProc(h,message,w,l);
  }
  internal IntPtr Geometry(uint message,IntPtr w,IntPtr l) {
    if(message==0x0024) {
     int fw,fh;Frame(out fw,out fh);var info=Monitor();
     int height=info.Work.Bottom-info.Work.Top,width=(int)Math.Round((height-fh-ToolbarHeight)*aspect)+fw+DetailsWidth;
     if(width>info.Work.Right-info.Work.Left){width=info.Work.Right-info.Work.Left;height=(int)Math.Round((width-fw-DetailsWidth)/aspect)+fh+ToolbarHeight;}
     Marshal.WriteInt32(l,8,width);Marshal.WriteInt32(l,12,height);
     Marshal.WriteInt32(l,16,info.Work.Left-info.Monitor.Left+(info.Work.Right-info.Work.Left-width)/2);
     Marshal.WriteInt32(l,20,info.Work.Top-info.Monitor.Top);
     int minWidth=Math.Min(width,340+fw+DetailsWidth);
     Marshal.WriteInt32(l,24,minWidth);Marshal.WriteInt32(l,28,(int)Math.Round((minWidth-fw-DetailsWidth)/aspect)+fh+ToolbarHeight);
     Marshal.WriteInt32(l,32,width);Marshal.WriteInt32(l,36,height);
     return IntPtr.Zero;
    }
    if(message==0x0214) {
     var r=(Native.Rect)Marshal.PtrToStructure(l,typeof(Native.Rect));int fw,fh;Frame(out fw,out fh);int edge=w.ToInt32();
     int width=r.Right-r.Left,height=r.Bottom-r.Top;
     if(edge==1||edge==2)height=(int)Math.Round((width-fw-DetailsWidth)/aspect)+fh+ToolbarHeight;
     else width=(int)Math.Round((height-fh-ToolbarHeight)*aspect)+fw+DetailsWidth;
     if(edge==1||edge==4||edge==7)r.Left=r.Right-width;else r.Right=r.Left+width;
     if(edge==3||edge==4||edge==5)r.Top=r.Bottom-height;else r.Bottom=r.Top+height;
     Marshal.StructureToPtr(r,l,false);return new IntPtr(1);
    }
    return IntPtr.Zero;
  }
  internal void Focus(){if(Window!=IntPtr.Zero)Native.SetFocus(child);}
  internal void Reveal(){if(Window==IntPtr.Zero)return;Native.ShowWindow(Window,5);Native.ShowWindow(Shell,9);Native.SetForegroundWindow(Shell);Focus();}
  public void Dispose(){if(Window!=IntPtr.Zero){Native.DestroyWindow(Window);Window=IntPtr.Zero;}}
 }
}
