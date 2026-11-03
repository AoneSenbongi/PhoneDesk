// SPDX-License-Identifier: Apache-2.0
package com.phonedesk;
import java.util.List;
import java.lang.reflect.Method;

public final class AlarmWatch {
    private static volatile boolean running=true;
    public static void main(String[] args) throws Exception {
        Class<?> binderType=Class.forName("android.os.IBinder");
        Object binder=Class.forName("android.os.ServiceManager").getMethod("getService",String.class).invoke(null,"audio");
        Object service=Class.forName("android.media.IAudioService$Stub").getMethod("asInterface",binderType).invoke(null,binder);
        Method configurations=Class.forName("android.media.IAudioService").getMethod("getActivePlaybackConfigurations");
        Class<?> config=Class.forName("android.media.AudioPlaybackConfiguration");
        Method attributes=config.getMethod("getAudioAttributes"),state=config.getMethod("getPlayerState");
        Method usage=Class.forName("android.media.AudioAttributes").getMethod("getUsage");
        Thread input=new Thread(new Runnable(){public void run(){try{while(System.in.read()!=-1){}}catch(Exception ignored){}finally{running=false;}}});input.setDaemon(true);input.start();
        boolean current=false;long lastActive=0;int emitted=-1;
        try{
            while(running){
                boolean active=false;
                for(Object item:(List<?>)configurations.invoke(service)){
                    if(((Integer)state.invoke(item))==2&&((Integer)usage.invoke(attributes.invoke(item)))==4){active=true;break;}
                }
                long now=System.nanoTime();if(active){lastActive=now;current=true;}else if(now-lastActive>=750000000L)current=false;
                int value=current?1:0;
                if(value!=emitted){System.out.println("ALARM "+value);System.out.flush();emitted=value;}
                Thread.sleep(250);
            }
        }finally{if(emitted==1){System.out.println("ALARM 0");System.out.flush();}}
        Runtime.getRuntime().halt(0);
    }
}
