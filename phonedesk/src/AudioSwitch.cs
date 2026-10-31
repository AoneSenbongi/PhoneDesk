// SPDX-License-Identifier: Apache-2.0
using System;using System.Runtime.InteropServices;
namespace PhoneDesk {
 static class AudioSwitch {
  [StructLayout(LayoutKind.Sequential)]struct Item{internal uint Type,Id,ItemId,Action,State;internal IntPtr Window,DC;internal Native.Rect Bounds;internal UIntPtr Data;}
  [DllImport("user32.dll")]static extern bool InvalidateRect(IntPtr h,IntPtr rect,bool erase);
  [DllImport("user32.dll")]static extern IntPtr GetSysColorBrush(int index);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int DrawText(IntPtr dc,string text,int count,ref Native.Rect rect,uint format);
  [DllImport("user32.dll")]static extern bool DrawFocusRect(IntPtr dc,ref Native.Rect rect);
  [DllImport("gdi32.dll")]static extern IntPtr CreateSolidBrush(uint color);
  [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")]static extern IntPtr GetStockObject(int index);
  [DllImport("gdi32.dll")]static extern bool RoundRect(IntPtr dc,int l,int t,int r,int b,int w,int h);
  [DllImport("gdi32.dll")]static extern bool Ellipse(IntPtr dc,int l,int t,int r,int b);
  [DllImport("gdi32.dll")]static extern int SetBkMode(IntPtr dc,int mode);
  [DllImport("gdi32.dll")]static extern uint SetTextColor(IntPtr dc,uint color);
  internal static void Refresh(IntPtr h){InvalidateRect(h,IntPtr.Zero,true);}
  internal static void Draw(IntPtr data,bool enabled,IntPtr font){
   var item=(Item)Marshal.PtrToStructure(data,typeof(Item));var bounds=item.Bounds;
   Native.FillRect(item.DC,ref bounds,GetSysColorBrush(15));
   var oldFont=SelectObject(item.DC,font);int oldMode=SetBkMode(item.DC,1);uint oldText=SetTextColor(item.DC,0);
   var label=bounds;label.Left+=4;label.Right-=58;DrawText(item.DC,"PCから音を出す",-1,ref label,0x824);
   int left=bounds.Right-50,top=bounds.Top+(bounds.Bottom-bounds.Top-24)/2;
   var track=CreateSolidBrush(enabled?0xD77800u:0x888888u);var thumb=CreateSolidBrush(0xFFFFFF);
   var oldPen=SelectObject(item.DC,GetStockObject(8));var oldBrush=SelectObject(item.DC,track);
   RoundRect(item.DC,left,top,left+46,top+24,24,24);SelectObject(item.DC,thumb);
   int knob=left+(enabled?24:2);Ellipse(item.DC,knob,top+2,knob+20,top+22);
   SelectObject(item.DC,oldBrush);SelectObject(item.DC,oldPen);Native.DeleteObject(track);Native.DeleteObject(thumb);
   SetTextColor(item.DC,oldText);SetBkMode(item.DC,oldMode);SelectObject(item.DC,oldFont);
   if((item.State&0x10)!=0)DrawFocusRect(item.DC,ref bounds);
  }
 }
}
