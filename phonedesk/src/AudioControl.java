// SPDX-License-Identifier: Apache-2.0
package com.phonedesk;

public final class AudioControl {
    public static void main(String[] args) throws Exception {
        Object binder=Class.forName("android.os.ServiceManager").getMethod("getService",String.class).invoke(null,"audio");
        Object audio=Class.forName("android.media.IAudioService$Stub").getMethod("asInterface",Class.forName("android.os.IBinder")).invoke(null,binder);
        Class<?> api=Class.forName("android.media.IAudioService");
        int min=(Integer)api.getMethod("getStreamMinVolume",int.class).invoke(audio,3);
        int max=(Integer)api.getMethod("getStreamMaxVolume",int.class).invoke(audio,3);
        int value=(Integer)api.getMethod("getStreamVolume",int.class).invoke(audio,3);
        if(args.length>0&&!args[0].equals("get")) {
            int target=args[0].equals("up")?value+1:args[0].equals("down")?value-1:Integer.parseInt(args[0]);
            target=Math.max(min,Math.min(max,target));
            api.getMethod("setStreamVolume",int.class,int.class,int.class,String.class).invoke(audio,3,target,1,"com.android.shell");
            value=(Integer)api.getMethod("getStreamVolume",int.class).invoke(audio,3);
            if(value!=target)throw new IllegalStateException("Volume was not applied");
        }
        System.out.println("VOLUME "+value+" "+min+" "+max);System.out.flush();
        Runtime.getRuntime().halt(0);
    }
}
