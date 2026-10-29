using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Calculator
{
    internal static class Native
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwaBorderColor = 34;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaTextColor = 36;
        private const int DwmcpRound = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] keyState);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint code, uint mapType);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicode(uint virtualKey, uint scanCode, byte[] keyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 16)] StringBuilder buffer, int bufferSize,
            uint flags);

        public static void ApplyDarkChrome(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                int enabled = 1;
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));

                int corner = DwmcpRound;
                DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref corner, sizeof(int));

                int caption = ColorTranslator.ToWin32(Theme.Background);
                int text = ColorTranslator.ToWin32(Theme.Text);
                int border = ColorTranslator.ToWin32(Theme.Border);
                DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref caption, sizeof(int));
                DwmSetWindowAttribute(handle, DwmwaTextColor, ref text, sizeof(int));
                DwmSetWindowAttribute(handle, DwmwaBorderColor, ref border, sizeof(int));
            }
            catch (EntryPointNotFoundException)
            {
                // Older Windows builds simply keep their default frame.
            }
            catch (DllNotFoundException)
            {
                // Extremely old systems: nothing to do.
            }
        }

        /// <summary>
        /// Resolves the character for a key press using the active keyboard layout,
        /// so punctuation works on non-US layouts too.
        /// </summary>
        public static bool TryGetCharacter(Keys keyData, out char character)
        {
            character = '\0';

            Keys key = keyData & Keys.KeyCode;
            Keys modifiers = keyData & Keys.Modifiers;
            bool shift = (modifiers & Keys.Shift) == Keys.Shift;
            bool control = (modifiers & Keys.Control) == Keys.Control;
            bool alt = (modifiers & Keys.Alt) == Keys.Alt;

            byte[] state = new byte[256];
            if (!GetKeyboardState(state))
            {
                return false;
            }

            state[0x10] = shift ? (byte)0x80 : (byte)0;
            state[0x11] = control ? (byte)0x80 : (byte)0;
            state[0x12] = alt ? (byte)0x80 : (byte)0;

            uint virtualKey = (uint)key;
            uint scanCode = MapVirtualKey(virtualKey, 0);
            StringBuilder buffer = new StringBuilder(8);
            int count = ToUnicode(virtualKey, scanCode, state, buffer, buffer.Capacity, 0);
            if (count > 0)
            {
                character = buffer[0];
                return true;
            }

            return false;
        }
    }
}
