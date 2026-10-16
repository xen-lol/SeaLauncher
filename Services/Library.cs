using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SeaLauncher.Services
{
    public class ImportedEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ExePath { get; set; }
        public string Arguments { get; set; }
    }

    public class LibraryState
    {
        public List<ImportedEntry> Imported { get; set; } = new List<ImportedEntry>();
        public List<string> Favorites { get; set; } = new List<string>();
        public List<string> Hidden { get; set; } = new List<string>();
        public Dictionary<string, string> Names { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Covers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, DateTime> LastPlayed { get; set; } = new Dictionary<string, DateTime>();
        public int Sort { get; set; }
    }

    public static class Library
    {
        private static readonly object Gate = new object();

        public static string DataDir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SeaLauncher");
            }
        }

        public static string CoverDir
        {
            get { return Path.Combine(DataDir, "covers"); }
        }

        private static string FilePath
        {
            get { return Path.Combine(DataDir, "library.json"); }
        }

        public static LibraryState Load()
        {
            LibraryState state = null;
            try
            {
                if (File.Exists(FilePath))
                {
                    state = JsonSerializer.Deserialize<LibraryState>(FileUtil.ReadText(FilePath));
                }
            }
            catch
            {
                state = null;
            }

            if (state == null)
            {
                state = new LibraryState();
            }
            if (state.Imported == null) state.Imported = new List<ImportedEntry>();
            if (state.Favorites == null) state.Favorites = new List<string>();
            if (state.Hidden == null) state.Hidden = new List<string>();
            if (state.Names == null) state.Names = new Dictionary<string, string>();
            if (state.Covers == null) state.Covers = new Dictionary<string, string>();
            if (state.LastPlayed == null) state.LastPlayed = new Dictionary<string, DateTime>();
            return state;
        }

        public static void Save(LibraryState state)
        {
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(DataDir);
                    string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                    string temp = FilePath + ".tmp";
                    File.WriteAllText(temp, json);
                    File.Move(temp, FilePath, true);
                }
                catch
                {
                }
            }
        }
    }
}
