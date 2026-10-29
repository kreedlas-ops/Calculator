using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Calculator
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(0x1F, 0x1F, 0x29);
        public static readonly Color KeyFill = Color.FromArgb(0x30, 0x30, 0x3D);
        public static readonly Color KeyHover = Color.FromArgb(0x3A, 0x3A, 0x49);
        public static readonly Color KeyPressed = Color.FromArgb(0x28, 0x28, 0x33);
        public static readonly Color FunctionFill = Color.FromArgb(0x29, 0x29, 0x34);
        public static readonly Color FunctionHover = Color.FromArgb(0x34, 0x34, 0x41);
        public static readonly Color FunctionPressed = Color.FromArgb(0x23, 0x23, 0x2D);
        public static readonly Color GhostHover = Color.FromArgb(0x2D, 0x2D, 0x39);
        public static readonly Color GhostPressed = Color.FromArgb(0x26, 0x26, 0x30);
        public static readonly Color Accent = Color.FromArgb(0x62, 0xCD, 0xF9);
        public static readonly Color AccentHover = Color.FromArgb(0x86, 0xDB, 0xFF);
        public static readonly Color AccentPressed = Color.FromArgb(0x4C, 0xB6, 0xE4);
        public static readonly Color AccentText = Color.FromArgb(0x06, 0x22, 0x2E);
        public static readonly Color Text = Color.FromArgb(0xFF, 0xFF, 0xFF);
        public static readonly Color TextSecondary = Color.FromArgb(0xA7, 0xA7, 0xB8);
        public static readonly Color TextDisabled = Color.FromArgb(0x59, 0x59, 0x69);
        public static readonly Color MenuBackground = Color.FromArgb(0x26, 0x26, 0x31);
        public static readonly Color MenuHover = Color.FromArgb(0x35, 0x35, 0x43);
        public static readonly Color MenuBorder = Color.FromArgb(0x3C, 0x3C, 0x4B);
        public static readonly Color Border = Color.FromArgb(0x34, 0x34, 0x3F);

        public static readonly string DisplayFamily = ResolveFamily("Segoe UI Variable Display", "Segoe UI");
        public static readonly string DisplaySemiboldFamily =
            ResolveFamily("Segoe UI Variable Display Semibold", "Segoe UI");
        public static readonly string TextFamily = ResolveFamily("Segoe UI Variable Text", "Segoe UI");

        public static readonly Font ModeFont = new Font(DisplaySemiboldFamily, 17f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font ExpressionFont = new Font(TextFamily, 11f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font KeyFont = new Font(TextFamily, 16f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font SmallKeyFont = new Font(TextFamily, 11f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font PercentFont = new Font(TextFamily, 15f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font AccentFont = new Font(TextFamily, 20f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font MemoryFont = new Font(TextFamily, 9.5f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font DialogFont = new Font(TextFamily, 10.5f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static readonly Font DialogMonoFont = new Font("Consolas", 10.5f, FontStyle.Regular,
            GraphicsUnit.Point);

        public static Font ResultFont(float points)
        {
            return new Font(DisplayFamily, points, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static string ResolveFamily(string preferred, string fallback)
        {
            try
            {
                FontFamily[] families = FontFamily.Families;
                for (int i = 0; i < families.Length; i++)
                {
                    if (string.Equals(families[i].Name, preferred, StringComparison.OrdinalIgnoreCase))
                    {
                        return preferred;
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to the default UI font.
            }

            return fallback;
        }

        public static Color Blend(Color from, Color to, float amount)
        {
            if (amount < 0f)
            {
                amount = 0f;
            }

            if (amount > 1f)
            {
                amount = 1f;
            }

            int r = (int)Math.Round(from.R + (to.R - from.R) * amount);
            int g = (int)Math.Round(from.G + (to.G - from.G) * amount);
            int b = (int)Math.Round(from.B + (to.B - from.B) * amount);
            return Color.FromArgb(r, g, b);
        }

        public static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float max = Math.Min(rect.Width, rect.Height) / 2f;
            if (radius > max)
            {
                radius = max;
            }

            if (radius <= 0.1f)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180f, 90f);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270f, 90f);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }
}
