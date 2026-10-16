using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SeaLauncher.Ui
{
    public static class WindowTools
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

        public static void StyleTitleBar(Window window)
        {
            try
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                int dark = 1;
                DwmSetWindowAttribute(handle, 20, ref dark, 4);
                DwmSetWindowAttribute(handle, 19, ref dark, 4);

                int caption = ColorRef(4, 20, 34);
                DwmSetWindowAttribute(handle, 35, ref caption, 4);

                int text = ColorRef(232, 246, 252);
                DwmSetWindowAttribute(handle, 36, ref text, 4);
            }
            catch
            {
            }
        }

        private static int ColorRef(int r, int g, int b)
        {
            return r | (g << 8) | (b << 16);
        }
    }
}
