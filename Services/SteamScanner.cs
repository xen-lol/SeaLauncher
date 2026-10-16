using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class SteamScanner
    {
        private static readonly string[] SkipPrefixes =
        {
            "Steamworks Common",
            "Steam Linux Runtime",
            "Proton ",
            "Steam Controller",
            "SteamVR Media"
        };

        public static List<Game> Scan()
        {
            var result = new List<Game>();
            string steamPath = FindSteamPath();
            if (steamPath == null)
            {
                return result;
            }

            string cache = Path.Combine(steamPath, "appcache", "librarycache");
            var seen = new HashSet<string>();

            foreach (string library in FindLibraries(steamPath))
            {
                string apps = Path.Combine(library, "steamapps");
                if (!Directory.Exists(apps))
                {
                    continue;
                }

                string[] manifests;
                try
                {
                    manifests = Directory.GetFiles(apps, "appmanifest_*.acf");
                }
                catch
                {
                    continue;
                }

                foreach (string file in manifests)
                {
                    Game game = ReadManifest(file, library, cache);
                    if (game != null && seen.Add(game.Id))
                    {
                        result.Add(game);
                    }
                }
            }

            return result;
        }

        private static Game ReadManifest(string file, string library, string cache)
        {
            try
            {
                VdfNode root = Vdf.Parse(FileUtil.ReadText(file));
                VdfNode state = root.Child("AppState");
                if (state == null)
                {
                    return null;
                }

                string appId = state.Get("appid");
                string name = state.Get("name");
                string installDir = state.Get("installdir");
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                int flags;
                int.TryParse(state.Get("StateFlags"), out flags);
                if ((flags & 4) == 0)
                {
                    return null;
                }

                foreach (string prefix in SkipPrefixes)
                {
                    if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }

                return new Game
                {
                    Id = "steam:" + appId,
                    Name = name.Trim(),
                    Platform = Platform.Steam,
                    InstallDir = Path.Combine(library, "steamapps", "common", installDir ?? ""),
                    LaunchKey = appId,
                    CoverPath = FindCover(cache, appId)
                };
            }
            catch
            {
                return null;
            }
        }

        private static string FindCover(string cache, string appId)
        {
            try
            {
                if (!Directory.Exists(cache))
                {
                    return null;
                }

                string flat = Path.Combine(cache, appId + "_library_600x900.jpg");
                if (File.Exists(flat))
                {
                    return flat;
                }

                string folder = Path.Combine(cache, appId);
                if (Directory.Exists(folder))
                {
                    string[] patterns = { "library_600x900*.jpg", "library_capsule*.jpg", "header*.jpg" };
                    foreach (string pattern in patterns)
                    {
                        foreach (string found in Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories))
                        {
                            return found;
                        }
                    }
                }

                string header = Path.Combine(cache, appId + "_header.jpg");
                if (File.Exists(header))
                {
                    return header;
                }
            }
            catch
            {
            }
            return null;
        }

        private static string FindSteamPath()
        {
            string[] candidates =
            {
                ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
                ReadRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
                ReadRegistry(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }
                try
                {
                    string full = Path.GetFullPath(candidate);
                    if (Directory.Exists(Path.Combine(full, "steamapps")))
                    {
                        return full;
                    }
                }
                catch
                {
                }
            }
            return null;
        }

        private static List<string> FindLibraries(string steamPath)
        {
            var list = new List<string>();
            AddUnique(list, steamPath);

            try
            {
                string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdf))
                {
                    VdfNode root = Vdf.Parse(FileUtil.ReadText(vdf));
                    VdfNode folders = root.Child("libraryfolders");
                    if (folders != null)
                    {
                        foreach (KeyValuePair<string, VdfNode> pair in folders.Children)
                        {
                            string path = pair.Value.Get("path");
                            if (path == null)
                            {
                                path = pair.Value.Value;
                            }
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                AddUnique(list, path);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return list;
        }

        private static void AddUnique(List<string> list, string path)
        {
            try
            {
                string full = Path.GetFullPath(path).TrimEnd('\\', '/');
                foreach (string existing in list)
                {
                    if (string.Equals(existing.TrimEnd('\\', '/'), full, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
                list.Add(full);
            }
            catch
            {
            }
        }

        private static string ReadRegistry(RegistryKey hive, string subKey, string name)
        {
            try
            {
                using (RegistryKey key = hive.OpenSubKey(subKey))
                {
                    if (key == null)
                    {
                        return null;
                    }
                    return key.GetValue(name) as string;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
