using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class GogScanner
    {
        public static List<Game> Scan()
        {
            var result = new List<Game>();
            var seen = new HashSet<string>();
            string[] roots =
            {
                @"SOFTWARE\WOW6432Node\GOG.com\Games",
                @"SOFTWARE\GOG.com\Games"
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
                                Game game = Read(key, subName);
                                if (game != null && seen.Add(game.Id))
                                {
                                    result.Add(game);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return result;
        }

        private static Game Read(RegistryKey key, string subName)
        {
            try
            {
                if (key == null)
                {
                    return null;
                }

                string name = key.GetValue("gameName") as string;
                string exe = key.GetValue("exe") as string;
                string path = key.GetValue("path") as string;
                string args = key.GetValue("launchParam") as string;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                {
                    return null;
                }

                return new Game
                {
                    Id = "gog:" + subName,
                    Name = name.Trim(),
                    Platform = Platform.Gog,
                    InstallDir = string.IsNullOrWhiteSpace(path) ? Path.GetDirectoryName(exe) : path,
                    ExePath = exe,
                    Arguments = args
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
