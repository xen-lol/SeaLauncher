using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class UbisoftScanner
    {
        public static List<Game> Scan()
        {
            var result = new List<Game>();
            var seen = new HashSet<string>();
            string[] roots =
            {
                @"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs",
                @"SOFTWARE\Ubisoft\Launcher\Installs"
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

                        foreach (string gameId in root.GetSubKeyNames())
                        {
                            using (RegistryKey key = root.OpenSubKey(gameId))
                            {
                                Game game = Read(key, gameId);
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

        private static Game Read(RegistryKey key, string gameId)
        {
            try
            {
                if (key == null)
                {
                    return null;
                }

                string location = key.GetValue("InstallDir") as string;
                if (string.IsNullOrWhiteSpace(location))
                {
                    return null;
                }

                string folder = Path.GetFullPath(location.Trim()).TrimEnd('\\', '/');
                if (!Directory.Exists(folder))
                {
                    return null;
                }

                string name = Path.GetFileName(folder);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                return new Game
                {
                    Id = "ubisoft:" + gameId,
                    Name = name,
                    Platform = Platform.Ubisoft,
                    InstallDir = folder,
                    ExePath = ExeFinder.Find(folder, name),
                    LaunchKey = gameId
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
