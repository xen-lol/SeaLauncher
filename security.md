# About the Code

if you're looking through the code and see something that looks suspicious, here's what it does.

### Windows Registry

Sea checks certain Windows Registry locations to help find installed games and launchers.

you might see stuff like `WOW6432Node` in the code. that's a normal part of Windows for handling certain 32-bit application registry entries on 64-bit systems. it's not malware or anything by itself.

### Game Scanning

the scanners look for games from supported platforms like Steam, Epic Games, Xbox, GOG, Ubisoft Connect, the EA app, and Battle.net. depending on the platform, this can involve checking folders, registry entries, shortcuts, or local configuration files.

### Launching Games

when you press Play, Sea starts the game or shortcut you selected. some games still need their original launcher to be running, so Sea doesn't replace Steam, Epic, or the other platforms.

### Running Programs

Sea looks at the list of running programs to know when a game you started from Sea has opened or closed. it uses that to update Discord and to start a game directly if its launcher didn't (for example if Epic didn't open it). it only compares each program's file location with the game's install folder. it doesn't read memory, windows, or keystrokes, and the list never leaves your PC.

### Discord Rich Presence

Sea talks to the Discord app running on your PC (not to any website) to show the game name and platform on your Discord profile. create `discord.txt` in `%AppData%\SeaLauncher` with the word `off` to turn it off.

### Launch Log

`launch.log` in `%AppData%\SeaLauncher` notes which launch method Sea tried for a game. it stays on your PC and is trimmed when it gets big.

### Local Files

Sea saves information like your settings and imported games locally so your library stays the way you left it.

### Just so you know

i made this file because some parts of the code might look weird if you don't know what they're for. i'd rather explain what things do than leave people guessing.

this isn't meant to be proof that every part of the app is safe. you can still look through the source yourself and see exactly what it does.