// SPDX-License-Identifier: Apache-2.0
package com.phonedesk;
import java.lang.reflect.*;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
public final class ClipboardWatch {
 public static void main(String[]args)throws Exception {
  final Class<?> looper=Class.forName("android.os.Looper");
  looper.getMethod("prepareMainLooper").invoke(null);
  Class.forName("com.genymobile.scrcpy.Workarounds").getMethod("apply").invoke(null);
  final Object clipboard=Class.forName("com.genymobile.scrcpy.wrappers.ServiceManager").getMethod("getClipboardManager").invoke(null);
  Class<?> listenerType=Class.forName("android.content.ClipboardManager$OnPrimaryClipChangedListener");
  Object listener=Proxy.newProxyInstance(listenerType.getClassLoader(),new Class<?>[]{listenerType},new InvocationHandler(){
   public Object invoke(Object proxy,Method method,Object[]params)throws Exception {
    if(method.getName().equals("hashCode"))return System.identityHashCode(proxy);
    if(method.getName().equals("equals"))return proxy==params[0];
    if(method.getName().equals("toString"))return "PhoneDesk clipboard listener";
    if(method.getName().equals("onPrimaryClipChanged")){
     CharSequence text=(CharSequence)clipboard.getClass().getMethod("getText").invoke(clipboard);
     if(text!=null){System.out.println("CLIP "+Base64.getEncoder().encodeToString(text.toString().getBytes(StandardCharsets.UTF_8)));System.out.flush();}
    }
    return null;
   }
  });
  clipboard.getClass().getMethod("addPrimaryClipChangedListener",listenerType).invoke(clipboard,listener);
  Thread stop=new Thread(new Runnable(){public void run(){try{System.in.read();}catch(Exception ignored){}Runtime.getRuntime().halt(0);}});
  stop.setDaemon(true);stop.start();System.out.println("READY");System.out.flush();
  looper.getMethod("loop").invoke(null);
 }
}
