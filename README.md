# ROM Semi-Unpacker and Tiled Template Creator for Lego Battles
This command line tool allows you to create custom maps for the Nintendo DS game Lego Battles. No original game assets are provided with this tool, you must provide your own legally obtained ROM.

Note that this tool is not a map editor within itself. It simply generates the files required to create the map in [Tiled](https://www.mapeditor.org/) from a valid Lego Battles ROM, and processes Tiled Files (.tmx) into .map files that can be patched into the ROM.

This tool also cannot change the logic of missions. For example, you can move the position of a trigger, or the ID it has, but you cannot determine the logic that happens when it is triggered.

## Contents
- [Notes](#Notes)
- [Commands](#Commands)
	- [`unpack`](#unpack)
	- [`create`](#create)
	- [`upgrade`](#upgrade)
	- [`import-rom`](#import-rom)
	- [`export`](#export)
	- [`export-lbz`](#export-lbz)
- [Workflow](#Workflow)
	- [Placing Markers](#Placing-Markers)
	- [Placing Entities](#Placing-Entities)
	- [Placing Mines](#Placing-Mines)
	- [Placing Trees](#Placing-Trees)
	- [Placing Detail](#Placing-Detail)
	- [Good to Know](#Good-to-Know)
- [Planned Features](#Planned-Features)
- [Known Bugs](#Known-Bugs)
- [Credits](#Credits)

## Notes
At the time of creating this tool, Tiled 1.11.0 was the latest. If the tool fails to work, downgrade Tiled to this version.

[Community Discord](https://discord.gg/pkGt3C79Af)

## Commands

Every command has the following option:
- `-s`, `--silent`: Causes the program to execute silently.

### `unpack`

Unpacks a Lego Battles ROM (not included) to extract the graphical assets. This must be done before anything else. The resulting folder is used as a "workspace", which includes multiple sub-folders and files to automate organisation.

```shell
unpack -i "LEGO Battles.nds" -o "Templates" -f
```

Options:
- `-i`, `--input`: The path to the ROM file to unpack.
- `-o`, `--output`: The desired name of the workspace folder, relative to the exe's folder. This folder name is passed in as arguments to other commands, so make sure it's clearly named.
- `-f`, `--force`: If this is given, the workspace folder will be deleted and recreated. Note that this will delete the entire folder, including any maps you may have made.

### `create`

Creates a new basic map with all the required layers and data, to be used as a template.

```shell
create -o "Templates" -n "TestMap" -t "Mars"
```

Options:
- `-w`, `--workspace`: The path to the workspace folder, relative to the exe's folder. This is what was created by unpacking.
- `-n`, `--name`: The desired name of the map.
- `-c`, `--creator`: The desired name of the creator (that's you!).
- `-t`, `--tileset`: The name of the tileset to use. "King", "Mars", or "Pirate". (k, m, and p also work).

### `upgrade`

Upgrades a map from an old version of this tool, to one that can be used. Note that this command overwrites the target file, so it is a good idea to create a backup beforehand.

```shell
upgrade -w "Templates" -i "Roundabout.tmx"
```

Options:
- `-w`, `--workspace`: The path to the workspace folder, relative to the exe's folder. This is what was created by unpacking.
- `-i`, `--input`: The path to the tmx file to upgrade. This is overwritten in-place with the upgraded file.

### `import-rom`

Imports a map from the game and creates a tmx file for it, in the Maps folder relative to the workspace folder.

```shell
import-rom -w "Templates" -i "LEGO Battles.nds" -m "ck1_1" -o "CK1_1"
```

Options:
- `-w`, `--workspace`: The path to the workspace folder, relative to the exe's folder. This is what was created by unpacking.
- `-i`, `--input`: The path to the ROM file to get the map from.
- `-m`, `--map`: The map's file name inside the rom, such as "ck1_1". Note that this isn't the name within the game itself, "The Pond" is "mp01". Use [CrystalTile](https://www.romhacking.net/utilities/818/) or similar to see the map names.
- `-o`, `--output`: The desired name of the map's output files. This creates the main map in the Maps folder, and any extra tileset data in the TileBlueprints folder.

### `export`

Exports a Tiled tmx map and creates multiple files that can be patched into the game ROM using [CrystalTile](https://www.romhacking.net/utilities/818/) or similar.

```shell
export -w "Templates" -i "CK1_1" -o "ck1_1"
```

Options:
- `-w`, `--workspace`: The path to the workspace folder, relative to the exe's folder. This is what was created by unpacking.
- `-i`, `--input`: The name of the map file to process. The tool looks in the Maps folder within the workspace, so only the map name is needed.
- `-o`, `--output`: The desired name of the map's output files. These will be placed into the Output folder within the workspace.
- `-c`, `--skip-compression`: If this is given, the compression stage will be skipped. This file will not be directly usable in the game and will cause a crash.

### `export-lbz`

Exports a Tiled tmx map and creates a single file that can be used for online play.

```shell
export-lbz -w "Templates" -i "CK1_1" -o "ck1_1"
```

Options:
- `-w`, `--workspace`: The path to the workspace folder, relative to the exe's folder. This is what was created by unpacking.
- `-i`, `--input`: The name of the map file to process. The tool looks in the Maps folder within the workspace, so only the map name is needed.
- `-o`, `--output`: The desired name of the map's output file. This will be placed into the Output folder within the workspace.

## Workflow
To start with, you should run the unpacking process.

Within the templates folder there is a .tiled-project file which contains useful types and other data that you will need, make sure you open it with Tiled.

The templates folder will contain an example map, you can rename this file and use it as a map template if you wish. Tiled allows you to resize maps, although a maximum size of 255 should be observed. Within this map file exists multiple layers:

- Triggers: Contains trigger areas that missions use to trigger specific logic.
- Camera Bounds: Contains areas that limit where the camera is allowed to see in missions.
- Patrol Points: Contains paths that entities in the same event ID/sort key group follow.
- Walls: Contains walls that spawn on game start.
- Markers: Contains entities that define bridge spots and tell the AI about the map's features.
- Mines: Contains rectangles that signify where mines are located.
- Entities: Contains buildings and units. Note that entities are linked to events, so just because an entity is visible on an imported map, does not mean it spawns when the map is loaded.
- Pickups: Contains any pickups, such as golden bricks.
- Trees: A tile layer for placing down trees on the map.
- Details: The main layer for the detail of the map, which includes the grass, water, stone, and mountains.

Failure to include all of these layers in the map may cause the tool to not function as expected.

The map properties (Map>Properties in the toolbar to open it in the properties window) includes these properties:
- Name: This is used for online play primarily, and will one day change the name of the map in-game too.
- Tileset: This allows you to change what type of tileset is being used. Note that this won't actually change the tileset within Tiled, but tells Lego Battles which tileset you're using.
- ToolVersion: This is the version of the tool that was used to generate the map. Do not change this.
- ReplacesMPIndex: This is used for online play, and tells the packer tool which freeplay map to replace.

When you are done editing the map, you can run the tool on the map file in processing mode to create the .map file. You can then use a tool such as [CrystalTile](https://www.romhacking.net/utilities/818/) to import the map into the game. You must replace an existing freeplay map (named like "mp01.map"). You can then load up the game, select the replaced map, and it will instead load your custom map. There are also 4 minimap files that are generated, replace the files in the ROM with these files the same way as the main map. Finally, there is the detail tiles file, which replaces the file of the same name within the BP folder in the rom.

### Placing Markers
- Markers are not too useful for multiplayer, other than bridges, but ensure AI opponents can function.
- Use the marker class. Almost all markers can just be points, but you can use rectangles for bridges purely for ease of use.
- If you intend your map to be played versus AI, markers are important, so use a decent amount of them.
- Place markers over groups of trees or other points of interest, where the AI can find resources.
- For bridges, the game does not ensure the tiles on the shore are correct, you must manually draw these tiles onto the detail layer. The tiles between the two "posts" is a shore texture, and the "post" tiles go on either side of the bridge (they should not be directly within the bridge's rectangle).
- Ensure bridges are either 3, 6, or 9 tiles long. Not following this rule will cause bridges to either be too short, or run aground. This also breaks the AI, and can cause builders to become stuck.

Markers have IDs, this ID tells the game what the marker is used for. These are the current ones we know:
- 0: The AI sends builders here to look for trees.
- 3: The AI seems to build around here.
- 6: The AI seems to use this area to build shipyards.
- 7: Horizontal bridge.
- 8: Vertical bridge.

A lot of markers past this seem to just be arbitrary and mission-specific. If you think you know what a marker ID does and it's not listed here, please get in touch.

### Placing Entities
- For units and gold bricks, create a Point entity. With it selected, set the Class to "entity" in the properties window. The type can then be set to determine what type of unit it is, or if it is a gold brick. The team index can also be set from 0 to 3 to determine which team it belongs to.
- For buildings, create a Rectangle entity. Set the class as with units.
- Try to make sure rectangular entities are snapped nicely to the grid so that the edges follow the lines of the grid, and that point entities sit with the very tip of the point in the centre of the tile. This is not super important, but ensures positions don't get rounded into the adjacent tiles.
- Ensure each team has a base, farm, barracks, two builders, and a hero. Not all of these units will be used, some will only be used when the "pre-built bases" setting is turned on in-game.

Entities have a sub-index. This is only really used for pickups. These are the current ones we know:
- 5: Red Brick
- 6: Minikit
- 7: Blue Stud
- 8: Golden Brick
- Other: Mission Pickup

Note that placing pickups in maps where they don't belong will break the game. (e.g. blue studs in a freeplay map).

### Placing Mines
- Use rectangles. No class is needed, and no special data can be saved.
- The game does not ensure that the tiles under the mine are correct, you must manually draw these tiles onto the detail layer.

### Placing Trees
- The type of tile placed does not matter, any tile placed will count as a tree, and a lack of tile will count as a lack of tree. The example map uses the "middle" tree tile for simplicity, but really any tile can be used.

### Placing Detail
- The main graphical data of the map is the detail layer. This forms the "base" layer of the map.
- The default tileset includes terrain sets for grass, stone, water, and mountains. Use the terrain brush to create large amounts of detail, and it will automatically smooth out the transition between different tile types (for the most part).
- The data is automatically calculated so that you cannot build on stone, water, or mountain tiles.
- Try to avoid using any "weird" tiles, like the bridges, trees, fog, or glitched ones. These may display, but could possibly cause instability.

### Good to Know
- Rectangular entities (bridges, mines, buildings, etc.) use the top-left corner as the position.
- Point entities (units, gold bricks, markers, etc.) use the centre as the position.
- The smallest size of trees that will not display as stumps is 2x2.
- Making smart use of hiding layers will ensure you do not accidentally draw or create objects on the wrong layers.

## Planned Features
- Map names. It is currently not decided how this will be done, as it requires the localisation files of the game to be edited.
- Integrated ROM patching. Ideally this would circumvent any map size mismatch issues, as well as remove a step in the workflow.
- Automatic tileset, rather than a map property.

## Known Bugs
- Replacing a freeplay map will use the minimap colour palette of the original map. e.g. mp01 (The Pond) will use the King minimap palette on the map select screen.

## Credits
- CUE as always for the LZX compression tools.
- Garhoogin for working out the Lego compression.
- Obviously Tiled, for their flexible tilemap editor.
- The Lego Battles community for actually using these tools and helping in development.
