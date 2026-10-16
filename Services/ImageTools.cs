using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeaLauncher.Services
{
    public static class ImageTools
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "PrivateExtractIconsW")]
        private static extern uint PrivateExtractIcons(string file, int index, int width, int height, IntPtr[] icons, int[] ids, uint count, uint flags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr icon);

        public static ImageSource LoadFile(string path, int decodeWidth)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.DecodePixelWidth = decodeWidth;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static ImageSource ExtractIcon(string path)
        {
            IntPtr[] icons = new IntPtr[1];
            int[] ids = new int[1];
            try
            {
                uint count = PrivateExtractIcons(path, 0, 256, 256, icons, ids, 1, 0);
                if (count == 0 || count == 0xFFFFFFFF || icons[0] == IntPtr.Zero)
                {
                    return null;
                }

                BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(icons[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (icons[0] != IntPtr.Zero)
                {
                    DestroyIcon(icons[0]);
                }
            }
        }
    }
}
