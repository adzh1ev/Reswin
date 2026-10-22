using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Reswin {
    // Native dismissal/keyboard support, with a rounded window silhouette.
    internal sealed class TrayMenu : ContextMenuStrip {
        readonly Font menuFont=new Font("Segoe UI",9.5f);
        internal TrayMenu() {
            Font=menuFont; ShowImageMargin=false; ShowCheckMargin=false;
            DropShadowEnabled=true; Renderer=new MinimalRenderer();
        }
        protected override void OnOpening(System.ComponentModel.CancelEventArgs e) {
            float scale=DeviceDpi/96f;
            Padding=new Padding((int)(4*scale),(int)(5*scale),(int)(4*scale),(int)(5*scale));
            BackColor=MinimalRenderer.Background; ForeColor=MinimalRenderer.Foreground;
            int width=(int)(212*scale);
            foreach(ToolStripItem item in Items)
                if(!(item is ToolStripSeparator)) width=Math.Max(width,TextRenderer.MeasureText(item.Text,Font).Width+(int)(48*scale));
            foreach(ToolStripItem item in Items) {
                item.ForeColor=ForeColor; item.AutoSize=false;
                item.Padding=Padding.Empty; item.Margin=Padding.Empty;
                item.Size=new Size(width,(int)((item is ToolStripSeparator?9:30)*scale));
            }
            base.OnOpening(e); RoundWindow();
        }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); RoundWindow(); }
        void RoundWindow() {
            if(Width<20 || Height<20) return;
            using(var path=Rounded(new RectangleF(0,0,Width,Height),10*DeviceDpi/96f)) {
                var old=Region; Region=new Region(path); if(old!=null) old.Dispose();
            }
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if(disposing) menuFont.Dispose(); }
        static GraphicsPath Rounded(RectangleF r,float radius) {
            var path=new GraphicsPath(); float d=radius*2;
            path.AddArc(r.Left,r.Top,d,d,180,90); path.AddArc(r.Right-d,r.Top,d,d,270,90);
            path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); path.AddArc(r.Left,r.Bottom-d,d,d,90,90);
            path.CloseFigure(); return path;
        }
        sealed class MinimalRenderer : ToolStripProfessionalRenderer {
            internal static Color Background { get { return SystemInformation.HighContrast?SystemColors.Menu:Color.FromArgb(31,31,31); } }
            internal static Color Foreground { get { return SystemInformation.HighContrast?SystemColors.MenuText:Color.FromArgb(242,242,242); } }
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { e.Graphics.Clear(Background); }
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) { }
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
                if(!e.Item.Selected) return;
                using(var path=Rounded(new RectangleF(0,0,e.Item.Width,e.Item.Height),4*e.Graphics.DpiX/96f))
                using(var brush=new SolidBrush(SystemInformation.HighContrast?SystemColors.Highlight:Color.FromArgb(49,49,49))) {
                    e.Graphics.SmoothingMode=SmoothingMode.AntiAlias; e.Graphics.FillPath(brush,path);
                }
            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
                int inset=(int)(24*e.Graphics.DpiX/96f);
                e.TextRectangle=new Rectangle(inset,0,e.Item.Width-inset-10,e.Item.Height);
                e.TextFormat=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;
                e.TextColor=SystemInformation.HighContrast && e.Item.Selected?SystemColors.HighlightText:Foreground;
                base.OnRenderItemText(e);
            }
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
                using(var pen=new Pen(SystemInformation.HighContrast?SystemColors.MenuText:Color.FromArgb(75,75,75)))
                    e.Graphics.DrawLine(pen,0,e.Item.Height/2,e.Item.Width,e.Item.Height/2);
            }
        }
    }
}
