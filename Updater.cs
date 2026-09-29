using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace EASearch {
public sealed class ReleaseInfo {
 public string Tag,Name,Url;
 public Version Version;
}
public static class Updater {
 public const string Repository="mixkung2540-ctrl/ea-column-wall-search";
 public const string LatestApi="https://api.github.com/repos/"+Repository+"/releases/latest";
 public const string ReleasesUrl="https://github.com/"+Repository+"/releases";
 public static readonly Version Current=new Version(1,0,3);
 public static ReleaseInfo Parse(string json){
  var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
  string tag=Value(data,"tag_name"),url=Value(data,"html_url"),name=Value(data,"name");
  string clean=tag.Trim().TrimStart('v','V');int dash=clean.IndexOf('-');if(dash>=0)clean=clean.Substring(0,dash);
  Version version;if(!Version.TryParse(clean,out version))throw new Exception("GitHub release has an invalid version tag: "+tag);
  if(string.IsNullOrWhiteSpace(url))url=ReleasesUrl;
  return new ReleaseInfo{Tag=tag,Name=name,Url=url,Version=version};
 }
 static string Value(Dictionary<string,object> data,string key){object value;return data.TryGetValue(key,out value)&&value!=null?Convert.ToString(value):"";}
 public static ReleaseInfo Latest(){
  ServicePointManager.SecurityProtocol|=(SecurityProtocolType)3072;
  try{using(var client=new WebClient()){
   client.Headers[HttpRequestHeader.UserAgent]="EA-Column-Wall-Search/1.0.3";
   client.Headers[HttpRequestHeader.Accept]="application/vnd.github+json";
   return Parse(client.DownloadString(LatestApi));
  }}catch(WebException ex){var response=ex.Response as HttpWebResponse;if(response!=null&&response.StatusCode==HttpStatusCode.NotFound)throw new Exception("No public GitHub release was found. The repository and a published release must be public for update checks without a sign-in.",ex);throw new Exception("Could not contact GitHub. Check the internet connection and try again.",ex);}
 }
 public static void OpenRelease(string url){Process.Start(new ProcessStartInfo{FileName=url,UseShellExecute=true});}
}
public sealed partial class MainForm {
 void CheckUpdate(){
  ReleaseInfo latest=null;
  Run("Checking GitHub for updates...",()=>latest=Updater.Latest(),()=>{
   if(latest.Version<=Updater.Current){MessageBox.Show("Version 1.0.3 is up to date.","EA Search Update",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
   string title=string.IsNullOrWhiteSpace(latest.Name)?latest.Tag:latest.Name;
   if(MessageBox.Show("A newer version is available: "+title+"\n\nOpen the secure GitHub Releases page to download it?","EA Search Update",MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)Updater.OpenRelease(latest.Url);
  });
 }
}
}
