// SPDX-License-Identifier: Apache-2.0
// Local verification only. Not part of the published PhoneDesk build.
using System;
using System.Drawing;
using System.IO;
using System.Net;
using PhoneDesk;

class PcViewTests {
 static int checks;
 static void Check(bool condition,string name){
  checks++;
  if(!condition)throw new Exception("FAIL: "+name);
 }
 static void Main(){
  string token=PcViewProtocol.NewToken();
  Check(token.Length==16,"token length");
  Check(PcViewProtocol.TokenMatches(token,token),"token match");
  Check(!PcViewProtocol.TokenMatches(token,token.Substring(0,15)+"0"+(token[15]=='0'?"1":"0")),"token mismatch");
  Check(!PcViewProtocol.TokenMatches(token,null),"token null");
  Check(PcViewProtocol.QueryValue("?a=1&t=abc","t")=="abc","query value");
  Check(PcViewProtocol.QueryValue("?t=a%20b","t")=="a b","query unescape");
  Check(PcViewProtocol.QueryValue(null,"t")==null,"query empty");
  Check(PcViewProtocol.OriginAllowed(null),"origin absent");
  Check(PcViewProtocol.OriginAllowed("http://localhost:8796"),"origin localhost");
  Check(!PcViewProtocol.OriginAllowed("https://example.test"),"origin foreign");

  PcViewAction action;
  Check(PcViewProtocol.TryParse("touch:0,3,0.5,0.25",out action)&&action.PointerAction==0&&action.PointerId==3&&Math.Abs(action.U-0.5)<0.0001&&Math.Abs(action.V-0.25)<0.0001,"touch down");
  Check(PcViewProtocol.TryParse("touch:1,3,1,1",out action)&&action.PointerAction==1,"touch up");
  Check(!PcViewProtocol.TryParse("touch:3,3,0.5,0.5",out action),"touch bad action");
  Check(!PcViewProtocol.TryParse("touch:0,3,1.1,0.5",out action),"touch out of range");
  Check(PcViewProtocol.TryParse("key:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("日本語")),out action)&&action.Text=="日本語","key utf8");
  Check(!PcViewProtocol.TryParse("key:!!!",out action),"key bad base64");
  Check(PcViewProtocol.TryParse("special:8",out action)&&action.Special==8,"special backspace");
  Check(!PcViewProtocol.TryParse("special:65",out action),"special unlisted");
  Check(PcViewProtocol.TryParse("wheel:-3",out action)&&action.Wheel==-3,"wheel");
  Check(PcViewProtocol.TryParse("move:12,-7",out action)&&action.MoveX==12&&action.MoveY==-7,"move delta");
  Check(!PcViewProtocol.TryParse("move:999,0",out action),"move range");
  Check(!PcViewProtocol.TryParse("wheel:99",out action),"wheel range");
  Check(PcViewProtocol.TryParse("click:1",out action)&&action.Click==1,"left click");
  Check(PcViewProtocol.TryParse("click:2",out action)&&action.Click==2,"right click");
  Check(!PcViewProtocol.TryParse("unknown:1",out action),"unknown kind");

  Point point=PcViewProtocol.Map(0.5,1.0,1920,1080);
  Check(point.X==960&&point.Y==1079,"map bounds");
  point=PcViewProtocol.Map(0,0,1920,1080);
  Check(point.X==0&&point.Y==0,"map origin");
  point=PcViewProtocol.Map(2,-1,1920,1080);
  Check(point.X==1919&&point.Y==0,"map clamp");

  using(var server=new PcViewServer("adb.exe","dummy")){
   string url=server.Start(false);
   Check(server.Running,"server running");
   string urlToken=PcViewProtocol.QueryValue(new Uri(url).Query,"t");
   var request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+PcViewProtocol.Port+"/?t="+urlToken);
   request.Timeout=5000;
   using(var response=(HttpWebResponse)request.GetResponse())
   using(var reader=new StreamReader(response.GetResponseStream())){
    string html=reader.ReadToEnd();
    Check(html.Contains("id='screen'"),"html screen element");
    Check(html.Contains("'/input?t='"),"html input endpoint");
   }
   var denied=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+PcViewProtocol.Port+"/?t=wrong");
   denied.Timeout=5000;
   try{
    using(var response=(HttpWebResponse)denied.GetResponse()){}
    Check(false,"token denial");
   }catch(WebException ex){
    Check(ex.Response!=null&&(int)((HttpWebResponse)ex.Response).StatusCode==403,"token denial");
   }
  }
  Console.WriteLine("PASS: PcView "+checks+" checks");
 }
}
