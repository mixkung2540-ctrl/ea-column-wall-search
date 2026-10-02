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
 HashSet<string> selectedMembers=new HashSet<string>(),selectedStories=new HashSet<string>();bool filtering;
 Control FilterPanel(TextBox box){var p=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};p.RowStyles.Add(new RowStyle(SizeType.Percent,100));p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,86));p.Controls.Add(new Label{Text="Find:",AutoSize=true,Anchor=AnchorStyles.Left},0,0);box.Dock=DockStyle.Fill;p.Controls.Add(box,1,0);var clear=new RoundedButton{Text="Clear All",Dock=DockStyle.Fill,Margin=new Padding(3,0,0,0),AccessibleDescription="Clear all selections in this list, including hidden items"};clear.Click+=(s,e)=>ClearSelections(box==memberFilter?members:stories);p.Controls.Add(clear,2,0);return p;}
 void ClearSelections(CheckedListBox box){CommitPT();if(box==null||box==members)selectedMembers.Clear();if(box==null||box==stories)selectedStories.Clear();InvalidateResults();ApplyFilters();status.Text="Cleared all "+(box==members?"member":box==stories?"Story":"member and Story")+" selections, including hidden items. Imports and PT values kept.";}
 void SelectionChanged(CheckedListBox box,ItemCheckEventArgs e){if(filtering)return;string key=box==members?((Member)box.Items[e.Index]).Key:(string)box.Items[e.Index];var set=box==members?selectedMembers:selectedStories;if(e.NewValue==CheckState.Checked)set.Add(key);else set.Remove(key);InvalidateResults();status.Text=selectedMembers.Count+" members / "+selectedStories.Count+" Stories selected (including hidden). All/None = visible only.";}
 void ApplyFilters(){
  if(filtering)return;filtering=true;members.BeginUpdate();stories.BeginUpdate();
  try{members.Items.Clear();stories.Items.Clear();string mf=memberFilter.Text.Trim(),sf=storyFilter.Text.Trim();
   foreach(var m in allMembers)if((m.ToString()+" "+engine.OutputName(m.SourceId,m.Name)).IndexOf(mf,StringComparison.OrdinalIgnoreCase)>=0)members.Items.Add(m,selectedMembers.Contains(m.Key));
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
  var table=NameTable();using(var dialog=new NameEditor(table,kind.Text,ApplyNames)){var grid=dialog.TableGrid;
   if(grid.Columns.Count!=4||grid.Columns[2].ReadOnly||!grid.Columns[0].ReadOnly||grid.Columns[3].Visible)throw new Exception("Name grid column setup failure");
   dialog.Show(this);Application.DoEvents();
   if(grid.Rows.Count!=table.Rows.Count)throw new Exception("Name dialog binding failure");
   grid.CurrentCell=grid.Rows[0].Cells[2];grid.BeginEdit(true);((DataGridViewTextBoxEditingControl)grid.EditingControl).Text="TEST-ALIAS";grid.EndEdit();dialog.ApplyFilter("TEST-ALIAS");if(grid.Rows.Count!=1)throw new Exception("Output alias filter failure");dialog.ApplyFilter("no matching name");if(grid.Rows.Count!=0)throw new Exception("Output no-match filter failure");dialog.ApplyFilter("");dialog.AcceptEdits();
  }if(engine.OutputName("S001","W-1")!="TEST-ALIAS")throw new Exception("Name editor failed");
 }
}
}
