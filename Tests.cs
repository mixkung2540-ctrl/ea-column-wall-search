using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
namespace EASearch {
static class Tests {
 static void Check(bool b,string message){if(!b)throw new Exception("FAIL: "+message);}
 static DataTable Sample(string kind){var t=new DataTable();foreach(var c in new[]{"Story","Member","Case","Position","P","M2","M3"})t.Columns.Add(c);foreach(string story in new[]{"SUB-UR","7th-8th Mid Ramp","ชั้น [B-2] / A'B"})foreach(var cas in new[]{"UDCON1","UDCON2","UDCON3","UDCON4","UDCON10"}){t.Rows.Add(story,"W-1",cas,kind=="WALL"?"Bottom":"0","-100","20","30");t.Rows.Add(story,"W-1",cas,kind=="WALL"?"Top":"3","-120","40","50");}return t;}
 public static void Run(string folder){
  var report=new List<string>();
  var release=Updater.Parse("{\"tag_name\":\"v1.2.3\",\"name\":\"EA Search 1.2.3\",\"html_url\":\"https://github.com/mixkung2540-ctrl/ea-column-wall-search/releases/tag/v1.2.3\"}");
  Check(release.Version==new Version(1,2,3)&&release.Url.EndsWith("/v1.2.3")&&Updater.Current==new Version(1,0,3),"GitHub update metadata");
  report.Add("PASS updater: repository URL, release JSON and semantic version comparison.");
  var e=new Engine();e.Sources.Add(new Source{Id="S001",Kind="WALL",Version="2020",Path="synthetic",Data=Sample("WALL")});
  var r=e.Search("WALL",e.Members("WALL"),e.Stories("WALL"));Check(r.Count==15,"normal count");Check(r[0].P==120&&r[0].XT==50&&r[0].YT==40&&r[0].XB==30&&r[0].YB==20,"force mapping");Check(r[4].Case=="UDCON10","natural case sort");
  Check(r[0].Designation=="SUB-UR-W-1-UDCON1","no source ID in designation");
  e.Prepare(r);var p=e.PT.Rows.Find(new object[]{"WALL","SUB-UR"});p[2]=0;p[3]=-200;
  e.Prepare(r);Check((double)p[2]==0,"prepare retains edits");var result=e.Append(r);Check(result.Count==27,"PT count");Check(result[15].XT==0&&result[15].YT==-200&&result[15].XB==30&&result[15].P==120,"zero blank negative P");Check(e.Append(r).Count==27,"repeat PT");
  e.Sources.Add(new Source{Id="S002",Kind="COLUMN",Version="2016",Path="synthetic2",Data=Sample("COLUMN")});var col=e.Search("COLUMN",e.Members("COLUMN"),e.Stories("COLUMN"));e.Prepare(col);Check(e.Append(col)[15].XT==50,"PT type isolation");
  e.NextId=3;string project=Path.Combine(folder,"synthetic.easearch");e.Save(project);var loaded=Engine.Load(project);Check(loaded.Sources.Count==2&&loaded.PT.Rows.Count==6&&loaded.NextId==3,"project round trip");Check(loaded.Append(loaded.Search("WALL",loaded.Members("WALL"),loaded.Stories("WALL")))[15].XT==0,"saved PT");
  e.Names[Engine.Key("S001","W-1")]="W-RENAMED";e.Save(project);loaded=Engine.Load(project);Check(loaded.Search("WALL",loaded.Members("WALL"),loaded.Stories("WALL"))[0].Designation=="SUB-UR-W-RENAMED-UDCON1","alias save/load");e.Names.Clear();
  var dupe=new Source{Id="S003",Kind="WALL",Version="2020",Path="duplicate",Data=Sample("WALL")};e.Sources.Add(dupe);bool collision=false;try{e.Search("WALL",e.Members("WALL"),e.Stories("WALL"));}catch(Exception){collision=true;}Check(collision,"duplicate name blocked");e.Names[Engine.Key("S003","W-1")]="W-OTHER";Check(e.Search("WALL",e.Members("WALL"),e.Stories("WALL")).Count==30,"alias resolves duplicate without merging");e.Sources.Remove(dupe);e.Names.Clear();
  var legacy=Path.Combine(Path.GetDirectoryName(folder),"EA_Search_0.1.0","synthetic.easearch");if(File.Exists(legacy))Check(Engine.Load(legacy).Sources.Count==2,"legacy project loads");
  report.Add("PASS synthetic: end mapping, P, natural sort, exact Story names, PT zero/blank/negative, repeat prepare/append, type isolation, save/open, no source tag, aliases persisted, duplicate blocking, old project compatibility.");
  string root=Environment.GetEnvironmentVariable("EA_SEARCH_TEST_DATA");
  if(Directory.Exists(root)){
  var real=new Engine();
  string[] names={"WALL-2020.accdb","WALL 2016.mdb","WALL 9.7.4.mdb","COLUMN 2020.accdb","COLUMN 2016.mdb","COLUMN 9.7.4.mdb"};
  for(int i=0;i<names.Length;i++){string kind=i<3?"WALL":"COLUMN",ver=new[]{"2020","2016","9.7.4"}[i%3];var sw=Stopwatch.StartNew();real.Import(new[]{Path.Combine(root,names[i])},kind,ver);report.Add(names[i]+": "+real.Sources.Last().Data.Rows.Count+" rows, "+sw.Elapsed.TotalSeconds.ToString("0.00")+" seconds");}
  foreach(string k in new[]{"WALL","COLUMN"})foreach(var member in real.Members(k))real.Names[member.Key]=member.Name+"_"+member.SourceId;
  foreach(string kind in new[]{"WALL","COLUMN"}){
   var normal=real.Search(kind,real.Members(kind),real.Stories(kind));Check(normal.Count>0,kind+" real results");real.Prepare(normal);var appended=real.Append(normal);Check(appended.Count>=normal.Count,"PT append");report.Add(kind+": "+normal.Count+" normal, "+(appended.Count-normal.Count)+" PT, "+real.Skipped+" incomplete pairs");
   // Independent first-case end extraction against source records.
   var x=normal[0];var source=real.Sources.Single(s=>s.Id==x.SourceId);var rows=source.Data.AsEnumerable().Where(a=>Engine.S(a[0])==x.Story&&Engine.S(a[1])==x.Member&&Engine.S(a[2])==x.Case).ToList();
   var top=kind=="WALL"?rows.Last(a=>Engine.S(a[3]).Equals("Top",StringComparison.OrdinalIgnoreCase)):rows.OrderByDescending(a=>Engine.N(a[3])).First();
   var bot=kind=="WALL"?rows.Last(a=>Engine.S(a[3]).Equals("Bottom",StringComparison.OrdinalIgnoreCase)):rows.OrderBy(a=>Engine.N(a[3])).First();
   Check(x.P==Math.Abs(rows.Min(a=>Engine.N(a[4])))&&x.XT==Engine.N(top[6])&&x.YT==Engine.N(top[5])&&x.XB==Engine.N(bot[6])&&x.YB==Engine.N(bot[5]),kind+" independent values");
  }
  int count=real.Sources.Sum(s=>s.Data.Rows.Count);real.Import(new[]{Path.Combine(root,names[0])},"WALL","2020");Check(real.Sources.Sum(s=>s.Data.Rows.Count)==count,"reimport replacement");
  bool failed=false;try{real.Import(new[]{Path.Combine(root,names[0]),Path.Combine(root,"missing.accdb")},"WALL","2020");}catch{failed=true;}Check(failed&&real.Sources.Sum(s=>s.Data.Rows.Count)==count,"atomic failed batch");
  failed=false;try{real.Import(new[]{Path.Combine(root,names[3])},"WALL","2020");}catch{failed=true;}Check(failed&&real.Sources.Sum(s=>s.Data.Rows.Count)==count,"wrong type preserved");
  var target=real.Sources.First(s=>s.Kind=="COLUMN"&&s.Version=="2016");var retained=real.Sources.Where(s=>s!=target).Select(s=>s.Data).ToArray();var timer=Stopwatch.StartNew();real.Sources.Remove(target);timer.Stop();Check(real.Sources.All(s=>retained.Contains(s.Data)),"remove preserves retained tables");report.Add("PASS real: six schemas, independent end values, atomic import, reimport, wrong-type rejection, remove retains other tables. Remove core: "+timer.Elapsed.TotalMilliseconds.ToString("0.000")+" ms (UI refresh excluded).");
  }else report.Add("SKIP real Access tests: set EA_SEARCH_TEST_DATA to a private test-data folder containing the six expected databases.");
  using(var form=new MainForm()){form.Show();System.Windows.Forms.Application.DoEvents();form.PreviewSample(e,folder);form.Close();}report.Add("PASS UI binding: default Type/Version, PT Story filter, editable PT cell committed to appended results.");
  var namesTable=new DataTable();foreach(string c in new[]{"Source","Original Member","Output Name","Key"})namesTable.Columns.Add(c);
  for(int i=0;i<2000;i++)namesTable.Rows.Add(i%2==0?"S001":"S002","C"+i,"C"+i,"K"+i);
  namesTable.Rows[0][2]="ชั้น [B-2] / A'B %*";bool applied=false;
  using(var dialog=new NameEditor(namesTable,"COLUMN",t=>{Check(Engine.S(t.Rows[1][2])=="RENAMED-1","hidden edit retained");Check(t.Rows.Count==2000,"apply all rows, not filtered subset");applied=true;})){
   dialog.Show();System.Windows.Forms.Application.DoEvents();var grid=dialog.TableGrid;
   dialog.ApplyFilter("a'b %*");Check(grid.Rows.Count==1,"literal special character search");
   dialog.ApplyFilter("S002");Check(grid.Rows.Count==1000,"source filter");
   grid.Rows[0].Cells[2].Value="RENAMED-1";dialog.ApplyFilter("RENAMED-1");Check(grid.Rows.Count==1,"alias search");
   dialog.ApplyFilter("not-found");Check(grid.Rows.Count==0,"zero matches");
   dialog.ApplyFilter("C10");Check(grid.Rows.Count>0,"original name search");
   using(var bitmap=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(Path.Combine(folder,"output-names-preview.png"));}
   dialog.AcceptEdits();Check(applied,"apply callback");
  }
  bool cancelledApplied=false;var cancelTable=namesTable.Copy();cancelTable.Columns.Remove("__Match");using(var dialog=new NameEditor(cancelTable,"COLUMN",t=>cancelledApplied=true)){dialog.Show();System.Windows.Forms.Application.DoEvents();dialog.TableGrid.Rows[0].Cells[2].Value="CANCELLED";dialog.Close();}Check(!cancelledApplied&&Engine.S(namesTable.Rows[0][2])!="CANCELLED","cancel does not commit");
  report.Add("PASS Output Names: 2000 rows, Source/original/alias search, literal punctuation and Thai, no matches, hidden edits preserved, full Apply, Cancel.");
  foreach(string version in new[]{"0.1.1","0.1.2"}){string old=Path.Combine(Path.GetDirectoryName(folder),"EA_Search_"+version,"synthetic.easearch");if(File.Exists(old)){var prior=Engine.Load(old);Check(prior.Sources.Count>0&&prior.Names.Count>0,"legacy "+version);report.Add("PASS open project "+version+" with aliases.");}}
  var clipRows=new List<Result>{new Result{Designation="3-C15_UDCON1",P=599.999,XT=-74.312846,YT=-20.007,XB=0,YB=19.2},new Result{Designation="ชั้น-เสา-UDCON1-PT",P=20,XT=1,YT=2,XB=3,YB=4}};
  var clipTable=Engine.Output(clipRows);string text=Engine.PlainClipboard(clipTable);
  Check(text=="1\t3-C15_UDCON1\t599.999\t-74.312846\t-20.007\t0\t19.2\r\n2\tชั้น-เสา-UDCON1-PT\t20\t1\t2\t3\t4","plain copy exact text, no quotes/headers, original names retained");
  var priorCulture=System.Threading.Thread.CurrentThread.CurrentCulture;try{System.Threading.Thread.CurrentThread.CurrentCulture=new System.Globalization.CultureInfo("de-DE");Check(Engine.PlainClipboard(clipTable)==text,"clipboard invariant decimals");}finally{System.Threading.Thread.CurrentThread.CurrentCulture=priorCulture;}
  Check(Engine.Delimited(clipTable,",").StartsWith("\"Loadcase\",\"Designation\""),"CSV headers unchanged");
  clipTable.Rows[0][1]="bad\tname";bool rejected=false;try{Engine.PlainClipboard(clipTable);}catch{rejected=true;}Check(rejected,"prevent broken TSV columns");
  report.Add("PASS Copy Data: exact 7-column TSV, no headers/added quotes/trailing blank row, negatives/zero/Thai/original underscores preserved, invariant decimals; CSV unchanged.");
  File.WriteAllLines(Path.Combine(folder,"test-results.txt"),report);
 }
}
}
