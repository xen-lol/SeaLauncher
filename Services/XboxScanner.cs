using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using SeaLauncher.Models;

namespace SeaLauncher.Services
{
    public static class XboxScanner
    {
        public static List<Game> Scan()
        {
            var result = new List<Game>();
            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch
            {
                return result;
            }

            var seen = new HashSet<string>();
            foreach (DriveInfo drive in drives)
            {
                try
                {
                    if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable)
                    {
                        continue;
                    }
                    if (!drive.IsReady)
                    {
                        continue;
                    }

                    string root = Path.Combine(drive.RootDirectory.FullName, "XboxGames");
                    if (!Directory.Exists(root))
                    {
                        continue;
                    }

                    foreach (string folder in Directory.GetDirectories(root))
                    {
                        Game game = ReadFolder(folder);
                        if (game != null && seen.Add(game.Id))
                        {
                            result.Add(game);
                        }
                    }
                }
                catch
                {
                }
            }
            return result;
        }

        private static Game ReadFolder(string folder)
        {
            try
            {
                string content = Path.Combine(folder, "Content");
                string config = Path.Combine(content, "MicrosoftGame.Config");
                if (!File.Exists(config))
                {
                    content = folder;
                    config = Path.Combine(folder, "MicrosoftGame.Config");
                    if (!File.Exists(config))
                    {
                        return null;
                    }
                }

                XDocument doc;
                using (var stream = new FileStream(config, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    doc = XDocument.Load(stream);
                }

                XElement identity = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Identity");
                XElement visuals = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "ShellVisuals");
                XElement executable = PickExecutable(doc);

                string folderName = Path.GetFileName(folder.TrimEnd('\\', '/'));
                string name = Attr(visuals, "DefaultDisplayName");
                if (string.IsNullOrWhiteSpace(name) || name.StartsWith("ms-resource", StringComparison.OrdinalIgnoreCase))
                {
                    name = folderName;
                }

                string packageName = Attr(identity, "Name");
                string publisher = Attr(identity, "Publisher");
                string appId = Attr(executable, "Id");
                if (string.IsNullOrWhiteSpace(appId))
                {
                    appId = "Game";
                }

                string aumid = null;
                if (!string.IsNullOrWhiteSpace(packageName) && !string.IsNullOrWhiteSpace(publisher))
                {
                    aumid = packageName + "_" + PublisherId(publisher) + "!" + appId;
                }

                string exePath = null;
                string exeName = Attr(executable, "Name");
                if (!string.IsNullOrWhiteSpace(exeName))
                {
                    string candidate = Path.Combine(content, exeName);
                    if (File.Exists(candidate))
                    {
                        exePath = candidate;
                    }
                }

                if (aumid == null && exePath == null)
                {
                    return null;
                }

                string icon = null;
                string[] logoKeys = { "Square480x480Logo", "Square150x150Logo", "StoreLogo", "Square44x44Logo" };
                foreach (string logoKey in logoKeys)
                {
                    string relative = Attr(visuals, logoKey);
                    if (string.IsNullOrWhiteSpace(relative))
                    {
                        continue;
                    }
                    string logoPath = Path.Combine(content, relative);
                    if (File.Exists(logoPath))
                    {
                        icon = logoPath;
                        break;
                    }
                }

                return new Game
                {
                    Id = "xbox:" + (packageName ?? folderName),
                    Name = name.Trim(),
                    Platform = Platform.Xbox,
                    InstallDir = folder,
                    ExePath = exePath,
                    LaunchKey = aumid,
                    IconPath = icon
                };
            }
            catch
            {
                return null;
            }
        }

        private static XElement PickExecutable(XDocument doc)
        {
            List<XElement> all = doc.Descendants().Where(e => e.Name.LocalName == "Executable").ToList();
            foreach (XElement element in all)
            {
                string target = Attr(element, "TargetDevice");
                if (string.IsNullOrEmpty(target) || string.Equals(target, "PC", StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }
            return all.FirstOrDefault();
        }

        private static string Attr(XElement element, string name)
        {
            if (element == null)
            {
                return null;
            }
            XAttribute attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }

        private static string PublisherId(string publisher)
        {
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.Unicode.GetBytes(publisher));
            }

            const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
            var builder = new StringBuilder();
            int bits = 0;
            int value = 0;
            for (int i = 0; i < 8; i++)
            {
                value = (value << 8) | hash[i];
                bits += 8;
                while (bits >= 5)
                {
                    builder.Append(alphabet[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
                value &= (1 << bits) - 1;
            }
            if (bits > 0)
            {
                builder.Append(alphabet[(value << (5 - bits)) & 31]);
            }
            return builder.ToString();
        }
    }
}
