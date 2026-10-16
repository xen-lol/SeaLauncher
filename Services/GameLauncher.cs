using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class GameLauncher
    {
        public static bool Launch(Game game, out string error)
        {
            error = null;
            try
            {
                switch (game.Platform)
                {
                    case Platform.Steam:
                        Shell("steam" + Uri.SchemeDelimiter + "rungameid/" + game.LaunchKey, null);
                        return true;

                    case Platform.Epic:
                        try
                        {
                            Shell("com.epicgames.launcher" + Uri.SchemeDelimiter + "apps/" + game.LaunchKey + "?action=launch&silent=true", null);
                            return true;
                        }
                        catch (Exception)
                        {
                            return StartExe(game, out error);
                        }

                    case Platform.Xbox:
                        if (!string.IsNullOrEmpty(game.LaunchKey))
                        {
                            Shell("explorer.exe", "shell:AppsFolder\\" + game.LaunchKey);
                            return true;
                        }
                        return StartExe(game, out error);

                    default:
                        return StartExe(game, out error);
                }
            }
            catch (Win32Exception ex)
            {
                error = ex.NativeErrorCode == 1223 ? "Launch was cancelled." : "Could not launch " + game.Name + ".";
                return false;
            }
            catch (Exception)
            {
                error = "Could not launch " + game.Name + ".";
                return false;
            }
        }

        private static void Shell(string file, string arguments)
        {
            var info = new ProcessStartInfo(file);
            info.UseShellExecute = true;
            if (arguments != null)
            {
                info.Arguments = arguments;
            }
            Process process = Process.Start(info);
            if (process != null)
            {
                process.Dispose();
            }
        }

        private static bool StartExe(Game game, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(game.ExePath) || !File.Exists(game.ExePath))
            {
                error = "The game file could not be found.";
                return false;
            }

            var info = new ProcessStartInfo(game.ExePath);
            info.UseShellExecute = true;

            string extension = Path.GetExtension(game.ExePath).ToLowerInvariant();
            if (extension != ".lnk" && extension != ".url")
            {
                string folder = Path.GetDirectoryName(game.ExePath);
                if (!string.IsNullOrEmpty(folder))
                {
                    info.WorkingDirectory = folder;
                }
                if (!string.IsNullOrWhiteSpace(game.Arguments))
                {
                    info.Arguments = game.Arguments;
                }
            }

            Process process = Process.Start(info);
            if (process != null)
            {
                process.Dispose();
            }
            return true;
        }
    }
}
