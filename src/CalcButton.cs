using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Calculator
{
    internal enum KeyStyle
    {
        Number,
        Function,
        Accent,
        Ghost
    }

    internal enum KeyIcon
    {
        None,
        Menu,
        Backspace,
        Chevron,
        Info
    }

    /// <summary>
    /// Flat, rounded calculator key with custom hover and pressed states.
    /// </summary>
    internal sealed class CalcButton : Button
    {
        private bool _hover;
        private bool _pressed;
        private KeyStyle _style = KeyStyle.Number;
        private KeyIcon _icon = KeyIcon.None;

        public CalcButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.KeyFont;
            TabStop = false;
            Cursor = Cursors.Hand;
        }

        public KeyStyle Style
        {
            get { return _style; }
            set
            {
                _style = value;
                Invalidate();
            }
        }

        public KeyIcon Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                _pressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (!Enabled)
            {
                _hover = false;
                _pressed = false;
            }

            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (SolidBrush background = new SolidBrush(BackColor))
            {
                g.FillRectangle(background, ClientRectangle);
            }

            Color fill = GetFillColor();
            if (fill.A > 0)
            {
                float scale = DeviceDpi / 96f;
                RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                using (GraphicsPath path = Theme.RoundedRect(rect, 5f * scale))
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
            }

            Color foreground = GetForegroundColor();
            if (_icon != KeyIcon.None)
            {
                DrawIcon(g, foreground);
            }
            else if (!string.IsNullOrEmpty(Text))
            {
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
                                        TextFormatFlags.VerticalCenter |
                                        TextFormatFlags.SingleLine |
                                        TextFormatFlags.NoPadding |
                                        TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, foreground, flags);
            }
        }

        private Color GetFillColor()
        {
            switch (_style)
            {
                case KeyStyle.Accent:
                    if (!Enabled)
                    {
                        return Theme.Blend(Theme.Accent, Theme.Background, 0.55f);
                    }

                    return _pressed ? Theme.AccentPressed : (_hover ? Theme.AccentHover : Theme.Accent);

                case KeyStyle.Function:
                    return _pressed ? Theme.FunctionPressed : (_hover ? Theme.FunctionHover : Theme.FunctionFill);

                case KeyStyle.Ghost:
                    if (!Enabled)
                    {
                        return Color.Transparent;
                    }

                    return _pressed ? Theme.GhostPressed : (_hover ? Theme.GhostHover : Color.Transparent);

                default:
                    return _pressed ? Theme.KeyPressed : (_hover ? Theme.KeyHover : Theme.KeyFill);
            }
        }

        private Color GetForegroundColor()
        {
            if (!Enabled)
            {
                return Theme.TextDisabled;
            }

            if (_style == KeyStyle.Accent)
            {
                return Theme.AccentText;
            }

            if (_style == KeyStyle.Ghost && (_hover || _pressed))
            {
                return Theme.Text;
            }

            return ForeColor;
        }

        private void DrawIcon(Graphics g, Color color)
        {
            float scale = DeviceDpi / 96f;
            float cx = Width / 2f;
            float cy = Height / 2f;

            using (Pen pen = new Pen(color, 1.6f * scale))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (_icon)
                {
                    case KeyIcon.Menu:
                    {
                        float half = 7.5f * scale;
                        float gap = 4.8f * scale;
                        g.DrawLine(pen, cx - half, cy - gap, cx + half, cy - gap);
                        g.DrawLine(pen, cx - half, cy, cx + half, cy);
                        g.DrawLine(pen, cx - half, cy + gap, cx + half, cy + gap);
                        break;
                    }

                    case KeyIcon.Backspace:
                    {
                        float w = 9.5f * scale;
                        float h = 7.2f * scale;
                        PointF[] points =
                        {
                            new PointF(cx - w, cy),
                            new PointF(cx - w * 0.55f, cy - h),
                            new PointF(cx + w * 0.95f, cy - h),
                            new PointF(cx + w * 0.95f, cy + h),
                            new PointF(cx - w * 0.55f, cy + h)
                        };
                        g.DrawPolygon(pen, points);

                        float xs = 3.1f * scale;
                        float xo = 0.4f * scale;
                        g.DrawLine(pen, cx - xs + xo, cy - xs, cx + xs + xo, cy + xs);
                        g.DrawLine(pen, cx - xs + xo, cy + xs, cx + xs + xo, cy - xs);
                        break;
                    }

                    case KeyIcon.Chevron:
                    {
                        float half = 3.8f * scale;
                        g.DrawLine(pen, cx - half, cy - 1.6f * scale, cx, cy + 1.8f * scale);
                        g.DrawLine(pen, cx, cy + 1.8f * scale, cx + half, cy - 1.6f * scale);
                        break;
                    }

                    case KeyIcon.Info:
                    {
                        float r = 6.8f * scale;
                        g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);
                        using (SolidBrush dot = new SolidBrush(color))
                        {
                            g.FillEllipse(dot, cx - 1f * scale, cy - 4.4f * scale, 2f * scale, 2f * scale);
                        }

                        g.DrawLine(pen, cx, cy - 0.8f * scale, cx, cy + 3.8f * scale);
                        break;
                    }
                }
            }
        }
    }
}
