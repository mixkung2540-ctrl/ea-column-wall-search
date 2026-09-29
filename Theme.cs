using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace EASearch {
static class Theme {
 public static readonly Color Navy=Color.FromArgb(19,32,49),Panel=Color.FromArgb(37,55,77),Ink=Color.FromArgb(225,231,239),Accent=Color.FromArgb(53,175,212);
 public static void Apply(Control root){
  root.BackColor=root is Form?Navy:Panel;root.ForeColor=Ink;
  var b=root as Button;if(b!=null){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(56,78,102);b.FlatAppearance.MouseOverBackColor=Color.FromArgb(45,85,113);b.FlatAppearance.MouseDownBackColor=Accent;b.BackColor=Navy;b.ForeColor=Ink;b.Cursor=Cursors.Hand;b.Padding=new Padding(5,2,5,2);}
  if(root is TextBox||root is ComboBox){root.BackColor=Color.FromArgb(23,39,58);root.ForeColor=Ink;}
  if(root is ListBox){root.BackColor=Color.FromArgb(232,238,244);root.ForeColor=Navy;}
  var grid=root as DataGridView;if(grid!=null){Grid(grid);return;}
  foreach(Control c in root.Controls)Apply(c);
 }
 public static void Grid(DataGridView grid){grid.BackgroundColor=Color.FromArgb(220,226,232);grid.BorderStyle=BorderStyle.None;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(75,78,80);grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.WhiteSmoke;grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=Color.FromArgb(75,78,80);grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9);grid.ColumnHeadersHeight=29;grid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;grid.DefaultCellStyle.BackColor=Color.FromArgb(234,239,244);grid.DefaultCellStyle.ForeColor=Color.FromArgb(24,28,33);grid.DefaultCellStyle.SelectionBackColor=Accent;grid.DefaultCellStyle.SelectionForeColor=Color.White;grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(214,221,228);grid.GridColor=Color.FromArgb(216,223,230);grid.CellBorderStyle=DataGridViewCellBorderStyle.None;grid.RowTemplate.Height=23;}
 public static Bitmap Icon(string name){
  var image=new Bitmap(26,26);using(var g=Graphics.FromImage(image))using(var p=new Pen(Ink,2.3f)){g.SmoothingMode=SmoothingMode.AntiAlias;p.StartCap=p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
   if(name.Contains("Search")){g.DrawEllipse(p,4,3,13,13);g.DrawLine(p,16,16,23,23);}
   else if(name.Contains("Import")){g.DrawLine(p,13,2,13,16);g.DrawLines(p,new[]{new Point(7,10),new Point(13,16),new Point(19,10)});g.DrawLines(p,new[]{new Point(4,18),new Point(4,23),new Point(22,23),new Point(22,18)});}
   else if(name.Contains("Save")){g.DrawRectangle(p,4,3,18,20);g.DrawRectangle(p,8,3,10,7);g.DrawRectangle(p,8,15,10,8);}
   else if(name.Contains("Open")){g.DrawPolygon(p,new[]{new Point(3,7),new Point(11,7),new Point(14,10),new Point(23,10),new Point(20,22),new Point(3,22)});}
   else if(name.Contains("Append")){g.DrawLine(p,13,3,13,23);g.DrawLine(p,3,13,23,13);}
   else if(name.Contains("Names")){g.DrawRectangle(p,3,5,15,18);g.DrawLine(p,9,17,23,3);g.DrawLine(p,8,18,13,17);}
   else if(name.Contains("Copy")){g.DrawRectangle(p,3,3,14,17);g.DrawRectangle(p,9,8,14,17);}
   else if(name.Contains("Update")){g.DrawArc(p,3,3,20,20,-65,285);g.DrawLines(p,new[]{new Point(19,2),new Point(23,3),new Point(22,8)});}
   else if(name.Contains("Help")){g.DrawEllipse(p,2,2,22,22);using(var f=new Font("Segoe UI",16,FontStyle.Bold))using(var brush=new SolidBrush(Ink))g.DrawString("?",f,brush,5,-1);}
   else{g.DrawRectangle(p,5,2,16,22);g.DrawLine(p,8,9,18,9);g.DrawLine(p,8,14,18,14);g.DrawLine(p,8,19,16,19);}
  }return image;
 }
}
public sealed partial class MainForm {
 MenuStrip appMenu=new MenuStrip();
 void MenuAction(ToolStripMenuItem parent,string text,Action action){var m=new ToolStripMenuItem(text);m.Click+=(s,e)=>{if(busy)return;try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"EA Search",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};parent.DropDownItems.Add(m);}
 void InstallTheme(TableLayoutPanel layout){
  Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
  RoundSidebar();
  Theme.Apply(this);layout.RowStyles[0].Height=101;bar.WrapContents=false;bar.AutoScroll=true;bar.Padding=new Padding(6,2,6,2);
  foreach(var b in bar.Controls.OfType<Button>()){b.AutoSize=false;b.Width=91;b.Height=61;b.Font=new Font("Segoe UI",9);b.Padding=new Padding(1);b.Margin=new Padding(3);b.Image=Theme.Icon(b.Text);b.TextImageRelation=TextImageRelation.ImageAboveText;b.ImageAlign=ContentAlignment.MiddleCenter;b.TextAlign=ContentAlignment.BottomCenter;b.Text=b.Text.Replace("...","");}
  kind.Width=90;version.Width=75;kind.Margin=version.Margin=new Padding(4,22,4,4);
  var buttons=bar.Controls.OfType<Button>().ToArray();bar.Controls.Clear();
  AddBlock("DATA && PROJECT",new Control[]{kind,version,buttons[0],buttons[1],buttons[2]},486);
  AddBlock("SEARCH && PT",new Control[]{buttons[3],buttons[4],buttons[5]},307);
  AddBlock("OUTPUT",new Control[]{buttons[6],buttons[7],buttons[8]},307);
  AddBlock("UPDATE & HELP",new Control[]{buttons[9],buttons[10]},205);
  foreach(var combo in new[]{kind,version}){combo.DrawMode=DrawMode.OwnerDrawFixed;combo.FlatStyle=FlatStyle.Flat;combo.DrawItem+=(s,e)=>{if(e.Index<0)return;var box=(ComboBox)s;using(var bg=new SolidBrush((e.State&DrawItemState.Selected)!=0?Theme.Accent:Theme.Navy))e.Graphics.FillRectangle(bg,e.Bounds);TextRenderer.DrawText(e.Graphics,box.Items[e.Index].ToString(),box.Font,e.Bounds,Theme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};}
  var menu=new ToolStripMenuItem("File");MenuAction(menu,"Import Access...",Import);MenuAction(menu,"Save project...",Save);MenuAction(menu,"Open project...",Open);MenuAction(menu,"Export CSV...",Export);MenuAction(menu,"Exit",Close);appMenu.Items.Add(menu);
  menu=new ToolStripMenuItem("Edit");MenuAction(menu,"Output Names...",EditNames);MenuAction(menu,"Copy Data",Copy);appMenu.Items.Add(menu);
  menu=new ToolStripMenuItem("Project");MenuAction(menu,"Search",Search);MenuAction(menu,"Prepare PT",Prepare);MenuAction(menu,"Append PT",Append);appMenu.Items.Add(menu);
  menu=new ToolStripMenuItem("View");MenuAction(menu,"Search",()=>tabs.SelectedIndex=0);MenuAction(menu,"PT Override",()=>tabs.SelectedIndex=1);MenuAction(menu,"Imports",()=>tabs.SelectedIndex=2);appMenu.Items.Add(menu);
  menu=new ToolStripMenuItem("Help");MenuAction(menu,"Check for updates...",CheckUpdate);MenuAction(menu,"About",()=>MessageBox.Show("EA Column and Wall Search 1.0.3\nCore functions work offline; Check Update uses GitHub only when clicked.\nUnits: kN / kNm. Mx=M3, My=M2.\nCheck results and units before engineering use.\nSee README_TH.txt for instructions.","About"));appMenu.Items.Add(menu);
  appMenu.BackColor=Theme.Navy;appMenu.ForeColor=Theme.Ink;appMenu.Renderer=new ToolStripProfessionalRenderer(new NavyMenuColors());appMenu.Dock=DockStyle.Top;Controls.Add(appMenu);MainMenuStrip=appMenu;foreach(ToolStripMenuItem top in appMenu.Items){top.ForeColor=Theme.Ink;foreach(ToolStripItem item in top.DropDownItems)item.ForeColor=Theme.Ink;}
  tabs.DrawMode=TabDrawMode.OwnerDrawFixed;tabs.SizeMode=TabSizeMode.Fixed;tabs.ItemSize=new Size(125,30);tabs.DrawItem+=(s,e)=>{using(var bg=new SolidBrush(e.Index==tabs.SelectedIndex?Theme.Accent:Theme.Panel)){e.Graphics.FillRectangle(bg,e.Bounds);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,Font,e.Bounds,Theme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}};
  status.BackColor=Theme.Navy;status.ForeColor=Theme.Ink;status.Padding=new Padding(8,4,0,0);
 }
 void AddBlock(string caption,Control[] controls,int width){var group=new RoundedBlock{Text=caption.Replace("&&","&"),Width=width,Height=84,ForeColor=Color.FromArgb(164,183,205),BackColor=Theme.Panel,Font=new Font("Segoe UI",7.5f),Margin=new Padding(3,2,6,2),Padding=new Padding(5,17,5,2)};var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=Padding.Empty,Margin=Padding.Empty,BackColor=Theme.Panel};group.Controls.Add(flow);foreach(var c in controls){if(!(c is Button))c.Font=new Font("Segoe UI",10);flow.Controls.Add(c);}bar.Controls.Add(group);}
 void RoundSidebar(){
  var old=(TableLayoutPanel)members.Parent;var host=old.Parent;var controls=Enumerable.Range(0,6).Select(i=>old.GetControlFromPosition(0,i)).ToArray();old.Controls.Clear();host.Controls.Remove(old);old.Dispose();
  var outer=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,Padding=new Padding(2)};outer.RowStyles.Add(new RowStyle(SizeType.Percent,55));outer.RowStyles.Add(new RowStyle(SizeType.Percent,45));host.Controls.Add(outer);
  for(int i=0;i<2;i++){var card=new RoundedBlock{Dock=DockStyle.Fill,Padding=new Padding(8),Margin=new Padding(2,3,5,5)};var inner=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3};inner.RowStyles.Add(new RowStyle(SizeType.Absolute,40));inner.RowStyles.Add(new RowStyle(SizeType.Absolute,34));inner.RowStyles.Add(new RowStyle(SizeType.Percent,100));for(int j=0;j<3;j++)inner.Controls.Add(controls[i*3+j],0,j);card.Controls.Add(inner);outer.Controls.Add(card,0,i);}
  foreach(var list in new[]{members,stories}){list.BorderStyle=BorderStyle.None;list.Resize+=(s,e)=>{var c=(Control)s;if(c.Width<12||c.Height<12)return;using(var path=RoundShape.Path(new RectangleF(0,0,c.Width,c.Height),6)){var prior=c.Region;c.Region=new Region(path);if(prior!=null)prior.Dispose();}};}
  foreach(var box in new[]{memberFilter,storyFilter}){var parent=(TableLayoutPanel)box.Parent;parent.Controls.Remove(box);var frame=new RoundedBlock{Dock=DockStyle.Fill,Padding=new Padding(5,4,5,3),Margin=new Padding(2)};box.BorderStyle=BorderStyle.None;frame.Controls.Add(box);parent.Controls.Add(frame,1,0);}
 }
 void ShowHelp(){MessageBox.Show("EA Column and Wall Search 1.0.3\n\n1. DATA & PROJECT: select Type/Version, Import, Save/Open project.\n2. Select Members and Stories, then Search. Find keeps hidden selections; All/None affects visible items.\n3. SEARCH & PT: Prepare PT, edit, Append PT. Blank = original; 0 = zero.\n4. OUTPUT: change names, Export CSV or Copy Data.\nOutput Names Search matches Source, Original Member and Output Name. Apply includes hidden edits.\nSave project preserves imports, PT and output names.\n\nAssumed units: kN and kNm. Mx=M3, My=M2; P=Abs(min P).\nCheck results and target column layout before engineering use.","EA Search 1.0.3");}
}
class NavyMenuColors:ProfessionalColorTable {
 public override Color ToolStripDropDownBackground{get{return Theme.Panel;}}
 public override Color ImageMarginGradientBegin{get{return Theme.Panel;}}public override Color ImageMarginGradientMiddle{get{return Theme.Panel;}}public override Color ImageMarginGradientEnd{get{return Theme.Panel;}}
 public override Color MenuItemSelected{get{return Theme.Accent;}}public override Color MenuItemSelectedGradientBegin{get{return Theme.Accent;}}public override Color MenuItemSelectedGradientEnd{get{return Theme.Accent;}}
 public override Color MenuItemPressedGradientBegin{get{return Theme.Panel;}}public override Color MenuItemPressedGradientMiddle{get{return Theme.Panel;}}public override Color MenuItemPressedGradientEnd{get{return Theme.Panel;}}
}
}
