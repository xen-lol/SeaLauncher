using System;

namespace SeaLauncher.Models
{
    public enum Platform
    {
        Steam = 0,
        Epic = 1,
        Xbox = 2,
        Gog = 3,
        Ubisoft = 4,
        Ea = 5,
        BattleNet = 6,
        Imported = 7
    }

    public enum NavFilter
    {
        All,
        Favorites,
        Recent,
        Steam,
        Epic,
        Xbox,
        Gog,
        Ubisoft,
        Ea,
        BattleNet,
        Imported,
        Hidden
    }

    public enum SortMode
    {
        Name = 0,
        Recent = 1,
        Platform = 2
    }

    public class Game
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public Platform Platform { get; set; }
        public string InstallDir { get; set; }
        public string ExePath { get; set; }
        public string Arguments { get; set; }
        public string LaunchKey { get; set; }
        public string CoverPath { get; set; }
        public string IconPath { get; set; }
        public bool Favorite { get; set; }
        public bool Hidden { get; set; }
        public DateTime? LastPlayed { get; set; }
    }

    public static class PlatformInfo
    {
        public static string NameOf(Platform platform)
        {
            switch (platform)
            {
                case Platform.Steam: return "Steam";
                case Platform.Epic: return "Epic Games";
                case Platform.Xbox: return "Xbox";
                case Platform.Gog: return "GOG";
                case Platform.Ubisoft: return "Ubisoft Connect";
                case Platform.Ea: return "EA app";
                case Platform.BattleNet: return "Battle.net";
                default: return "Imported";
            }
        }
    }
}
