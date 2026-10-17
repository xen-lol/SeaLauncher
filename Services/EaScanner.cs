using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class EaScanner
    {
        private static readonly string[] RelativeRoots =
        {
            @"Program Files\EA Games",
            @"Program Files (x86)\EA Games",
            @"Program Files (x86)\Origin Games",
            @"Program Files\Origin Games",
            @"EA Games",
            @"Origin Games"
        };

        public static List<Game> Scan()
        {
            var result = new List<Game>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string root in GetRoots())
            {
                string[] folders;
                try
                {
                    folders = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string folder in folders)
                {
                    try
                    {
                        string id = "ea:" + FileUtil.ShortHash(folder.ToLowerInvariant());
                        if (!seen.Add(id))
                        {
                            continue;
                        }

                        Game game = Read(folder, id);
                        if (game != null)
                        {
                            result.Add(game);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return result;
        }

        private static IEnumerable<string> GetRoots()
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable)
                    {
                        continue;
                    }
                    if (!drive.IsReady)
                    {
                        continue;
                    }
                    foreach (string relative in RelativeRoots)
                    {
                        string candidate = Path.Combine(drive.RootDirectory.FullName, relative);
                        if (Directory.Exists(candidate))
                        {
                            roots.Add(candidate);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return roots;
        }

        private static Game Read(string folder, string id)
        {
            string folderName = Path.GetFileName(folder.TrimEnd('\\', '/'));
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return null;
            }

            string name = folderName;
            string exe = null;

            string manifest = Path.Combine(folder, "__Installer", "installerdata.xml");
            if (File.Exists(manifest))
            {
                try
                {
                    XDocument doc;
                    using (var stream = new FileStream(manifest, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        doc = XDocument.Load(stream);
                    }

                    string title = PickTitle(doc);
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        name = title.Trim();
                    }
                    exe = PickLauncher(doc, folder);
                }
                catch (Exception)
                {
                }
            }

            if (exe == null)
            {
                exe = ExeFinder.Find(folder, name);
            }
            if (exe == null)
            {
                return null;
            }

            return new Game
            {
                Id = id,
                Name = name,
                Platform = Platform.Ea,
                InstallDir = folder,
                ExePath = exe
            };
        }

        private static string PickTitle(XDocument doc)
        {
            List<XElement> titles = doc.Descendants().Where(e => e.Name.LocalName == "gameTitle").ToList();
            foreach (XElement title in titles)
            {
                XAttribute locale = title.Attribute("locale");
                if (locale != null && locale.Value.StartsWith("en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(title.Value))
                {
                    return title.Value;
                }
            }
            foreach (XElement title in titles)
            {
                if (!string.IsNullOrWhiteSpace(title.Value))
                {
                    return title.Value;
                }
            }
            return null;
        }

        private static string PickLauncher(XDocument doc, string folder)
        {
            foreach (XElement element in doc.Descendants().Where(e => e.Name.LocalName == "filePath"))
            {
                string text = element.Value == null ? "" : element.Value.Trim();
                int bracket = text.LastIndexOf(']');
                if (bracket >= 0)
                {
                    text = text.Substring(bracket + 1);
                }
                text = text.Trim().TrimStart('\\', '/');

                if (!text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                string lower = text.ToLowerInvariant();
                if (lower.Contains("unins") || lower.Contains("cleanup") || lower.Contains("touchup"))
                {
                    continue;
                }

                try
                {
                    string candidate = Path.Combine(folder, text);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }
    }
}
