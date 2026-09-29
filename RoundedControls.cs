using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace EASearch {
static class RoundShape {
 public static GraphicsPath Path(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
public sealed class RoundedButton:Button {
 bool hover,down;
 public RoundedButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=down=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){down=true;Invalidate();base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){down=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Parent==null?Theme.Panel:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
  var r=new RectangleF(0.5f,0.5f,Width-1,Height-1);Color a=down?Color.FromArgb(38,95,124):hover?Color.FromArgb(39,65,91):Color.FromArgb(26,44,65);Color b=down?Theme.Accent:Color.FromArgb(19,33,51);
  using(var shape=RoundShape.Path(r,5))using(var fill=new LinearGradientBrush(r,a,b,90))using(var border=new Pen(hover?Color.FromArgb(78,128,161):Color.FromArgb(43,63,85))){g.FillPath(fill,shape);g.DrawPath(border,shape);}
  var textArea=new Rectangle(5,3,Width-10,Height-6);
  if(Image!=null){int size=Math.Min(26,Height/2);g.DrawImage(Image,new Rectangle((Width-size)/2,6,size,size));textArea=new Rectangle(3,size+7,Width-6,Height-size-10);}
  TextRenderer.DrawText(g,Text,Font,textArea,Enabled?ForeColor:Color.FromArgb(125,140,155),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
  if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(4,4,Width-8,Height-8),Theme.Ink,Theme.Navy);
 }
}
public sealed class RoundedBlock:Panel {
 public RoundedBlock(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=RoundShape.Path(new RectangleF(0.5f,0.5f,Width-1,Height-1),6))using(var border=new Pen(Color.FromArgb(54,75,99))){e.Graphics.DrawPath(border,path);}TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(9,2,Width-18,14),ForeColor,TextFormatFlags.Left|TextFormatFlags.NoPrefix);}
}
public sealed class NavyTabs:TabControl {
 public NavyTabs(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.Clear(Theme.Panel);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  for(int i=0;i<TabPages.Count;i++){var r=GetTabRect(i);r.Inflate(-2,-2);using(var path=RoundShape.Path(r,5))using(var fill=new SolidBrush(i==SelectedIndex?Theme.Accent:Theme.Navy))using(var pen=new Pen(Color.FromArgb(58,82,107))){e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(pen,path);}TextRenderer.DrawText(e.Graphics,TabPages[i].Text,Font,r,Theme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
 }
}
}
