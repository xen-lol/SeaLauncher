using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class EpicScanner
    {
        public static List<Game> Scan()
        {
            var result = new List<Game>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenInstallDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string dir in GetManifestDirectories())
            {
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(dir, "*.item", SearchOption.TopDirectoryOnly);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (string file in files)
                {
                    try
                    {
                        Game game = Read(file);
                        if (game != null && seen.Add(game.Id))
                        {
                            result.Add(game);
                            if (!string.IsNullOrWhiteSpace(game.InstallDir)) seenInstallDirs.Add(game.InstallDir);
                        }
                    }
                    catch (IOException)
                    {
                        // Epic may update a manifest while a scan is running.
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (JsonException)
                    {
                        // Ignore a malformed or partially-written manifest.
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (NotSupportedException)
                    {
                    }
                }
            }

            foreach (string gameRoot in GetGameInstallRoots())
            {
                if (!Directory.Exists(gameRoot)) continue;
                string[] gameDirs;
                try
                {
                    gameDirs = Directory.GetDirectories(gameRoot, "*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (string gameDir in gameDirs)
                {
                    try
                    {
                        string name = Path.GetFileName(gameDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        if (string.IsNullOrWhiteSpace(name) || name.Equals(".egstore", StringComparison.OrdinalIgnoreCase)) continue;

                        string exe = FindLaunchExecutable(gameDir, name);
                        string id = "epic-folder:" + gameDir;
                        if (!seenInstallDirs.Contains(gameDir) && seen.Add(id))
                        {
                            result.Add(new Game
                            {
                                Id = id,
                                Name = name,
                                OriginalName = name,
                                Platform = Platform.Epic,
                                InstallDir = gameDir,
                                ExePath = exe,
                                LaunchKey = null
                            });
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
                    {
                    }
                }
            }

            return result;
        }

        private static IEnumerable<string> GetGameInstallRoots()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddCandidate(roots, Path.Combine(programFiles, "Epic Games"));
            AddCandidate(roots, Path.Combine(programFilesX86, "Epic Games"));
            AddCandidate(roots, Path.Combine(userProfile, "Epic Games"));
            return roots;
        }

        private static string FindLaunchExecutable(string gameDir, string gameName)
        {
            var queue = new Queue<Tuple<string, int>>();
            queue.Enqueue(Tuple.Create(gameDir, 0));
            string fallback = null;
            int visited = 0;
            while (queue.Count > 0 && visited < 500)
            {
                var current = queue.Dequeue();
                visited++;
                string[] exes;
                try { exes = Directory.GetFiles(current.Item1, "*.exe", SearchOption.TopDirectoryOnly); }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { exes = Array.Empty<string>(); }

                foreach (string exe in exes)
                {
                    string fileName = Path.GetFileName(exe);
                    if (fileName.IndexOf(gameName, StringComparison.OrdinalIgnoreCase) >= 0) return exe;
                    if (fallback == null && !fileName.Contains("Uninstall", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.Contains("CrashReport", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.Contains("Prereq", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.Contains("EasyAntiCheat", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.Contains("BattlEye", StringComparison.OrdinalIgnoreCase)) fallback = exe;
                }

                if (current.Item2 >= 5) continue;
                string[] children;
                try { children = Directory.GetDirectories(current.Item1, "*", SearchOption.TopDirectoryOnly); }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { continue; }
                foreach (string child in children)
                {
                    string leaf = Path.GetFileName(child);
                    if (leaf.Equals(".egstore", StringComparison.OrdinalIgnoreCase) ||
                        leaf.Equals("Engine", StringComparison.OrdinalIgnoreCase) ||
                        leaf.Equals("Redist", StringComparison.OrdinalIgnoreCase) ||
                        leaf.Equals("Prerequisites", StringComparison.OrdinalIgnoreCase)) continue;
                    queue.Enqueue(Tuple.Create(child, current.Item2 + 1));
                }
            }
            return fallback;
        }

        private static IEnumerable<string> GetManifestDirectories()
        {
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddCandidate(candidates, Path.Combine(common, "Epic", "EpicGamesLauncher", "Data", "Manifests"));
            AddCandidate(candidates, Path.Combine(common, "Epic Games", "EpicGamesLauncher", "Data", "Manifests"));
            AddCandidate(candidates, Path.Combine(local, "EpicGamesLauncher", "Saved", "Manifests"));
            AddCandidate(candidates, Path.Combine(local, "Epic Games", "EpicGamesLauncher", "Data", "Manifests"));
            AddCandidate(candidates, Path.Combine(common, "EpicGamesLauncher", "Data", "Manifests"));

            // Standard locations, including custom launcher install locations.
            AddCandidate(candidates, Path.Combine(programFiles, "Epic Games", "Launcher", "Data", "Manifests"));
            AddCandidate(candidates, Path.Combine(programFilesX86, "Epic Games", "Launcher", "Data", "Manifests"));
            AddRegistryManifestLocations(candidates);
            AddManifestFoldersNearEpicInstalls(candidates, programFiles);
            AddManifestFoldersNearEpicInstalls(candidates, programFilesX86);
            AddManifestFoldersNearEpicInstalls(candidates, local);

            return candidates;
        }


        private static void AddRegistryManifestLocations(HashSet<string> candidates)
        {
            string[] keys =
            {
                @"SOFTWARE\Epic Games\EpicGamesLauncher",
                @"SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Epic Games Launcher",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Epic Games Launcher"
            };

            RegistryKey[] roots = { Registry.LocalMachine, Registry.CurrentUser };
            foreach (RegistryKey root in roots)
            {
                foreach (string keyPath in keys)
                {
                    try
                    {
                        using (RegistryKey key = root.OpenSubKey(keyPath))
                        {
                            if (key == null) continue;
                            AddRegistryPath(candidates, key.GetValue("InstallLocation") as string);
                            AddRegistryPath(candidates, key.GetValue("InstallDir") as string);
                            AddRegistryPath(candidates, key.GetValue("AppDataPath") as string);
                            AddRegistryPath(candidates, key.GetValue("ManifestLocation") as string);
                        }
                    }
                    catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException || ex is IOException)
                    {
                    }
                }
            }
        }

        private static void AddRegistryPath(HashSet<string> candidates, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                string path = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
                if (File.Exists(path) && string.Equals(Path.GetExtension(path), ".item", StringComparison.OrdinalIgnoreCase))
                {
                    AddCandidate(candidates, Path.GetDirectoryName(path));
                    return;
                }
                if (Directory.Exists(path))
                {
                    AddCandidate(candidates, path);
                    AddCandidate(candidates, Path.Combine(path, "Data", "Manifests"));
                    AddCandidate(candidates, Path.Combine(path, "Launcher", "Data", "Manifests"));
                    AddCandidate(candidates, Path.Combine(path, "EpicGamesLauncher", "Data", "Manifests"));
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
            }
        }

        private static void AddManifestFoldersNearEpicInstalls(HashSet<string> candidates, string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
            try
            {
                foreach (string directory in Directory.GetDirectories(root, "*Epic*", SearchOption.TopDirectoryOnly))
                {
                    AddCandidate(candidates, Path.Combine(directory, "Data", "Manifests"));
                    AddCandidate(candidates, Path.Combine(directory, "Launcher", "Data", "Manifests"));
                    AddCandidate(candidates, Path.Combine(directory, "EpicGamesLauncher", "Data", "Manifests"));
                    AddCandidate(candidates, Path.Combine(directory, "Saved", "Manifests"));
                    try
                    {
                        foreach (string child in Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                        {
                            AddCandidate(candidates, Path.Combine(child, "Data", "Manifests"));
                            AddCandidate(candidates, Path.Combine(child, "Launcher", "Data", "Manifests"));
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }

        private static void AddCandidate(HashSet<string> candidates, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                candidates.Add(path);
            }
        }

        private static Game Read(string file)
        {
            var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
            using (JsonDocument doc = JsonDocument.Parse(FileUtil.ReadText(file), options))
            {
                JsonElement root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object || Flag(root, "bIsIncompleteInstall"))
                {
                    return null;
                }

                string name = Str(root, "DisplayName");
                string appName = Str(root, "AppName");
                string location = Str(root, "InstallLocation");
                string exe = Str(root, "LaunchExecutable");
                string args = Str(root, "LaunchCommand");
                string ns = Str(root, "CatalogNamespace");
                string item = Str(root, "CatalogItemId");

                if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(location))
                {
                    return null;
                }

                string installDir;
                try
                {
                    installDir = Path.GetFullPath(location.Trim());
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                {
                    return null;
                }

                if (!Directory.Exists(installDir))
                {
                    return null;
                }

                string exePath = null;
                if (!string.IsNullOrWhiteSpace(exe))
                {
                    try
                    {
                        string candidate = Path.GetFullPath(Path.Combine(installDir, exe.Trim().Replace('/', Path.DirectorySeparatorChar)));
                        if (File.Exists(candidate))
                        {
                            exePath = candidate;
                        }
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                    {
                        // The Epic URI launch path below does not require an executable path.
                    }
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = appName;
                }

                string key = appName;
                if (!string.IsNullOrWhiteSpace(ns) && !string.IsNullOrWhiteSpace(item))
                {
                    key = ns + "%3A" + item + "%3A" + appName;
                }

                return new Game
                {
                    Id = "epic:" + appName,
                    Name = name.Trim(),
                    OriginalName = name.Trim(),
                    Platform = Platform.Epic,
                    InstallDir = installDir,
                    ExePath = exePath,
                    Arguments = args,
                    LaunchKey = key
                };
            }
        }

        private static string Str(JsonElement element, string property)
        {
            JsonElement value;
            if (element.TryGetProperty(property, out value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
            return null;
        }

        private static bool Flag(JsonElement element, string property)
        {
            JsonElement value;
            return element.TryGetProperty(property, out value) && value.ValueKind == JsonValueKind.True;
        }
    }
}
