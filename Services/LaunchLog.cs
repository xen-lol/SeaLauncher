using System;
using System.IO;

namespace SeaLauncher.Services
{
    public static class LaunchLog
    {
        private static readonly object Gate = new object();

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Library.DataDir);
                    string path = Path.Combine(Library.DataDir, "launch.log");
                    var info = new FileInfo(path);
                    if (info.Exists && info.Length > 262144)
                    {
                        File.Delete(path);
                    }
                    File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
