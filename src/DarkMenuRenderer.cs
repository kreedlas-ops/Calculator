using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Calculator
{
    internal sealed class DarkMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground
        {
            get { return Theme.MenuBackground; }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Theme.MenuBackground; }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Theme.MenuBackground; }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Theme.MenuBackground; }
        }

        public override Color MenuBorder
        {
            get { return Theme.MenuBorder; }
        }

        public override Color MenuItemBorder
        {
            get { return Theme.MenuHover; }
        }

        public override Color MenuItemSelected
        {
            get { return Theme.MenuHover; }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return Theme.MenuHover; }
        }

        public override Color MenuItemSelectedGradientEnd
        {
            get { return Theme.MenuHover; }
        }

        public override Color SeparatorDark
        {
            get { return Theme.MenuBorder; }
        }

        public override Color SeparatorLight
        {
            get { return Theme.MenuBorder; }
        }

        public override Color CheckBackground
        {
            get { return Theme.MenuBackground; }
        }

        public override Color CheckSelectedBackground
        {
            get { return Theme.MenuHover; }
        }
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer()
            : base(new DarkMenuColors())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Theme.MenuBackground))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Rectangle bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using (Pen pen = new Pen(Theme.MenuBorder))
            {
                e.Graphics.DrawRectangle(pen, bounds);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
            {
                return;
            }

            Rectangle bounds = new Rectangle(3, 1, e.Item.Width - 6, e.Item.Height - 2);
            using (GraphicsPath path = Theme.RoundedRect(bounds, 4f))
            using (SolidBrush brush = new SolidBrush(Theme.MenuHover))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.TextDisabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle bounds = e.ImageRectangle;
            float x = bounds.Width > 0 ? bounds.X + 2f : 8f;
            float y = e.Item.Height / 2f - 5f;

            using (Pen pen = new Pen(Theme.Accent, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawLine(pen, x, y + 5f, x + 4f, y + 9f);
                e.Graphics.DrawLine(pen, x + 4f, y + 9f, x + 10f, y + 1f);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen pen = new Pen(Theme.MenuBorder))
            {
                e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Theme.TextSecondary;
            base.OnRenderArrow(e);
        }
    }
}
