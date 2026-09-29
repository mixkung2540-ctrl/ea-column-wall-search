using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace EASearch {
public sealed class NameEditor:Form {
 public readonly DataGridView TableGrid=new DataGridView();
 readonly DataTable table;readonly BindingSource binding=new BindingSource();readonly Action<DataTable> apply;
 readonly TextBox find=new TextBox();readonly Label count=new Label();
 public NameEditor(DataTable data,string kind,Action<DataTable> onApply){
  table=data;apply=onApply;Text="Output Names | "+kind;Font=new Font("Segoe UI",10);Width=960;Height=640;MinimumSize=new Size(750,480);StartPosition=FormStartPosition.CenterParent;
  table.Columns.Add("__Match",typeof(bool));foreach(DataRow row in table.Rows)row["__Match"]=true;
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(14)};root.RowStyles.Add(new RowStyle(SizeType.Absolute,62));root.RowStyles.Add(new RowStyle(SizeType.Absolute,46));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,52));Controls.Add(root);
  root.Controls.Add(new Label{Dock=DockStyle.Fill,Text="OUTPUT NAMES\nEdit the member name in Story-OutputName-Case. Original data and PT matching stay unchanged.",AutoSize=false},0,0);
  var search=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4};search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,60));search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90));search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90));root.Controls.Add(search,0,1);
  search.Controls.Add(new Label{Text="Find:",AutoSize=true,Anchor=AnchorStyles.Left},0,0);find.Dock=DockStyle.Fill;search.Controls.Add(find,1,0);
  var searchButton=AddButton(search,"Search",()=>ApplyFilter(find.Text));search.SetCellPosition(searchButton,new TableLayoutPanelCellPosition(2,0));var clear=AddButton(search,"Clear",()=>{find.Text="";ApplyFilter("");});search.SetCellPosition(clear,new TableLayoutPanelCellPosition(3,0));find.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){Safe(()=>ApplyFilter(find.Text));e.SuppressKeyPress=true;}};
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
 public void ApplyFilter(string query){Commit();string q=query.Trim();foreach(DataRow row in table.Rows)row["__Match"]=new[]{0,1,2}.Any(i=>Engine.S(row[i]).IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0);UpdateCount();}
 void UpdateCount(){count.Text=binding.Count+" / "+table.Rows.Count+" names | Apply includes hidden edits";}
 public void AcceptEdits(){Commit();apply(table);DialogResult=DialogResult.OK;Close();}
 protected override void Dispose(bool disposing){if(disposing)binding.Dispose();base.Dispose(disposing);}
}
}
