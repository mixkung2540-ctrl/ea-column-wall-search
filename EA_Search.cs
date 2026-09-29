using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
[assembly: System.Reflection.AssemblyVersion("1.0.2.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.2.0")]
[assembly: System.Reflection.AssemblyProduct("EA Column and Wall Search")]

namespace EASearch {
public sealed class Source {
 public string Id, Kind, Version, Path;
 public DataTable Data;
 public override string ToString(){return Id+" | "+Kind+" | "+Version+" | "+System.IO.Path.GetFileName(Path)+" | "+Data.Rows.Count.ToString("N0")+" rows";}
}
public sealed class Member {
 public string SourceId, Name;
 public string Key {get{return Engine.Key(SourceId,Name);}}
 public override string ToString(){return Name+" ["+SourceId+"]";}
}
public sealed class Result {
 public string Kind, Story, SourceId, Member, Case, Designation;
 public double P, XT,YT,XB,YB;
 public Result Copy(){return (Result)MemberwiseClone();}
}
public sealed class Pair {
 public Result R; public bool Bottom,Top; public double Min,Max;
}
public sealed class Engine {
 public Dictionary<string,string> Names=new Dictionary<string,string>();
 public string OutputName(string id,string member){string v;return Names.TryGetValue(Key(id,member),out v)?v:member;}
 public static void UniqueNames(List<Result> rows){var duplicate=rows.GroupBy(r=>r.Designation,StringComparer.OrdinalIgnoreCase).FirstOrDefault(g=>g.Count()>1);if(duplicate!=null)throw new Exception("Duplicate Designation: "+duplicate.Key+"\nUse Output Names to give members from different sources distinct names, or select only one source.");}
 public List<Source> Sources=new List<Source>();
 public DataTable PT=NewPT();
 public int NextId=1, Skipped;
 public static string Key(params string[] s){return string.Concat(s.Select(x=>x.Length+":"+x));}
 public static string S(object x){return Convert.ToString(x,CultureInfo.InvariantCulture).Trim();}
 public static double N(object x){double n;if(x==DBNull.Value||!double.TryParse(S(x),NumberStyles.Float,CultureInfo.InvariantCulture,out n)||double.IsInfinity(n)||double.IsNaN(n))throw new Exception("Invalid numeric input: "+S(x));return n;}
 public static DataTable NewPT(){var t=new DataTable("PT");t.Columns.Add("Type");t.Columns.Add("Story");foreach(var c in new[]{"Mx top","My top","Mx bot","My bot"})t.Columns.Add(c,typeof(double));t.PrimaryKey=new[]{t.Columns[0],t.Columns[1]};return t;}
 public static DataTable Read(string path,string kind,string version){
  string table=kind=="WALL"?"Pier Forces":version=="2020"?"Element Forces - Columns":"Column Forces";
  string member=kind=="WALL"?"Pier":"Column", load=version=="2020"?"Output Case":version=="2016"?"CaseCombo":"Load";
  string loc=version=="9.7.4"?"Loc":kind=="WALL"?"Location":"Station";
  var builder=new OleDbConnectionStringBuilder();builder.Provider="Microsoft.ACE.OLEDB.12.0";builder.DataSource=path;builder["Mode"]="Read";
  var t=new DataTable();
  try {using(var cn=new OleDbConnection(builder.ConnectionString))using(var cmd=new OleDbCommand("SELECT [Story],["+member+"] AS [Member],["+load+"] AS [Case],["+loc+"] AS [Position],[P],[M2],[M3] FROM ["+table+"]",cn)){
   cn.Open();using(var r=cmd.ExecuteReader())t.Load(r);
  }}catch(Exception e){throw new Exception(System.IO.Path.GetFileName(path)+": "+kind+" / "+version+"\n"+e.Message+"\nCheck Type/Version and the Access driver (same bitness as this app).",e);}
  if(t.Rows.Count==0)throw new Exception("Empty source: "+path);
  foreach(DataRow r in t.Rows){if(S(r[0])==""||S(r[1])==""||S(r[2])=="")throw new Exception("Missing Story/Member/Case: "+path);N(r[4]);N(r[5]);N(r[6]);if(kind=="COLUMN")N(r[3]);else if(!new[]{"TOP","BOTTOM"}.Contains(S(r[3]).ToUpperInvariant()))throw new Exception("Unknown Wall location: "+S(r[3]));}
  return t;
 }
 public void Import(string[] paths,string kind,string version){
  var pending=paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(p=>new Source{Path=System.IO.Path.GetFullPath(p),Kind=kind,Version=version,Data=Read(p,kind,version)}).ToList();
  foreach(var s in pending){var old=Sources.FirstOrDefault(x=>x.Kind==kind&&string.Equals(x.Path,s.Path,StringComparison.OrdinalIgnoreCase));s.Id=old==null?"S"+(NextId++).ToString("000"):old.Id;if(old!=null)Sources.Remove(old);Sources.Add(s);}
 }
 public List<Member> Members(string kind){return Sources.Where(s=>s.Kind==kind).SelectMany(s=>s.Data.AsEnumerable().Select(r=>S(r[1])).Distinct().Select(n=>new Member{SourceId=s.Id,Name=n})).ToList();}
 public List<string> Stories(string kind){return Sources.Where(s=>s.Kind==kind).SelectMany(s=>s.Data.AsEnumerable().Select(r=>S(r[0]))).Distinct().ToList();}
 public List<Result> Search(string kind,List<Member> members,List<string> stories){
  if(members.Count==0||stories.Count==0)throw new Exception("Select at least one member and one Story.");
  var selected=new HashSet<string>(members.Select(m=>m.Key));var storySet=new HashSet<string>(stories);var pairs=new Dictionary<string,Pair>();
  foreach(var s in Sources.Where(x=>x.Kind==kind))foreach(DataRow r in s.Data.Rows){
   string story=S(r[0]),mem=S(r[1]),cas=S(r[2]);if(!storySet.Contains(story)||!selected.Contains(Key(s.Id,mem)))continue;
   string key=Key(s.Id,story,mem,cas);Pair p;double force=N(r[4]),m2=N(r[5]),m3=N(r[6]);
   if(!pairs.TryGetValue(key,out p)){p=new Pair{R=new Result{Kind=kind,SourceId=s.Id,Story=story,Member=mem,Case=cas,P=force,Designation=story+"-"+OutputName(s.Id,mem)+"-"+cas}};pairs.Add(key,p);}
   p.R.P=Math.Min(p.R.P,force);
   bool bot,top;double station=0;
   if(kind=="COLUMN"){station=N(r[3]);bot=!p.Bottom||station<p.Min;top=!p.Top||station>p.Max;}else{bot=S(r[3]).Equals("Bottom",StringComparison.OrdinalIgnoreCase);top=S(r[3]).Equals("Top",StringComparison.OrdinalIgnoreCase);}
   if(bot){p.Min=station;p.R.XB=m3;p.R.YB=m2;p.Bottom=true;}if(top){p.Max=station;p.R.XT=m3;p.R.YT=m2;p.Top=true;}
  }
  Skipped=pairs.Values.Count(p=>!p.Bottom||!p.Top);
  var storyOrder=stories.Select((s,i)=>new{s,i}).ToDictionary(x=>x.s,x=>x.i);var memberOrder=members.Select((m,i)=>new{m.Key,i}).ToDictionary(x=>x.Key,x=>x.i);
  var result=pairs.Values.Where(p=>p.Bottom&&p.Top).Select(p=>p.R).OrderBy(r=>storyOrder[r.Story]).ThenBy(r=>memberOrder[Key(r.SourceId,r.Member)]).ThenBy(r=>Regex.Replace(r.Case,@"\d+",m=>m.Value.Length.ToString("000")+m.Value),StringComparer.OrdinalIgnoreCase).ToList();
  foreach(var r in result)r.P=Math.Abs(r.P);UniqueNames(result);return result;
 }
 public void Prepare(List<Result> normal){foreach(var r in normal)if(PT.Rows.Find(new object[]{r.Kind,r.Story})==null)PT.Rows.Add(r.Kind,r.Story);}
 public List<Result> Append(List<Result> normal){
  if(normal.Count==0)throw new Exception("Run Search first.");var result=new List<Result>(normal);
  foreach(var r in normal.Where(r=>Regex.IsMatch(r.Case,@"^UDCON[1-4]$",RegexOptions.IgnoreCase))){var p=PT.Rows.Find(new object[]{r.Kind,r.Story});if(p==null)throw new Exception("Prepare PT Input first.");var c=r.Copy();c.Designation+="-PT";c.Case+="-PT";
   if(p[2]!=DBNull.Value)c.XT=N(p[2]);if(p[3]!=DBNull.Value)c.YT=N(p[3]);if(p[4]!=DBNull.Value)c.XB=N(p[4]);if(p[5]!=DBNull.Value)c.YB=N(p[5]);result.Add(c);
  }UniqueNames(result);return result;
 }
 public void Save(string path){
  var ds=new DataSet("EA_Search_0_1");var meta=new DataTable("Sources");foreach(var c in new[]{"Id","Type","Version","Path","Table"})meta.Columns.Add(c);ds.Tables.Add(meta);ds.Tables.Add(PT.Copy());
  var aliases=new DataTable("OutputNames");aliases.Columns.Add("Key");aliases.Columns.Add("Name");foreach(var alias in Names)aliases.Rows.Add(alias.Key,alias.Value);ds.Tables.Add(aliases);
  foreach(var s in Sources){var t=s.Data.Copy();t.TableName="Raw_"+s.Id;ds.Tables.Add(t);meta.Rows.Add(s.Id,s.Kind,s.Version,s.Path,t.TableName);}
  string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{using(var fs=File.Create(temp))using(var z=new GZipStream(fs,CompressionMode.Compress))ds.WriteXml(z,XmlWriteMode.WriteSchema);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public static Engine Load(string path){
  var ds=new DataSet();using(var fs=File.OpenRead(path))using(var z=new GZipStream(fs,CompressionMode.Decompress))using(var reader=XmlReader.Create(z,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))ds.ReadXml(reader,XmlReadMode.ReadSchema);
  if(ds.DataSetName!="EA_Search_0_1"||!ds.Tables.Contains("Sources")||!ds.Tables.Contains("PT"))throw new Exception("Not an EA Search project.");
  var e=new Engine();e.PT=ds.Tables["PT"].Copy();e.PT.PrimaryKey=new[]{e.PT.Columns[0],e.PT.Columns[1]};
  if(ds.Tables.Contains("OutputNames"))foreach(DataRow a in ds.Tables["OutputNames"].Rows)e.Names.Add(S(a[0]),S(a[1]));
  foreach(DataRow r in ds.Tables["Sources"].Rows){string id=S(r[0]);e.Sources.Add(new Source{Id=id,Kind=S(r[1]),Version=S(r[2]),Path=S(r[3]),Data=ds.Tables[S(r[4])].Copy()});e.NextId=Math.Max(e.NextId,int.Parse(id.Substring(1))+1);}return e;
 }
 public static DataTable Output(List<Result> results){var t=new DataTable();t.Columns.Add("Loadcase",typeof(int));t.Columns.Add("Designation");foreach(var c in new[]{"P (kN)","Mx top (kNm)","My top (kNm)","Mx bot (kNm)","My bot (kNm)"})t.Columns.Add(c,typeof(double));foreach(var r in results)t.Rows.Add(t.Rows.Count+1,r.Designation,r.P,r.XT,r.YT,r.XB,r.YB);return t;}
 public static string PlainClipboard(DataTable t){
  var lines=new List<string>();foreach(DataRow row in t.Rows){var cells=row.ItemArray.Select(x=>Convert.ToString(x,CultureInfo.InvariantCulture)).ToArray();if(cells.Any(s=>s.IndexOfAny(new[]{'\t','\r','\n'})>=0))throw new Exception("A name contains a tab or line break. Correct it before copying.");lines.Add(string.Join("\t",cells));}return string.Join("\r\n",lines);
 }
 public static string Delimited(DataTable t,string separator){var b=new StringBuilder();Func<object,string> esc=x=>{string s=S(x);if(x is string&&s.Length>0&&"=+-@".Contains(s[0]))s="'"+s;return "\""+s.Replace("\"","\"\"")+"\"";};b.AppendLine(string.Join(separator,t.Columns.Cast<DataColumn>().Select(c=>esc(c.ColumnName))));foreach(DataRow r in t.Rows)b.AppendLine(string.Join(separator,r.ItemArray.Select(esc)));return b.ToString();}
}

public sealed partial class MainForm:Form {
 Engine engine=new Engine();List<Result> normal=new List<Result>(),shown=new List<Result>();bool dirty,busy;
 ComboBox kind=new ComboBox(),version=new ComboBox();CheckedListBox members=new CheckedListBox(),stories=new CheckedListBox();ListBox sources=new ListBox();
 DataGridView results=Grid(true),pt=Grid(false);TabControl tabs=new NavyTabs();Label status=new Label();FlowLayoutPanel bar=new FlowLayoutPanel();
 string PTKind="";
 public MainForm(){
  Text="EA Column and Wall Search 1.0.2";Width=1350;Height=860;MinimumSize=new Size(1100,700);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,92));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,32));Controls.Add(layout);
  bar.Dock=DockStyle.Fill;bar.Padding=new Padding(6);layout.Controls.Add(bar,0,0);
  kind.DropDownStyle=version.DropDownStyle=ComboBoxStyle.DropDownList;kind.Items.AddRange(new object[]{"WALL","COLUMN"});version.Items.AddRange(new object[]{"2020","2016","9.7.4"});kind.Width=100;version.Width=100;kind.SelectedIndex=version.SelectedIndex=0;
  bar.Controls.Add(kind);bar.Controls.Add(version);Button(bar,"Import Access...",Import);Button(bar,"Save project...",Save);Button(bar,"Open project...",Open);Button(bar,"Search",Search);Button(bar,"Prepare PT",Prepare);Button(bar,"Append PT",Append);Button(bar,"Output Names...",EditNames);Button(bar,"Export CSV...",Export);Button(bar,"Copy Data",Copy);Button(bar,"Help",ShowHelp);
  tabs.Dock=DockStyle.Fill;layout.Controls.Add(tabs,0,1);status.Dock=DockStyle.Fill;status.Text="Ready | Offline | Assumed units: kN, kNm | Mx=M3 / My=M2";layout.Controls.Add(status,0,2);
  var search=new TabPage("Search");tabs.TabPages.Add(search);var split=new SplitContainer{Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel1};search.Controls.Add(split);Shown+=(s,e)=>{split.SplitterDistance=300;};
  var left=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=6};left.RowStyles.Add(new RowStyle(SizeType.Absolute,40));left.RowStyles.Add(new RowStyle(SizeType.Absolute,34));left.RowStyles.Add(new RowStyle(SizeType.Percent,55));left.RowStyles.Add(new RowStyle(SizeType.Absolute,40));left.RowStyles.Add(new RowStyle(SizeType.Absolute,34));left.RowStyles.Add(new RowStyle(SizeType.Percent,45));split.Panel1.Controls.Add(left);
  var mb=new FlowLayoutPanel{Dock=DockStyle.Fill};mb.Controls.Add(new Label{Text="Members",AutoSize=true});Button(mb,"All",()=>CheckAll(members,true));Button(mb,"None",()=>CheckAll(members,false));left.Controls.Add(mb,0,0);
  left.Controls.Add(FilterPanel(memberFilter),0,1);members.Dock=DockStyle.Fill;members.CheckOnClick=true;left.Controls.Add(members,0,2);
  var sb=new FlowLayoutPanel{Dock=DockStyle.Fill};sb.Controls.Add(new Label{Text="Stories",AutoSize=true});Button(sb,"All",()=>CheckAll(stories,true));Button(sb,"None",()=>CheckAll(stories,false));left.Controls.Add(sb,0,3);left.Controls.Add(FilterPanel(storyFilter),0,4);stories.Dock=DockStyle.Fill;stories.CheckOnClick=true;left.Controls.Add(stories,0,5);split.Panel2.Controls.Add(results);
  var ptTab=new TabPage("PT Override");tabs.TabPages.Add(ptTab);ptTab.Controls.Add(pt);var hint=new Label{Dock=DockStyle.Top,Height=46,Text="UDCON1-4 only. Blank = original; 0 = zero. One Story applies to all selected members/sources of the same Type.\nOnly Stories in the last Search appear here. Click Append PT after editing."};ptTab.Controls.Add(hint);
  var importTab=new TabPage("Imports");tabs.TabPages.Add(importTab);sources.Dock=DockStyle.Fill;sources.HorizontalScrollbar=true;importTab.Controls.Add(sources);var actions=new FlowLayoutPanel{Dock=DockStyle.Top,Height=45};Button(actions,"Remove selected source",Remove);Button(actions,"Clear current Type",ClearType);importTab.Controls.Add(actions);
  kind.SelectedIndexChanged+=(s,e)=>{CommitPT();RefreshLists();};members.ItemCheck+=(s,e)=>SelectionChanged(members,e);stories.ItemCheck+=(s,e)=>SelectionChanged(stories,e);
  memberFilter.TextChanged+=(s,e)=>ApplyFilters();storyFilter.TextChanged+=(s,e)=>ApplyFilters();
  pt.CellValueChanged+=(s,e)=>{if(e.RowIndex>=0)dirty=true;};pt.DataError+=(s,e)=>{e.ThrowException=false;MessageBox.Show("Enter a number, or leave blank.");};
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MessageBox.Show("Please wait for the current operation.");}else if(dirty&&MessageBox.Show("Unsaved imports/PT. Close without saving?","EA Search",MessageBoxButtons.YesNo)!=DialogResult.Yes)e.Cancel=true;};
  InstallTheme(layout);
 }
 static DataGridView Grid(bool read){return new DataGridView{Dock=DockStyle.Fill,ReadOnly=read,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.None,BackgroundColor=Color.White,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.CellSelect};}
 void Button(Control panel,string text,Action action){var b=new RoundedButton{Text=text,AutoSize=true,Height=32};b.Click+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"EA Search",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};panel.Controls.Add(b);}
 void CheckAll(CheckedListBox box,bool value){box.BeginUpdate();for(int i=0;i<box.Items.Count;i++)box.SetItemChecked(i,value);box.EndUpdate();}
 void InvalidateResults(){normal.Clear();shown.Clear();results.DataSource=null;pt.DataSource=null;PTKind="";}
 void CommitPT(){if(pt.DataSource!=null){if(!pt.EndEdit())throw new Exception("Finish the PT number edit first.");BindingContext[pt.DataSource].EndCurrentEdit();}}
 async void Run(string message,Action work,Action done){if(busy)return;busy=true;bar.Enabled=tabs.Enabled=appMenu.Enabled=false;status.Text=message;var sw=System.Diagnostics.Stopwatch.StartNew();try{await Task.Run(work);done();status.Text+=" | "+sw.Elapsed.TotalSeconds.ToString("0.00")+" s";}catch(Exception ex){status.Text="Failed: "+ex.Message;MessageBox.Show(ex.Message,"EA Search",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{busy=false;bar.Enabled=tabs.Enabled=appMenu.Enabled=true;}}
 void RefreshLists(){InvalidateResults();selectedMembers.Clear();selectedStories.Clear();allMembers=engine.Members(kind.Text);allStories=engine.Stories(kind.Text);sources.Items.Clear();sources.Items.AddRange(engine.Sources.Cast<object>().ToArray());ApplyFilters();status.Text=engine.Sources.Sum(s=>s.Data.Rows.Count).ToString("N0")+" imported rows | Select members and Stories";}
 void Import(){CommitPT();using(var d=new OpenFileDialog{Filter="Access databases|*.mdb;*.accdb",Multiselect=true})if(d.ShowDialog()==DialogResult.OK){string k=kind.Text,v=version.Text;string[] paths=d.FileNames;Run("Importing (all files validated before commit)...",()=>engine.Import(paths,k,v),()=>{dirty=true;RefreshLists();});}}
 void Search(){CommitPT();string k=kind.Text;var m=allMembers.Where(x=>selectedMembers.Contains(x.Key)).ToList();var s=allStories.Where(x=>selectedStories.Contains(x)).ToList();InvalidateResults();List<Result> r=null;Run("Searching...",()=>r=engine.Search(k,m,s),()=>{normal=r;shown=new List<Result>(r);pt.DataSource=null;PTKind="";ShowResults();status.Text=r.Count+" normal rows | "+engine.Skipped+" incomplete pairs skipped";tabs.SelectedIndex=0;});}
 void ShowResults(){results.DataSource=Engine.Output(shown);foreach(DataGridViewColumn c in results.Columns){c.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;c.MinimumWidth=c.Index==1?220:c.Index==0?65:100;c.FillWeight=c.Index==1?32:c.Index==0?8:12;if(c.Index>=2){c.DefaultCellStyle.Format="N3";c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}}}
 void Prepare(){CommitPT();if(normal.Count==0)throw new Exception("Run Search first.");engine.Prepare(normal);var wanted=new HashSet<string>(normal.Select(r=>r.Story));PTKind=normal[0].Kind;string filter="Type = '"+PTKind+"' AND Story IN ("+string.Join(",",wanted.Select(s=>"'"+s.Replace("'","''")+"'"))+ ")";pt.DataSource=new DataView(engine.PT){RowFilter=filter};pt.Columns[0].Visible=false;pt.Columns[1].ReadOnly=true;foreach(DataGridViewColumn c in pt.Columns)c.Width=c.Index==1?300:150;dirty=true;tabs.SelectedIndex=1;}
 void Append(){CommitPT();if(PTKind=="")throw new Exception("Prepare PT first.");shown=engine.Append(normal);ShowResults();tabs.SelectedIndex=0;status.Text=normal.Count+" normal + "+(shown.Count-normal.Count)+" PT rows";}
 void Remove(){CommitPT();var s=sources.SelectedItem as Source;if(s==null)throw new Exception("Select a source in Imports.");if(MessageBox.Show("Remove "+s+"?\nOriginal Access file will not be deleted.","Remove",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;engine.Sources.Remove(s);dirty=true;RefreshLists();}
 void ClearType(){CommitPT();string k=kind.Text;if(MessageBox.Show("Clear all imported "+k+" data AND its PT values?","Clear Type",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;engine.Sources.RemoveAll(s=>s.Kind==k);foreach(var r in engine.PT.AsEnumerable().Where(r=>Engine.S(r[0])==k).ToArray())engine.PT.Rows.Remove(r);dirty=true;RefreshLists();}
 void Save(){CommitPT();using(var d=new SaveFileDialog{Filter="EA Search project|*.easearch",DefaultExt="easearch"})if(d.ShowDialog()==DialogResult.OK){string p=d.FileName;Run("Saving project...",()=>engine.Save(p),()=>{dirty=false;status.Text="Saved "+p;});}}
 void Open(){CommitPT();if(dirty&&MessageBox.Show("Discard unsaved imports/PT and open another project?","Open",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;using(var d=new OpenFileDialog{Filter="EA Search project|*.easearch"})if(d.ShowDialog()==DialogResult.OK){Engine loaded=null;string p=d.FileName;Run("Opening project...",()=>loaded=Engine.Load(p),()=>{engine=loaded;dirty=false;RefreshLists();});}}
 void Export(){if(shown.Count==0)throw new Exception("No results to export.");using(var d=new SaveFileDialog{Filter="CSV (UTF-8)|*.csv",DefaultExt="csv"})if(d.ShowDialog()==DialogResult.OK)File.WriteAllText(d.FileName,Engine.Delimited(Engine.Output(shown),","),new UTF8Encoding(true));}
 void Copy(){if(shown.Count==0)throw new Exception("No results to copy.");Clipboard.SetText(Engine.PlainClipboard(Engine.Output(shown)));status.Text="Copied "+shown.Count+" data rows (7 columns), no headers or added quotes. Paste at the first Load case cell.";}
 internal void PreviewSample(Engine sample,string folder){
  if(kind.Text!="WALL"||version.Text!="2020")throw new Exception("UI default selector failure");
  engine=sample;RefreshLists();CheckAll(members,true);CheckAll(stories,true);VerifyFiltersAndNames();normal=engine.Search(kind.Text,allMembers.Where(m=>selectedMembers.Contains(m.Key)).ToList(),allStories.Where(s=>selectedStories.Contains(s)).ToList());shown=new List<Result>(normal);ShowResults();Prepare();
  if(pt.Rows.Count!=3)throw new Exception("PT grid Story filter failure");
  pt.Rows[0].Cells[2].Value=123.0;CommitPT();Append();if(shown[normal.Count].XT!=123)throw new Exception("PT grid commit failure");
  foreach(int tab in new[]{0,1,2}){tabs.SelectedIndex=tab;Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(System.IO.Path.Combine(folder,"preview-"+tab+".png"));}}dirty=false;
 }
}
static class Program {
 [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Length>0&&args[0]=="--test"){try{Tests.Run(args[1]);}catch(Exception ex){File.WriteAllText(System.IO.Path.Combine(args[1],"test-results.txt"),ex.ToString());Environment.ExitCode=1;}return;}Application.Run(new MainForm());}
}
}
