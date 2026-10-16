using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SeaLauncher.Services
{
    public static class FileUtil
    {
        public static string ReadText(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        public static string ShortHash(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder();
                for (int i = 0; i < 6; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string SafeName(string text)
        {
            var builder = new StringBuilder();
            foreach (char c in text)
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }
            return builder.ToString();
        }
    }
}
