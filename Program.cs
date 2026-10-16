using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SeaLauncher.Services;

namespace SeaLauncher
{
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "SeaLauncher.SingleInstance.4f2c91", out created))
            {
                if (!created)
                {
                    return 0;
                }

                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.DispatcherUnhandledException += OnUnhandled;

                var window = new MainWindow();
                return app.Run(window);
            }
        }

        private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Library.DataDir);
                string line = DateTime.Now.ToString("s") + "  " + e.Exception + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(Path.Combine(Library.DataDir, "error.log"), line);
            }
            catch
            {
            }
            e.Handled = true;
        }
    }
}
