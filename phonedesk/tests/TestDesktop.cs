// SPDX-License-Identifier: Apache-2.0
using System;
using System.Runtime.InteropServices;
static class TestDesktop {
 static IntPtr desktop;
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateDesktop(string name,IntPtr device,IntPtr mode,uint flags,uint access,IntPtr security);
 [DllImport("user32.dll",SetLastError=true)]static extern bool SetThreadDesktop(IntPtr h);
 internal static void Enter(){
  desktop=CreateDesktop("PhoneDesk-tests-"+Guid.NewGuid().ToString("N"),IntPtr.Zero,IntPtr.Zero,0,0x1ff,IntPtr.Zero);
  if(desktop==IntPtr.Zero||!SetThreadDesktop(desktop))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
 }
}
