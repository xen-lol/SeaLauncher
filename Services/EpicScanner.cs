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
                            if (!string.IsNullOrWhiteSpace(game.InstallDir)) seenInstallDirs.Add(NormalizeDir(game.InstallDir));
                        }
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (JsonException)
                    {
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
                        if (string.IsNullOrWhiteSpace(name) || IsIgnoredFolder(name)) continue;

                        string normalized = NormalizeDir(gameDir);
                        if (seenInstallDirs.Contains(normalized)) continue;

                        string exe = ExeFinder.Find(gameDir, name);
                        if (exe == null) continue;

                        string id = "epic-folder:" + normalized;
                        if (seen.Add(id))
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
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                    AddCandidate(roots, Path.Combine(drive.RootDirectory.FullName, "Epic Games"));
                    AddCandidate(roots, Path.Combine(drive.RootDirectory.FullName, "Program Files", "Epic Games"));
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
            return roots;
        }

        private static bool IsIgnoredFolder(string name)
        {
            string[] ignored =
            {
                ".egstore", "Launcher", "Portal", "Engine", "DirectXRedist", "Epic Online Services",
                "Download", "Cache", "Prerequisites", "UnrealEngineLauncher"
            };
            foreach (string entry in ignored)
            {
                if (name.Equals(entry, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string NormalizeDir(string path)
        {
            try
            {
                return Path.GetFullPath(path).TrimEnd('\\', '/');
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                return path;
            }
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

        public static bool TryEnrichFromManifest(Game game)
        {
            if (game == null || game.Platform != Platform.Epic || string.IsNullOrWhiteSpace(game.InstallDir))
            {
                return false;
            }

            string targetDir = NormalizeDir(game.InstallDir);
            foreach (string directory in GetManifestDirectories())
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                string[] manifests;
                try
                {
                    manifests = Directory.GetFiles(directory, "*.item", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (string manifest in manifests)
                {
                    try
                    {
                        Game candidate = Read(manifest);
                        if (candidate == null || string.IsNullOrWhiteSpace(candidate.InstallDir))
                        {
                            continue;
                        }

                        if (!string.Equals(NormalizeDir(candidate.InstallDir), targetDir, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        game.Id = candidate.Id;
                        game.Name = candidate.Name;
                        game.OriginalName = candidate.OriginalName;
                        game.InstallDir = candidate.InstallDir;
                        game.ExePath = candidate.ExePath ?? game.ExePath;
                        game.Arguments = candidate.Arguments;
                        game.LaunchKey = candidate.LaunchKey;
                        LaunchLog.Write("epic: recovered manifest metadata for " + game.Name + "; launchKey=" + (string.IsNullOrWhiteSpace(game.LaunchKey) ? "<missing>" : game.LaunchKey));
                        return !string.IsNullOrWhiteSpace(game.LaunchKey);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException || ex is ArgumentException || ex is NotSupportedException)
                    {
                    }
                }
            }

            return false;
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

                string main = Str(root, "MainGameAppName");

                if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(location))
                {
                    return null;
                }
                if (!string.IsNullOrEmpty(main) && !string.Equals(main, appName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                if (IsExcludedCategory(root))
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
                    }
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = appName;
                }

                if (exePath == null)
                {
                    exePath = ExeFinder.Find(installDir, name);
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

        private static bool IsExcludedCategory(JsonElement root)
        {
            JsonElement categories;
            if (!root.TryGetProperty("AppCategories", out categories) || categories.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (JsonElement entry in categories.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String) continue;
                string value = entry.GetString();
                if (string.Equals(value, "engines", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "plugins", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "addons", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
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
