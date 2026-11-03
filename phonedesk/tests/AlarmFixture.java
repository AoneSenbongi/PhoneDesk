// SPDX-License-Identifier: Apache-2.0
import android.media.AudioTrack;
import android.media.AudioFormat;
import android.media.AudioAttributes;
public final class AlarmFixture {
 public static void main(String[] args)throws Exception{
  int usage=args[0].equals("alarm")?AudioAttributes.USAGE_ALARM:AudioAttributes.USAGE_MEDIA;
  short[] silence=new short[48000];
  AudioTrack track=new AudioTrack.Builder().setAudioAttributes(new AudioAttributes.Builder().setUsage(usage).setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION).build()).setAudioFormat(new AudioFormat.Builder().setSampleRate(48000).setEncoding(AudioFormat.ENCODING_PCM_16BIT).setChannelMask(AudioFormat.CHANNEL_OUT_MONO).build()).setTransferMode(AudioTrack.MODE_STATIC).setBufferSizeInBytes(silence.length*2).build();
  try{track.write(silence,0,silence.length);track.setLoopPoints(0,silence.length,-1);track.play();Thread.sleep(3000);}finally{track.stop();track.release();}System.exit(0);
 }
}
