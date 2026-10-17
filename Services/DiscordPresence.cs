using System;
using System.IO;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class DiscordPresence
    {
        public const string ApplicationId = "1560557393581182978";

        private static readonly object Gate = new object();
        private static readonly DateTime SessionStart = DateTime.UtcNow;

        private static System.Threading.Timer timer;
        private static Game current;
        private static DateTime launchedAt;
        private static bool started;
        private static bool enabled;
        private static bool seen;
        private static int missed;
        private static int libraryCount;

        public static void Start()
        {
            lock (Gate)
            {
                if (started)
                {
                    return;
                }
                started = true;

                string id = ResolveId();
                if (id == null)
                {
                    LaunchLog.Write("discord: rich presence is off");
                    return;
                }

                try
                {
                    DiscordBridge.Connect(id);
                    enabled = true;
                    timer = new System.Threading.Timer(Tick, null, 5000, 5000);
                    Publish();
                }
                catch (Exception ex)
                {
                    enabled = false;
                    LaunchLog.Write("discord: could not start - " + ex.Message);
                }
            }
        }

        public static void SetLibraryCount(int count)
        {
            lock (Gate)
            {
                if (libraryCount == count)
                {
                    return;
                }
                libraryCount = count;
                if (current == null)
                {
                    Publish();
                }
            }
        }

        public static void GameLaunched(Game game)
        {
            lock (Gate)
            {
                current = game;
                launchedAt = DateTime.UtcNow;
                seen = false;
                missed = 0;
                Publish();
            }
        }

        public static void Stop()
        {
            lock (Gate)
            {
                try
                {
                    if (timer != null)
                    {
                        timer.Dispose();
                        timer = null;
                    }
                    if (enabled)
                    {
                        DiscordBridge.Close();
                    }
                }
                catch (Exception ex)
                {
                    LaunchLog.Write("discord: could not stop - " + ex.Message);
                }
                enabled = false;
            }
        }

        private static void Tick(object unused)
        {
            try
            {
                Game game;
                DateTime launched;
                lock (Gate)
                {
                    game = current;
                    launched = launchedAt;
                }
                if (game == null)
                {
                    return;
                }

                bool running = ProcessProbe.IsRunningFrom(game.InstallDir);

                lock (Gate)
                {
                    if (!ReferenceEquals(current, game))
                    {
                        return;
                    }

                    if (running)
                    {
                        seen = true;
                        missed = 0;
                    }
                    else if (seen)
                    {
                        missed++;
                        if (missed >= 2)
                        {
                            GoIdle();
                        }
                    }
                    else if ((DateTime.UtcNow - launched).TotalSeconds > 120)
                    {
                        GoIdle();
                    }
                }
            }
            catch (Exception ex)
            {
                LaunchLog.Write("discord: check failed - " + ex.Message);
            }
        }

        private static void GoIdle()
        {
            current = null;
            seen = false;
            missed = 0;
            Publish();
        }

        private static void Publish()
        {
            if (!enabled)
            {
                return;
            }

            try
            {
                if (current != null)
                {
                    string state = current.Platform == Platform.Imported
                        ? "via Sea Launcher"
                        : "on " + PlatformInfo.NameOf(current.Platform);
                    DiscordBridge.Show("Playing " + current.Name, state, launchedAt);
                }
                else
                {
                    string games = libraryCount == 1 ? "1 game" : libraryCount + " games";
                    DiscordBridge.Show("Browsing the library", games, SessionStart);
                }
            }
            catch (Exception ex)
            {
                LaunchLog.Write("discord: could not update - " + ex.Message);
            }
        }

        private static string ResolveId()
        {
            try
            {
                string file = Path.Combine(Library.DataDir, "discord.txt");
                if (File.Exists(file))
                {
                    string text = File.ReadAllText(file).Trim();
                    if (string.Equals(text, "off", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                    if (LooksLikeId(text))
                    {
                        return text;
                    }
                }
            }
            catch (Exception)
            {
            }

            return LooksLikeId(ApplicationId) ? ApplicationId : null;
        }

        private static bool LooksLikeId(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 15 || text.Length > 22)
            {
                return false;
            }
            foreach (char c in text)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }
            return true;
        }
    }
}
