using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
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
                        LaunchLog.Write("steam: " + game.Name + " (" + game.LaunchKey + ")");
                        ShellOpen(Protocol("steam", "rungameid/" + game.LaunchKey), null);
                        return true;

                    case Platform.Epic:
                        return LaunchEpic(game, out error);

                    case Platform.Ubisoft:
                        return LaunchUbisoft(game, out error);

                    case Platform.Xbox:
                        if (!string.IsNullOrEmpty(game.LaunchKey))
                        {
                            LaunchLog.Write("xbox: " + game.Name + " (" + game.LaunchKey + ")");
                            ShellOpen("explorer.exe", "shell:AppsFolder\\" + game.LaunchKey);
                            return true;
                        }
                        return StartExe(game, out error);

                    default:
                        return StartExe(game, out error);
                }
            }
            catch (Win32Exception ex)
            {
                LaunchLog.Write("failed: " + game.Name + " - " + ex.Message);
                error = ex.NativeErrorCode == 1223 ? "Launch was cancelled." : "Could not launch " + game.Name + ".";
                return false;
            }
            catch (Exception ex)
            {
                LaunchLog.Write("failed: " + game.Name + " - " + ex.Message);
                error = "Could not launch " + game.Name + ".";
                return false;
            }
        }

        private static bool LaunchEpic(Game game, out string error)
        {
            error = null;
            if (game == null)
            {
                error = "No game was selected.";
                return false;
            }

            // Folder-based discovery can find an executable but does not know the
            // Epic catalog identifier. Recover the authoritative identifier from
            // the .item manifest before attempting to launch the game.
            if (string.IsNullOrWhiteSpace(game.LaunchKey))
            {
                EpicScanner.TryEnrichFromManifest(game);
            }

            bool launcherRegistered = EpicLauncherRegistered();
            string launchKey = game.LaunchKey;

            // Fortnite's canonical Epic product key. This is used only when the
            // local manifest could not be found; launching its bootstrapper EXE
            // directly bypasses Epic's launch/authentication flow and commonly
            // results in no visible game starting.
            if (string.IsNullOrWhiteSpace(launchKey) && IsFortnite(game))
            {
                launchKey = "fn%3A4fe75bbc5a674f4f9b356b5c90567da5%3AFortnite";
                LaunchLog.Write("epic: using built-in Fortnite catalog key because no manifest key was available");
            }

            if (launcherRegistered && !string.IsNullOrWhiteSpace(launchKey))
            {
                string uri = Protocol("com.epicgames.launcher", "apps/" + launchKey + "?action=launch&silent=true");
                try
                {
                    LaunchLog.Write("epic: requesting launch through Epic Games Launcher for " + game.Name + " - " + uri);
                    ShellOpen(uri, null);

                    // Do not fall back to FortniteBootstrapper.exe: it is not a
                    // supported standalone launch path and can silently exit.
                    WatchEpicLaunch(game);
                    return true;
                }
                catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is ArgumentException)
                {
                    LaunchLog.Write("epic: protocol launch failed for " + game.Name + " - " + ex.Message);
                    error = "Windows could not open Epic Games Launcher for " + game.Name + ". Check that Epic Games Launcher is installed and repair its protocol registration.";
                    return false;
                }
            }

            string exe = ResolveExe(game);
            if (!string.IsNullOrWhiteSpace(exe) && !IsBootstrapperExecutable(exe))
            {
                LaunchLog.Write("epic: no Epic launch key available; attempting non-bootstrapper executable for " + game.Name + " - " + exe);
                return StartExe(game, out error);
            }

            LaunchLog.Write("epic: launch blocked because no catalog key or safe executable was found; launcherRegistered=" + launcherRegistered + "; launchKey=" + (string.IsNullOrWhiteSpace(launchKey) ? "<missing>" : launchKey) + "; exe=" + (exe ?? "<not found>"));
            error = launcherRegistered
                ? "Sea Launcher found Fortnite's installation but could not resolve its Epic game ID. Re-scan the library, then try again."
                : "Epic Games Launcher is not registered with Windows. Open or repair Epic Games Launcher, then try again.";
            return false;
        }

        private static bool IsFortnite(Game game)
        {
            if (game == null) return false;
            return string.Equals(game.Name, "Fortnite", StringComparison.OrdinalIgnoreCase)
                || string.Equals(game.OriginalName, "Fortnite", StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(game.InstallDir) && game.InstallDir.IndexOf("\\Fortnite", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsBootstrapperExecutable(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path ?? string.Empty);
            return name.IndexOf("bootstrapper", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("easyanticheat", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("battleye", StringComparison.OrdinalIgnoreCase) >= 0
                || string.Equals(name, "EpicGamesLauncher", StringComparison.OrdinalIgnoreCase);
        }

        private static void WatchEpicLaunch(Game game)
        {
            string folder = game.InstallDir;
            if (string.IsNullOrWhiteSpace(folder)) return;

            Task.Run(() =>
            {
                try
                {
                    DateTime deadline = DateTime.UtcNow.AddSeconds(90);
                    while (DateTime.UtcNow < deadline)
                    {
                        Thread.Sleep(3000);
                        if (ProcessProbe.IsRunningFrom(folder))
                        {
                            LaunchLog.Write("epic: game process detected after protocol launch: " + game.Name);
                            return;
                        }
                    }

                    LaunchLog.Write("epic: no process detected in install folder after protocol request for " + game.Name + "; no direct bootstrapper fallback was attempted");
                }
                catch (Exception ex)
                {
                    LaunchLog.Write("epic: launch monitor failed for " + game.Name + " - " + ex.Message);
                }
            });
        }

        private static bool LaunchUbisoft(Game game, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(game.LaunchKey))
            {
                return StartExe(game, out error);
            }

            string uri = Protocol("uplay", "launch/" + game.LaunchKey + "/0");
            LaunchLog.Write("ubisoft: " + game.Name + " - " + uri);
            ShellOpen(uri, null);
            WatchAndFallback(game, "UbisoftConnect");
            return true;
        }

        private static void WatchAndFallback(Game game, string launcherProcess)
        {
            string folder = game.InstallDir;
            string exe = ResolveExe(game);
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(exe))
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    DateTime started = DateTime.UtcNow;
                    while (true)
                    {
                        Thread.Sleep(2000);

                        if (ProcessProbe.IsRunningFrom(folder))
                        {
                            LaunchLog.Write("started through the launcher: " + game.Name);
                            return;
                        }

                        double seconds = (DateTime.UtcNow - started).TotalSeconds;
                        bool launcherRunning = ProcessProbe.IsRunningByName(launcherProcess);
                        if ((seconds > 40 && !launcherRunning) || seconds > 120)
                        {
                            break;
                        }
                    }

                    LaunchLog.Write("the launcher did not start " + game.Name + ", starting the game file directly");
                    string ignored;
                    StartExe(game, out ignored);
                }
                catch (Exception ex)
                {
                    LaunchLog.Write("watch failed: " + game.Name + " - " + ex.Message);
                }
            });
        }

        private static bool EpicLauncherRegistered()
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"com.epicgames.launcher\shell\open\command"))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    string command = key.GetValue("") as string;
                    if (string.IsNullOrWhiteSpace(command))
                    {
                        return false;
                    }

                    command = command.Trim();
                    string path;
                    if (command.StartsWith("\""))
                    {
                        int close = command.IndexOf('"', 1);
                        path = close > 1 ? command.Substring(1, close - 1) : command.Trim('"');
                    }
                    else
                    {
                        int exeEnd = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                        path = exeEnd > 0 ? command.Substring(0, exeEnd + 4) : command;
                    }
                    return File.Exists(path);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ResolveExe(Game game)
        {
            if (!string.IsNullOrEmpty(game.ExePath) && File.Exists(game.ExePath))
            {
                return game.ExePath;
            }

            if (!string.IsNullOrEmpty(game.InstallDir) && Directory.Exists(game.InstallDir))
            {
                string found = ExeFinder.Find(game.InstallDir, game.OriginalName ?? game.Name);
                if (found != null)
                {
                    game.ExePath = found;
                    return found;
                }
            }
            return null;
        }

        private static string Protocol(string scheme, string rest)
        {
            return scheme + Uri.SchemeDelimiter + rest;
        }

        private static void ShellOpen(string file, string arguments)
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
            string exe = ResolveExe(game);
            if (exe == null)
            {
                LaunchLog.Write("no game file found for " + game.Name);
                error = "The game file could not be found.";
                return false;
            }

            var info = new ProcessStartInfo(exe);
            info.UseShellExecute = true;

            string extension = Path.GetExtension(exe).ToLowerInvariant();
            if (extension != ".lnk" && extension != ".url")
            {
                string folder = Path.GetDirectoryName(exe);
                if (!string.IsNullOrEmpty(folder))
                {
                    info.WorkingDirectory = folder;
                }
                if (!string.IsNullOrWhiteSpace(game.Arguments))
                {
                    info.Arguments = game.Arguments;
                }
            }

            LaunchLog.Write("exe: " + game.Name + " - " + exe);
            Process process = Process.Start(info);
            if (process != null)
            {
                process.Dispose();
            }
            return true;
        }
    }
}
