using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SeaLauncher.Services
{
    public static class ProcessProbe
    {
        private const uint QueryLimited = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder text, ref uint size);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        public static bool IsRunningFrom(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return false;
            }

            string prefix;
            try
            {
                prefix = Path.GetFullPath(folder).TrimEnd('\\', '/') + "\\";
            }
            catch (Exception)
            {
                return false;
            }

            Process[] all;
            try
            {
                all = Process.GetProcesses();
            }
            catch (Exception)
            {
                return false;
            }

            bool found = false;
            foreach (Process process in all)
            {
                try
                {
                    if (!found)
                    {
                        string path = ImagePath(process.Id);
                        if (path != null && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
            return found;
        }

        public static bool IsRunningByName(string name)
        {
            try
            {
                Process[] list = Process.GetProcessesByName(name);
                bool any = list.Length > 0;
                foreach (Process process in list)
                {
                    process.Dispose();
                }
                return any;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ImagePath(int processId)
        {
            IntPtr handle = OpenProcess(QueryLimited, false, processId);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var text = new StringBuilder(1024);
                uint size = (uint)text.Capacity;
                if (QueryFullProcessImageName(handle, 0, text, ref size))
                {
                    return text.ToString();
                }
                return null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
    }
}
