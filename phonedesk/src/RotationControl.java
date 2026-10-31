// SPDX-License-Identifier: Apache-2.0
package com.phonedesk;
import java.io.*;
public final class RotationControl {
 static String run(String...args)throws Exception{
  Process p=new ProcessBuilder(args).redirectErrorStream(true).start();ByteArrayOutputStream output=new ByteArrayOutputStream();byte[] buffer=new byte[1024];int n;
  while((n=p.getInputStream().read(buffer))!=-1)output.write(buffer,0,n);
  if(p.waitFor()!=0)throw new IOException("Rotation command failed");return output.toString("UTF-8").trim();
 }
 public static void main(String[]args)throws Exception{
  if(args.length>0&&args[0].equals("guard")){
   Class.forName("android.system.Os").getMethod("setsid").invoke(null);
   String rotation=run("wm","user-rotation"),fixed=run("wm","fixed-to-user-rotation");
   if(!rotation.matches("free|lock [0-3]")||!fixed.matches("default|enabled|disabled|enabled_if_no_auto_rotation"))throw new IOException("Unknown rotation policy");
   try{
    run("wm","fixed-to-user-rotation","enabled");run("wm","user-rotation","lock","1");
    System.out.println("READY");System.out.flush();System.in.read();
   }finally{
    try{if(rotation.equals("free"))run("wm","user-rotation","free");else run("wm","user-rotation","lock",rotation.substring(5));}
    finally{run("wm","fixed-to-user-rotation",fixed);}
   }
  }else{
   Process guard=new ProcessBuilder("app_process","/",RotationControl.class.getName(),"guard").redirectError(ProcessBuilder.Redirect.INHERIT).start();
   BufferedReader ready=new BufferedReader(new InputStreamReader(guard.getInputStream()));
   if(!"READY".equals(ready.readLine()))throw new IOException("Rotation helper failed");
   System.out.println("READY");System.out.flush();
   try{System.in.read();}finally{guard.getOutputStream().close();if(guard.waitFor()!=0)throw new IOException("Rotation restore failed");}
  }
  Runtime.getRuntime().halt(0);
 }
}
