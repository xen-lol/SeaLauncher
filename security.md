# About the Code

if you're looking through the code and see something that looks suspicious, here's what it does.

### Windows Registry

Sea checks certain Windows Registry locations to help find installed games and launchers.

you might see stuff like `WOW6432Node` in the code. that's a normal part of Windows for handling certain 32-bit application registry entries on 64-bit systems. it's not malware or anything by itself.

### Game Scanning

the scanners look for games from supported platforms like Steam, Epic Games, Xbox, and GOG. depending on the platform, this can involve checking folders, registry entries, shortcuts, or local configuration files.

### Launching Games

when you press Play, Sea starts the game or shortcut you selected. some games still need their original launcher to be running, so Sea doesn't replace Steam, Epic, or the other platforms.

### Local Files

Sea saves information like your settings and imported games locally so your library stays the way you left it.

### Just so you know

i made this file because some parts of the code might look weird if you don't know what they're for. i'd rather explain what things do than leave people guessing.

this isn't meant to be proof that every part of the app is safe. you can still look through the source yourself and see exactly what it does.