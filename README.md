# <img align="left" width="64" height="64" src="https://i.imgur.com/MI4zj9F.png" /> Luna
[![License: MIT](https://img.shields.io/badge/license-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

An experimental Endless Online server emulator with a focus on user-defined scripts.

## Usage
You will need to include your EMF map files in `./data/maps` (auto-generated scripts for vanilla EO maps are included by default) as well as a global script for basic EO functionality, like using emotes, talking, picking up and dropping items, and attacking NPCs.

The game server runs on port 8000 by default. You can change this in `./config/server.json`

PUB files are automatically generated on server startup from the JSON files in:`./data/items` `./data/classes` `./data/npcs` etc.

## Credits
- EOSERV# (thanks Addison!)
- BatchPub (https://github.com/eoserv/pubcompiler-php)
- Dragon's Eye Productions (MoonScript is a barebones .NET implementation of DragonSpeak.)
