// SPDX-License-Identifier: Apache-2.0
package com.phonedesk;

// Uses the display wrappers from the bundled scrcpy server, under the same ADB shell identity.
public final class ScreenPower {
    public static void main(String[] args) throws Exception {
        if(args.length!=1) throw new IllegalArgumentException("power mode required");
        if(args[0].equals("guard")) {
            // Like scrcpy cleanup, survive termination of the ADB shell session.
            Class.forName("android.system.Os").getMethod("setsid").invoke(null);
            System.out.println("READY");System.out.flush();
            if(System.in.read()!='0') setPower(true);
            Runtime.getRuntime().halt(0);
        }
        if(args[0].equals("watch-off")) {
            ProcessBuilder builder=new ProcessBuilder("app_process","/",ScreenPower.class.getName(),"guard");
            Process guard=builder.start();
            java.io.BufferedReader ready=new java.io.BufferedReader(new java.io.InputStreamReader(guard.getInputStream()));
            if(!"READY".equals(ready.readLine())) throw new IllegalStateException("Display guardian failed");
            try {
                setPower(false);
                System.out.println("OK watch-off");System.out.flush();
                if(System.in.read()=='0') {
                    guard.getOutputStream().write('0');guard.getOutputStream().flush();
                }
            } finally {
                guard.getOutputStream().close();
                guard.waitFor();
            }
            Runtime.getRuntime().halt(0);
        }
        if(!(args[0].equals("on") || args[0].equals("off") || args[0].equals("lock"))) throw new IllegalArgumentException("on/off/lock required");
        if(args[0].equals("lock")) {
            Object windows=Class.forName("android.view.WindowManagerGlobal").getMethod("getWindowManagerService").invoke(null);
            Class.forName("android.view.IWindowManager").getMethod("lockNow",Class.forName("android.os.Bundle"))
                .invoke(windows,new Object[]{null});
            System.out.println("OK lock");
            System.out.flush();
            Runtime.getRuntime().halt(0);
        }
        setPower(!args[0].equals("off"));
        System.out.println("OK "+args[0]);
        System.out.flush();
        // Avoid unloading Android system-server JNI through normal VM teardown.
        Runtime.getRuntime().halt(0);
    }
    private static void setPower(boolean on) throws Exception {
        Class<?> surface=Class.forName("com.genymobile.scrcpy.wrappers.SurfaceControl");
        int sdk=Class.forName("android.os.Build$VERSION").getField("SDK_INT").getInt(null);
        Class<?> displays=sdk>=34?Class.forName("com.genymobile.scrcpy.wrappers.DisplayControl"):surface;
        long[] ids=(long[])displays.getMethod("getPhysicalDisplayIds").invoke(null);
        if(ids==null || ids.length==0) throw new IllegalStateException("No physical display");
        for(long id:ids) {
            Object token=displays.getMethod("getPhysicalDisplayToken",long.class).invoke(null,id);
            Object ok=surface.getMethod("setDisplayPowerMode",Class.forName("android.os.IBinder"),int.class)
                .invoke(null,token,on?2:0);
            if(!Boolean.TRUE.equals(ok)) throw new IllegalStateException("Display power request failed");
        }
    }
}
