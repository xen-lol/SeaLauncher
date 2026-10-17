using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class BattleNetScanner
    {
        public static List<Game> Scan()
        {
            var result = new List<Game>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] roots =
            {
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string rootPath in roots)
            {
                try
                {
                    using (RegistryKey root = Registry.LocalMachine.OpenSubKey(rootPath))
                    {
                        if (root == null)
                        {
                            continue;
                        }

                        foreach (string subName in root.GetSubKeyNames())
                        {
                            using (RegistryKey key = root.OpenSubKey(subName))
                            {
                                Game game = Read(key);
                                if (game != null && seen.Add(game.Id))
                                {
                                    result.Add(game);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return result;
        }

        private static Game Read(RegistryKey key)
        {
            try
            {
                if (key == null)
                {
                    return null;
                }

                string publisher = key.GetValue("Publisher") as string;
                string name = key.GetValue("DisplayName") as string;
                string location = key.GetValue("InstallLocation") as string;

                if (string.IsNullOrWhiteSpace(publisher) || publisher.IndexOf("Blizzard", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return null;
                }
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
                {
                    return null;
                }
                if (name.IndexOf("Battle.net", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Agent", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return null;
                }

                string folder = Path.GetFullPath(location.Trim().Trim('"')).TrimEnd('\\', '/');
                if (!Directory.Exists(folder))
                {
                    return null;
                }

                string exe = ExeFinder.Find(folder, name);
                if (exe == null)
                {
                    return null;
                }

                return new Game
                {
                    Id = "battlenet:" + FileUtil.ShortHash(folder.ToLowerInvariant()),
                    Name = name.Trim(),
                    Platform = Platform.BattleNet,
                    InstallDir = folder,
                    ExePath = exe
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
