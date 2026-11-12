using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace EASearch {
public sealed class NameEditor:Form {
 public readonly DataGridView TableGrid=new DataGridView();
 public readonly CheckBox RenamedOnly=new CheckBox{Text="Renamed only",AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(6,0,6,0)};
 readonly DataTable table;readonly BindingSource binding=new BindingSource();readonly Action<DataTable> apply;
 readonly TextBox find=new TextBox();readonly Label count=new Label();
 Button sortOrigBtn,sortOutBtn;string sortField="";bool sortAsc=true;
 public NameEditor(DataTable data,string kind,Action<DataTable> onApply){
  table=data;apply=onApply;Text="Output Names | "+kind;Font=new Font("Segoe UI",10);Width=980;Height=640;MinimumSize=new Size(820,480);StartPosition=FormStartPosition.CenterParent;
  if(!table.Columns.Contains("__Match"))table.Columns.Add("__Match",typeof(bool));
  if(!table.Columns.Contains("__Sort"))table.Columns.Add("__Sort",typeof(int));
  for(int i=0;i<table.Rows.Count;i++){table.Rows[i]["__Match"]=true;table.Rows[i]["__Sort"]=i;}
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(14)};root.RowStyles.Add(new RowStyle(SizeType.Absolute,62));root.RowStyles.Add(new RowStyle(SizeType.Absolute,46));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,52));Controls.Add(root);
  root.Controls.Add(new Label{Dock=DockStyle.Fill,Text="OUTPUT NAMES\nEdit the member name in Story-OutputName-Case. Original data and PT matching stay unchanged.",AutoSize=false},0,0);
  var search=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=7};search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,56));search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,82));search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));search.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,144));search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,144));root.Controls.Add(search,0,1);
  search.Controls.Add(new Label{Text="Find:",AutoSize=true,Anchor=AnchorStyles.Left},0,0);find.Dock=DockStyle.Fill;search.Controls.Add(find,1,0);
  var searchButton=AddButton(search,"Search",()=>ApplyFilter(find.Text));search.SetCellPosition(searchButton,new TableLayoutPanelCellPosition(2,0));var clear=AddButton(search,"Clear",()=>{find.Text="";ApplyFilter("");});search.SetCellPosition(clear,new TableLayoutPanelCellPosition(3,0));
  RenamedOnly.CheckedChanged+=(s,e)=>Safe(()=>ApplyFilter(find.Text));search.Controls.Add(RenamedOnly,4,0);
  sortOrigBtn=AddButton(search,"Sort Original ↕",()=>SortBy("Original"));sortOrigBtn.AutoSize=false;sortOrigBtn.Width=138;search.SetCellPosition(sortOrigBtn,new TableLayoutPanelCellPosition(5,0));
  sortOutBtn=AddButton(search,"Sort Output ↕",()=>SortBy("Output"));sortOutBtn.AutoSize=false;sortOutBtn.Width=138;search.SetCellPosition(sortOutBtn,new TableLayoutPanelCellPosition(6,0));
  find.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){Safe(()=>ApplyFilter(find.Text));e.SuppressKeyPress=true;}};
  TableGrid.Dock=DockStyle.Fill;TableGrid.AllowUserToAddRows=false;TableGrid.AllowUserToDeleteRows=false;TableGrid.RowHeadersVisible=false;TableGrid.AutoGenerateColumns=false;
  foreach(string c in new[]{"Source","Original Member","Output Name","Key"})TableGrid.Columns.Add(new DataGridViewTextBoxColumn{Name=c,HeaderText=c,DataPropertyName=c,ReadOnly=c!="Output Name",Visible=c!="Key",Width=c=="Source"?100:300});
  TableGrid.Columns["Output Name"].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
  binding.DataSource=new DataView(table){RowFilter="__Match = true"};TableGrid.DataSource=binding;root.Controls.Add(TableGrid,0,2);
  var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,8,0,0)};root.Controls.Add(footer,0,3);AddButton(footer,"Apply all edits",AcceptEdits);AddButton(footer,"Cancel",()=>Close());count.AutoSize=true;count.Margin=new Padding(15,9,0,0);footer.Controls.Add(count);
  Theme.Apply(this);Theme.Grid(TableGrid);TableGrid.Columns["Output Name"].DefaultCellStyle.BackColor=Color.FromArgb(227,241,247);UpdateCount();
 }
 Button AddButton(Control host,string caption,Action action){var b=new RoundedButton{Text=caption,AutoSize=true,Height=32};b.Click+=(s,e)=>Safe(action);host.Controls.Add(b);return b;}
 void Safe(Action action){try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Output Names",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
 void Commit(){if(!TableGrid.EndEdit())throw new Exception("Finish editing the Output Name first.");binding.EndEdit();}
 public void ApplyFilter(string query){Commit();string q=query.Trim();bool onlyRenamed=RenamedOnly.Checked;foreach(DataRow row in table.Rows){bool match=new[]{0,1,2}.Any(i=>Engine.S(row[i]).IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0);if(onlyRenamed&&Engine.S(row[2])==Engine.S(row[1]))match=false;row["__Match"]=match;}UpdateCount();}
 public void SortBy(string field){
  Commit();
  if(sortField==field)sortAsc=!sortAsc;else{sortField=field;sortAsc=true;}
  if(sortOrigBtn!=null)sortOrigBtn.Text="Sort Original "+(sortField=="Original"?(sortAsc?"▲":"▼"):"↕");
  if(sortOutBtn!=null)sortOutBtn.Text="Sort Output "+(sortField=="Output"?(sortAsc?"▲":"▼"):"↕");
  int col=sortField=="Output"?2:1,other=sortField=="Output"?1:2;
  var rows=table.Rows.Cast<DataRow>();
  var ordered=sortAsc
   ?rows.OrderBy(r=>Engine.NaturalKey(Engine.S(r[col])),StringComparer.OrdinalIgnoreCase).ThenBy(r=>Engine.NaturalKey(Engine.S(r[other])),StringComparer.OrdinalIgnoreCase).ThenBy(r=>Engine.S(r[0]),StringComparer.OrdinalIgnoreCase).ToList()
   :rows.OrderByDescending(r=>Engine.NaturalKey(Engine.S(r[col])),StringComparer.OrdinalIgnoreCase).ThenByDescending(r=>Engine.NaturalKey(Engine.S(r[other])),StringComparer.OrdinalIgnoreCase).ThenBy(r=>Engine.S(r[0]),StringComparer.OrdinalIgnoreCase).ToList();
  for(int i=0;i<ordered.Count;i++)ordered[i]["__Sort"]=i;
  ((DataView)binding.DataSource).Sort="__Sort ASC";
 }
 void UpdateCount(){count.Text=binding.Count+" / "+table.Rows.Count+" names | Apply includes hidden edits";}
 public void AcceptEdits(){Commit();apply(table);DialogResult=DialogResult.OK;Close();}
 protected override void Dispose(bool disposing){if(disposing)binding.Dispose();base.Dispose(disposing);}
}
}
