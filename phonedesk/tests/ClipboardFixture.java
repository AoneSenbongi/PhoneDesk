// SPDX-License-Identifier: Apache-2.0
public final class ClipboardFixture {
 public static void main(String[] args)throws Exception {
  Class.forName("android.os.Looper").getMethod("prepareMainLooper").invoke(null);
  Class.forName("com.genymobile.scrcpy.Workarounds").getMethod("apply").invoke(null);
  Object wrapper=Class.forName("com.genymobile.scrcpy.wrappers.ServiceManager").getMethod("getClipboardManager").invoke(null);
  java.lang.reflect.Field field=wrapper.getClass().getDeclaredField("manager");field.setAccessible(true);Object manager=field.get(wrapper);
  Class<?> api=Class.forName("android.content.ClipboardManager"),clip=Class.forName("android.content.ClipData");
  Object original=api.getMethod("getPrimaryClip").invoke(manager);
  System.out.println("READY");System.out.flush();
  String last=null;
  try{
   int command;while((command=System.in.read())!=-1){
    if(command=='3'){
     CharSequence current=(CharSequence)wrapper.getClass().getMethod("getText").invoke(wrapper);
     System.out.println(last!=null&&last.contentEquals(current==null?"":current)?"UNCHANGED":"CHANGED");System.out.flush();continue;
    }
    if(command!='1'&&command!='2')continue;
    String text=command=='1'?"PhoneDesk copy test":"\u65e5\u672c\u8a9e\u306e\u30b3\u30d4\u30fc\nPhoneDesk clipboard";
    wrapper.getClass().getMethod("setText",CharSequence.class).invoke(wrapper,text);
    last=text;System.out.println("COPIED");System.out.flush();
   }
  }finally{
   if(original!=null)api.getMethod("setPrimaryClip",clip).invoke(manager,original);else api.getMethod("clearPrimaryClip").invoke(manager);
  }
  Runtime.getRuntime().halt(0);
 }
}
