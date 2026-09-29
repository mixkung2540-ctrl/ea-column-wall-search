using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;
class MakeIcon{
 static void Main(string[] args){using(var b=new Bitmap(256,256))using(var g=Graphics.FromImage(b)){
  g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
  using(var p=new GraphicsPath()){p.AddArc(8,8,42,42,180,90);p.AddArc(206,8,42,42,270,90);p.AddArc(206,206,42,42,0,90);p.AddArc(8,206,42,42,90,90);p.CloseFigure();using(var fill=new LinearGradientBrush(new Rectangle(0,0,256,256),Color.FromArgb(42,65,91),Color.FromArgb(16,30,47),90))g.FillPath(fill,p);using(var pen=new Pen(Color.FromArgb(67,110,147),4))g.DrawPath(pen,p);}
  using(var blue=new SolidBrush(Color.FromArgb(53,175,212)))using(var white=new SolidBrush(Color.FromArgb(232,239,246))){
   g.FillRectangle(blue,40,40,25,74);g.FillRectangle(white,79,40,27,74);g.FillRectangle(blue,118,40,98,18);g.FillRectangle(blue,118,68,98,18);g.FillRectangle(blue,118,96,98,18);
   using(var f=new Font("Segoe UI",66,FontStyle.Bold,GraphicsUnit.Pixel))g.DrawString("EA",f,white,new RectangleF(31,129,200,85),new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center});
  }
  b.Save(Path.Combine(args[0],"EA_Search_Icon.png"),ImageFormat.Png);using(var mem=new MemoryStream()){b.Save(mem,ImageFormat.Png);var bytes=mem.ToArray();using(var w=new BinaryWriter(File.Create(Path.Combine(args[0],"EA_Search.ico")))){w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)1);w.Write((byte)0);w.Write((byte)0);w.Write((byte)0);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)32);w.Write(bytes.Length);w.Write(22);w.Write(bytes);}}
 }}
}
