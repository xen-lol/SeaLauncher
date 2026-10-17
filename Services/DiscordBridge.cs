using System;
using System.Runtime.CompilerServices;
using DiscordRPC;

namespace SeaLauncher.Services
{
    internal static class DiscordBridge
    {
        private static DiscordRpcClient client;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Connect(string applicationId)
        {
            client = new DiscordRpcClient(applicationId);
            client.Initialize();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Show(string details, string state, DateTime since)
        {
            if (client == null)
            {
                return;
            }

            var presence = new RichPresence();
            presence.Details = details;
            presence.State = state;
            presence.Timestamps = new Timestamps(since);
            presence.Assets = new Assets
            {
                LargeImageKey = "sea",
                LargeImageText = "Sea Launcher"
            };
            client.SetPresence(presence);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Close()
        {
            if (client == null)
            {
                return;
            }

            client.ClearPresence();
            client.Dispose();
            client = null;
        }
    }
}
