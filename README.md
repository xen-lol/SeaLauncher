# Sea Launcher

Sea Launcher is a Windows game launcher built with C#, WPF, and .NET 8. It brings your games together in one library so you can find and launch them without jumping between different launchers.
Yes, this has some vibe-coded parts, but everything is mainly made by me, i used AI to help explain and change things up, its mostly made by me with the **help** of AI, i am a new coder and still am learning so that is why I used AI in some parts.
## Features

- Detect installed games from Steam, Epic Games, Xbox / PC Game Pass, GOG, Ubisoft Connect, the EA app, and Battle.net.
- Import your own games using an executable or shortcut.
- Launch games directly from their cards.
- Search, sort, favorite, hide, and view recently played games.
- Rename games, change cover art, open install folders, and remove imported games.
- A moving sea background.
- Discord Rich Presence: show what you are playing on your Discord profile (optional, see below).

Game detection can depend on the platform and where a game is installed. If a game doesn't show up, try **Rescan library** and check its installation location.

## Requirements

- Windows 10 or Windows 11
- .NET 8
- Visual Studio 2022 with the **.NET desktop development** workload, or the .NET CLI

## Running from source

Want to build Sea Launcher yourself or make your own version? The source code is available to explore and modify, subject to the terms in `LICENSE`.

1. Open `SeaLauncher.csproj` in Visual Studio.
2. Press **F5** to build and run.

Or run these commands in a terminal from the project folder:

```powershell
dotnet restore
dotnet run
```

If you plan to publish or distribute your own version, please DM me on Discord at **@.runtimebroker** to discuss permission first.

## Where your data is stored

Sea Launcher stores local data under `%AppData%\SeaLauncher`:

- `library.json` — your library entries and preferences
- `covers\` — custom cover images
- `error.log` — unexpected UI errors
- `launch.log` — which launch method Sea tried for each game, handy if a game won't start
- `discord.txt` — optional: `off` to turn Rich Presence off, or your own Discord application ID

## Discord Rich Presence

Sea shows **Playing <game>** on your Discord profile, and **Browsing the library** while you are in the launcher. It needs the Discord app running on the same PC.

- To turn it off, create `%AppData%\SeaLauncher\discord.txt` containing the word `off`.
- To use your own Discord application, put its **Application ID** (just the number) in that same file.
- Optional: in the Discord Developer Portal, under Rich Presence > Art Assets, upload an image named `sea` for the big picture.

The application's name in the portal is what Discord shows after "Playing".

## Project structure

- `MainWindow.cs` — main window and UI
- `Models/` — game data
- `Services/` — game detection, library storage, and launching
- `Ui/` — reusable controls and visual helpers
- `Assets/` — application icon

## Contributing

Found a bug or have an idea? Feel free to open an issue or submit a pull request.

For bug reports, include what happened, where it happened, and the steps needed to reproduce the problem.

## Names and logos

Sea Launcher is an independent project and is not affiliated with or endorsed by Valve, Epic Games, Microsoft, GOG, Ubisoft, Electronic Arts, or Blizzard. Their respective names and logos belong to their respective owners. See `NOTICE.txt` for credits and trademark information.

## License

Please read `LICENSE` before using, modifying, distributing, or publishing Sea Launcher or a version based on it.

## this launcher was made fully by xen.

## Epic Games launch fix

Epic games should be launched through the Epic Games Launcher protocol so that launcher authentication, anti-cheat, and entitlement checks can run. Sea Launcher now attempts to recover the game's catalog identifier from Epic's installed `.item` manifests before launching. If Fortnite's manifest cannot be found, it uses Fortnite's canonical Epic protocol identifier. It no longer falls back to starting `FortniteBootstrapper.exe` directly.

After extracting this project, open `SeaLauncher.slnx` in Visual Studio, allow NuGet restore to finish, then use **Build > Rebuild Solution** and run the rebuilt project. The old executable in a previous `bin` folder will not contain these source changes.
