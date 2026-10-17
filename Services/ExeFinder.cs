using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaLauncher.Services
{
    public static class ExeFinder
    {
        private static readonly string[] SkipFileParts =
        {
            "unins", "uninstall", "crash", "prereq", "redist", "dxsetup", "setup", "installer",
            "easyanticheat", "battleye", "dotnet", "cleanup", "updater", "report"
        };

        private static readonly string[] SkipFolders =
        {
            ".egstore", "engine", "redist", "redistributables", "prerequisites", "_commonredist",
            "__installer", "directx", "support", "crashreporter", "thirdparty"
        };

        public static string Find(string folder, string hint)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return null;
            }

            string wanted = Simplify(hint);
            string bestMatch = null;
            int bestMatchDepth = int.MaxValue;
            string bestAny = null;
            int bestAnyDepth = int.MaxValue;
            long bestAnySize = -1;

            var queue = new Queue<KeyValuePair<string, int>>();
            queue.Enqueue(new KeyValuePair<string, int>(folder, 0));
            int visited = 0;

            while (queue.Count > 0 && visited < 400)
            {
                KeyValuePair<string, int> item = queue.Dequeue();
                visited++;

                string[] files;
                try
                {
                    files = Directory.GetFiles(item.Key, "*.exe", SearchOption.TopDirectoryOnly);
                }
                catch (Exception)
                {
                    files = new string[0];
                }

                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (IsSkippedFile(name))
                    {
                        continue;
                    }

                    if (wanted.Length > 2 && Simplify(name).Contains(wanted) && item.Value < bestMatchDepth)
                    {
                        bestMatch = file;
                        bestMatchDepth = item.Value;
                    }

                    long size = SizeOf(file);
                    if (item.Value < bestAnyDepth || (item.Value == bestAnyDepth && size > bestAnySize))
                    {
                        bestAny = file;
                        bestAnyDepth = item.Value;
                        bestAnySize = size;
                    }
                }

                if (item.Value >= 4)
                {
                    continue;
                }

                string[] children;
                try
                {
                    children = Directory.GetDirectories(item.Key, "*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string child in children)
                {
                    string leaf = Path.GetFileName(child);
                    if (IsSkippedFolder(leaf))
                    {
                        continue;
                    }
                    queue.Enqueue(new KeyValuePair<string, int>(child, item.Value + 1));
                }
            }

            return bestMatch ?? bestAny;
        }

        private static bool IsSkippedFile(string name)
        {
            string lower = name.ToLowerInvariant();
            foreach (string part in SkipFileParts)
            {
                if (lower.Contains(part))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSkippedFolder(string leaf)
        {
            if (string.IsNullOrEmpty(leaf))
            {
                return true;
            }
            string lower = leaf.ToLowerInvariant();
            foreach (string folder in SkipFolders)
            {
                if (lower == folder)
                {
                    return true;
                }
            }
            return false;
        }

        private static long SizeOf(string file)
        {
            try
            {
                return new FileInfo(file).Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static string Simplify(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            var builder = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }
            return builder.ToString();
        }
    }
}
