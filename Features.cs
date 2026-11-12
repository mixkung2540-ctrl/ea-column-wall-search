using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace EASearch {
public sealed partial class MainForm {
 TextBox memberFilter=new TextBox(),storyFilter=new TextBox();
 List<Member> allMembers=new List<Member>();List<string> allStories=new List<string>();
 HashSet<string> selectedMembers=new HashSet<string>(),selectedStories=new HashSet<string>();int lastMemberIndex=-1,lastStoryIndex=-1;bool filtering,bulkChecking,shiftTest;
 CheckBox memberRenamedOnly=new CheckBox{Text="Renamed",AutoSize=true,Margin=new Padding(2,7,0,0)};
 Button sortOrigBtn,sortOutBtn;string memberSortField="";bool memberSortAsc=true;
 void InitMemberTools(FlowLayoutPanel mb){
  mb.Padding=Padding.Empty;mb.Margin=Padding.Empty;
  foreach(Control c in mb.Controls){
   if(c is Label)c.Margin=new Padding(0,7,2,0);
   var b=c as Button;if(b!=null){b.AutoSize=false;b.Width=b.Text=="All"?44:54;b.Height=30;b.Margin=new Padding(2,2,2,2);}
  }
  memberRenamedOnly.CheckedChanged+=(s,e)=>ApplyFilters();
  mb.Controls.Add(memberRenamedOnly);mb.SetFlowBreak(memberRenamedOnly,true);
  sortOrigBtn=Button(mb,"Sort Original ↕",()=>ToggleMemberSort("Original"));
  sortOutBtn=Button(mb,"Sort Output ↕",()=>ToggleMemberSort("Output"));
  foreach(var b in new[]{sortOrigBtn,sortOutBtn}){b.AutoSize=false;b.Width=136;b.Height=30;b.Margin=new Padding(2,3,4,0);}
 }
 void ToggleMemberSort(string field){
  if(memberSortField==field)memberSortAsc=!memberSortAsc;else{memberSortField=field;memberSortAsc=true;}
  if(sortOrigBtn!=null)sortOrigBtn.Text="Sort Original "+(memberSortField=="Original"?(memberSortAsc?"▲":"▼"):"↕");
  if(sortOutBtn!=null)sortOutBtn.Text="Sort Output "+(memberSortField=="Output"?(memberSortAsc?"▲":"▼"):"↕");
  ApplyFilters();
 }
 void SortAllMembers(){
  foreach(var m in allMembers)m.OutputName=engine.OutputName(m.SourceId,m.Name);
  if(memberSortField=="")return;
  Func<Member,string> primary=m=>Engine.NaturalKey(memberSortField=="Output"?m.OutputName:m.Name);
  Func<Member,string> secondary=m=>Engine.NaturalKey(memberSortField=="Output"?m.Name:m.OutputName);
  allMembers=(memberSortAsc
   ?allMembers.OrderBy(primary,StringComparer.OrdinalIgnoreCase).ThenBy(secondary,StringComparer.OrdinalIgnoreCase).ThenBy(m=>m.SourceId,StringComparer.OrdinalIgnoreCase)
   :allMembers.OrderByDescending(primary,StringComparer.OrdinalIgnoreCase).ThenByDescending(secondary,StringComparer.OrdinalIgnoreCase).ThenBy(m=>m.SourceId,StringComparer.OrdinalIgnoreCase)).ToList();
 }
 Control FilterPanel(TextBox box){var p=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};p.RowStyles.Add(new RowStyle(SizeType.Percent,100));p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,86));p.Controls.Add(new Label{Text="Find:",AutoSize=true,Anchor=AnchorStyles.Left},0,0);box.Dock=DockStyle.Fill;p.Controls.Add(box,1,0);var clear=new RoundedButton{Text="Clear All",Dock=DockStyle.Fill,Margin=new Padding(3,0,0,0),AccessibleDescription="Clear all selections in this list, including hidden items"};clear.Click+=(s,e)=>ClearSelections(box==memberFilter?members:stories);p.Controls.Add(clear,2,0);return p;}
 void ClearSelections(CheckedListBox box){CommitPT();if(box==null||box==members){selectedMembers.Clear();lastMemberIndex=-1;}if(box==null||box==stories){selectedStories.Clear();lastStoryIndex=-1;}InvalidateResults();ApplyFilters();status.Text="Cleared all "+(box==members?"member":box==stories?"Story":"member and Story")+" selections, including hidden items. Imports and PT values kept.";}
 void SelectionChanged(CheckedListBox box,ItemCheckEventArgs e){
  if(filtering)return;
  int last=box==members?lastMemberIndex:lastStoryIndex;
  bool shift=!bulkChecking&&((Control.ModifierKeys&Keys.Shift)==Keys.Shift||shiftTest)&&last>=0&&last<box.Items.Count&&last!=e.Index;
  if(shift){bool target=box.GetItemChecked(last);e.NewValue=target?CheckState.Checked:CheckState.Unchecked;}
  string key=box==members?((Member)box.Items[e.Index]).Key:(string)box.Items[e.Index];var set=box==members?selectedMembers:selectedStories;
  if(e.NewValue==CheckState.Checked)set.Add(key);else set.Remove(key);
  if(shift){
   bool target=e.NewValue==CheckState.Checked;int start=Math.Min(last,e.Index),end=Math.Max(last,e.Index);
   bulkChecking=true;box.BeginUpdate();try{for(int i=start;i<=end;i++)if(i!=e.Index)box.SetItemChecked(i,target);}finally{box.EndUpdate();bulkChecking=false;}
  }
  if(!bulkChecking){if(box==members)lastMemberIndex=e.Index;else lastStoryIndex=e.Index;}
  InvalidateResults();status.Text=selectedMembers.Count+" members / "+selectedStories.Count+" Stories selected (including hidden). All/None = visible only.";
 }
 void ApplyFilters(){
  if(filtering)return;filtering=true;lastMemberIndex=lastStoryIndex=-1;members.BeginUpdate();stories.BeginUpdate();
  try{members.Items.Clear();stories.Items.Clear();string mf=memberFilter.Text.Trim(),sf=storyFilter.Text.Trim();
   SortAllMembers();
   foreach(var m in allMembers){if(memberRenamedOnly.Checked&&m.OutputName==m.Name)continue;if(m.ToString().IndexOf(mf,StringComparison.OrdinalIgnoreCase)>=0)members.Items.Add(m,selectedMembers.Contains(m.Key));}
   foreach(var s in allStories)if(s.IndexOf(sf,StringComparison.OrdinalIgnoreCase)>=0)stories.Items.Add(s,selectedStories.Contains(s));
  }finally{members.EndUpdate();stories.EndUpdate();filtering=false;}
  status.Text=selectedMembers.Count+" members / "+selectedStories.Count+" Stories selected (including hidden). All/None = visible only.";
 }
 DataTable NameTable(){var t=new DataTable();foreach(string c in new[]{"Source","Original Member","Output Name","Key"})t.Columns.Add(c);foreach(var m in allMembers)t.Rows.Add(m.SourceId,m.Name,engine.OutputName(m.SourceId,m.Name),m.Key);return t;}
 void ApplyNames(DataTable table){
  var changes=new Dictionary<string,string>();foreach(DataRow row in table.Rows){string name=Engine.S(row[2]);if(name.Length==0||name.Any(char.IsControl))throw new Exception("Output Name must not be blank or contain tabs/newlines.");changes.Add(Engine.S(row[3]),name);}
  foreach(var pair in changes)engine.Names[pair.Key]=pair.Value;dirty=true;InvalidateResults();ApplyFilters();status.Text="Output names saved. Run Search again. Save project to keep these names.";
 }
 void EditNames(){CommitPT();if(allMembers.Count==0)throw new Exception("Import data first.");using(var dialog=new NameEditor(NameTable(),kind.Text,ApplyNames))dialog.ShowDialog(this);}
 DataGridView NameGrid(DataTable table){
  // Columns must exist before binding: an unattached grid has no BindingContext yet.
  var grid=Grid(false);grid.AutoGenerateColumns=false;
  foreach(DataColumn c in table.Columns)grid.Columns.Add(new DataGridViewTextBoxColumn{Name=c.ColumnName,HeaderText=c.ColumnName,DataPropertyName=c.ColumnName,ReadOnly=c.ColumnName!="Output Name",Visible=c.ColumnName!="Key",Width=c.ColumnName=="Source"?90:c.ColumnName=="Output Name"?300:250});
  grid.DataSource=table;return grid;
 }
 internal void VerifyFiltersAndNames(){
  int originalStories=selectedStories.Count;storyFilter.Text="SUB";if(stories.Items.Count!=1)throw new Exception("Story filter failed");CheckAll(stories,false);if(selectedStories.Count!=originalStories-1)throw new Exception("Filtered None lost hidden checks");storyFilter.Text="";if(stories.CheckedItems.Count!=originalStories-1)throw new Exception("Hidden checks not restored");CheckAll(stories,true);
  int originalMembers=selectedMembers.Count;memberFilter.Text="NO-MATCH";if(members.Items.Count!=0||selectedMembers.Count!=originalMembers)throw new Exception("Member filter lost selection");memberFilter.Text="w-1";if(members.CheckedItems.Count!=originalMembers)throw new Exception("Case insensitive member filter failed");memberFilter.Text="";
  memberFilter.Text="NO-MATCH";CheckAll(members,false);if(selectedMembers.Count!=originalMembers)throw new Exception("Member None cleared hidden selections");ClearSelections(members);if(selectedMembers.Count!=0||selectedStories.Count!=originalStories||memberFilter.Text!="NO-MATCH")throw new Exception("Clear all members failed");memberFilter.Text="";if(members.CheckedItems.Count!=0)throw new Exception("Hidden member checks reappeared");CheckAll(members,true);
  storyFilter.Text="SUB";ClearSelections(stories);if(selectedStories.Count!=0||selectedMembers.Count!=originalMembers)throw new Exception("Clear all Stories failed");storyFilter.Text="";if(stories.CheckedItems.Count!=0)throw new Exception("Hidden Story checks reappeared");CheckAll(stories,true);
  memberFilter.Text="NO-MATCH";storyFilter.Text="NO-MATCH";ClearSelections(null);ClearSelections(null);if(selectedMembers.Count!=0||selectedStories.Count!=0||shown.Count!=0||normal.Count!=0)throw new Exception("Global clear failed");memberFilter.Text="";storyFilter.Text="";CheckAll(members,true);CheckAll(stories,true);
  if(members.Items[0].ToString()!="W-1 [S001] (W-1)")throw new Exception("Default member output display failed");
  memberRenamedOnly.Checked=true;if(members.Items.Count!=0)throw new Exception("Renamed filter should hide unchanged members");memberRenamedOnly.Checked=false;
  CheckAll(stories,false);stories.SetItemChecked(0,true);shiftTest=true;stories.SetItemChecked(2,true);shiftTest=false;if(stories.CheckedItems.Count!=3||selectedStories.Count!=3)throw new Exception("Shift range check failed");
  stories.SetItemChecked(2,false);shiftTest=true;stories.SetItemChecked(0,false);shiftTest=false;if(stories.CheckedItems.Count!=0||selectedStories.Count!=0)throw new Exception("Shift range uncheck failed");CheckAll(stories,true);
  var table=NameTable();using(var dialog=new NameEditor(table,kind.Text,ApplyNames)){var grid=dialog.TableGrid;
   if(grid.Columns.Count!=4||grid.Columns[2].ReadOnly||!grid.Columns[0].ReadOnly||grid.Columns[3].Visible)throw new Exception("Name grid column setup failure");
   dialog.Show(this);Application.DoEvents();
   if(grid.Rows.Count!=table.Rows.Count)throw new Exception("Name dialog binding failure");
   grid.CurrentCell=grid.Rows[0].Cells[2];grid.BeginEdit(true);((DataGridViewTextBoxEditingControl)grid.EditingControl).Text="TEST-ALIAS";grid.EndEdit();dialog.ApplyFilter("TEST-ALIAS");if(grid.Rows.Count!=1)throw new Exception("Output alias filter failure");dialog.ApplyFilter("no matching name");if(grid.Rows.Count!=0)throw new Exception("Output no-match filter failure");dialog.ApplyFilter("");dialog.AcceptEdits();
  }if(engine.OutputName("S001","W-1")!="TEST-ALIAS"||members.Items[0].ToString()!="W-1 [S001] (TEST-ALIAS)")throw new Exception("Name editor failed");
  memberRenamedOnly.Checked=true;if(members.Items.Count!=1)throw new Exception("Renamed filter failed to show renamed member");memberRenamedOnly.Checked=false;
  ToggleMemberSort("Original");ToggleMemberSort("Original");ToggleMemberSort("Output");ToggleMemberSort("Output");
 }
 internal void VerifyColumnMemberTools(){
  var sample=engine;var originalKind=kind.Text;
  try{
   engine=new Engine();kind.SelectedItem="COLUMN";
   allMembers=new List<Member>{new Member{SourceId="S001",Name="C10"},new Member{SourceId="S001",Name="C2"},new Member{SourceId="S001",Name="C3"}};
   engine.Names[allMembers[0].Key]="B2C9";engine.Names[allMembers[1].Key]="B2C10";
   memberSortField="";ToggleMemberSort("Original");
   if(((Member)members.Items[0]).Name!="C2"||((Member)members.Items[2]).Name!="C10")throw new Exception("Natural original sort failed");
   members.SetItemChecked(0,true);shiftTest=true;try{members.SetItemChecked(2,true);}finally{shiftTest=false;}
   if(members.CheckedItems.Count!=3||selectedMembers.Count!=3)throw new Exception("Column Shift range failed");
   ToggleMemberSort("Output");
   if(((Member)members.Items[0]).Name!="C10"||members.CheckedItems.Count!=3)throw new Exception("Output sort lost selections or used lexical order");
   memberRenamedOnly.Checked=true;CheckAll(members,false);
   if(members.Items.Count!=2||selectedMembers.Count!=1)throw new Exception("Renamed filter or visible-only None failed");
   memberRenamedOnly.Checked=false;
   if(members.CheckedItems.Count!=1||((Member)members.CheckedItems[0]).Name!="C3")throw new Exception("Hidden column check was not retained");
  }finally{engine=sample;memberRenamedOnly.Checked=false;kind.SelectedItem=originalKind;RefreshLists();CheckAll(members,true);CheckAll(stories,true);}
 }
}
}
