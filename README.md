# ROM Semi-Unpacker and Tiled Template Creator for Lego Battles
This command line tool allows you to create custom maps for the Nintendo DS game Lego Battles. No original game assets are provided with this tool, you must provide your own legally obtained ROM.

Note that this tool is not a map editor within itself. It simply generates the files required to create the map in [Tiled](https://www.mapeditor.org/) from a valid Lego Battles ROM, and processes Tiled Files (.tmx) into .map files that can be patched into the ROM.

## Notes
At the time of creating this tool, Tiled 1.11.0 was the latest. If the tool fails to work, downgrade Tiled to this version.

This tool is only intended for creating freeplay maps. There is no functionality for creating campaign maps, as the tool cannot export triggers or handle the complex mission system of Lego Battles. This may be added in future releases.

Currently, only the king tileset is supported. It takes time to create the tileset data within Tiled, but it is planned to add the Mars and Pirate tilesets in future updates.
## Usage
This tool has two modes. The first is the unpacking mode, which extracts and processes assets from the Lego Battles ROM file and creates a folder with both the extracted assets and some basic Tiled files. The second mode is the processor, which takes a Tiled File (.tmx) and generates a .map file that can be patched into the ROM. Read on for more detailed instructions for each mode.

## Unpacking
As previously mentioned, this mode unpacks data from the ROM and creates a template that can be used to create maps. This mode expects two inputs:
* -r or --rom: The path to the Lego Battles ROM file, including file extension.
* -t or --template: The path into which the extracted files and template files are placed. If this directory already exists, the files will be placed into it and replace anything already existing. If the directory does not exist, it will be created first.

### Example
``TiledToLB.exe -r "LegoBattles.nds" -t "Templates"``
Running this in the command prompt (within the folder containing the executable) will read the "LegoBattles.nds" ROM within the same folder, and produce in that same folder a "Templates" folder, which will contain the extracted files and template files.

## Processing
Processing processes a Tiled map file (.tmx) into a .map file for the game. This mode expects two inputs:
* -i or --input: The path to the Tiled file (.tmx), including file extension.
* -o or --output: The path to the desired output .map file, including file extension.

### Example
``TiledToLB.exe -i "MyTiledMap.tmx" "mp22.map``
Running this in the command prompt (within the folder containing the executable) will read the "MyTiledMap.tmx" file (and any referenced files, such as the tileset) within the same folder, and produce in that same folder the processed file "mp22.map".

## Workflow
To start with, you should run the unpacking process. You should copy and paste the resulting template folder, or rename it, to prevent your work from being overwritten by subsequent unpacking operations.

Within the templates folder there is a .tiled-project file which contains useful types and other data that you will need, make sure you open it with Tiled.

The templates folder will contain an example map, you can rename this file and use it as a map template if you wish. Tiled allows you to resize maps, although a maximum size of 255 should be observed. Within this map file exists multiple layers:
* Markers: Contains entities that tell the AI where they can harvest trees.
* Mines: Contains rectangles that signify where mines are located.
* Entities: Contains buildings and units that spawn on game start, as well as golden bricks.
* Bridges: Contains rectangles signifying where bridges should be buildable.
* Trees: A tile layer for placing down trees on the map.
* Details: The main layer for the detail of the map, which includes the grass, water, stone, and mountains.

Failure to include all of these layers in the map may cause the tool to not function as expected. It is therefore suggested that the example map is used as a template.

The map properties (Map>Properties in the toolbar to open it in the properties window) includes two unused custom properties, that are planned for future features:
* Name: This will be used to replace the localised map name in the game.
* Tileset: This enum will allow you to change what type of tileset is being used.

When you are done editing the map, you can run the tool on the map file in processing mode to create the .map file. You can then use a tool such as [CrystalTile](https://www.romhacking.net/utilities/818/) to import the map into the game. You must replace an existing freeplay map (named like "mp01.map"). You can then load up the game, select the replaced map, and it will instead load your custom map. There are also 4 minimap files that are generated, replace the files in the ROM with these files the same way as the main map.

### Placing Markers
* Markers are not too useful for multiplayer, but ensure AI opponents can function.
* Use regular point entities, there is no special class.
* If you intend your map to be played versus AI, markers are important, so use a decent amount of them.
* Place markers over groups of trees or other points of interest, where the AI can find resources.
### Placing Entities
* For units and gold bricks, create a Point entity. With it selected, set the Class to "entity" in the properties window. The type can then be set to determine what type of unit it is, or if it is a gold brick. The team index can also be set from 0 to 3 to determine which team it belongs to.
* For buildings, create a Rectangle entity. Set the class as with units.
* Try to make sure rectangular entities are snapped nicely to the grid so that the edges follow the lines of the grid, and that point entities sit with the very tip of the point in the centre of the tile. This is not super important, but ensures positions don't get rounded into the adjacent tiles.
* Ensure each team has a base, farm, barracks, two builders, and a hero. Not all of these units will be used, some will only be used when the "pre-built bases" setting is turned on in-game.
### Placing Mines
* Use rectangles. No class is needed, and no special data can be saved.
* The game does not ensure that the tiles under the mine are correct, you must manually draw these tiles onto the detail layer.
### Placing Bridges
* Use rectangles with the Bridge class. There will be a custom property IsHorizontal that you can set per bridge.
* Ensure bridges are either 3, 6, or 9 tiles long. Not following this rule will cause bridges to either be too short, or run aground. This also breaks the AI, and can cause builders to become stuck.
* The game does not ensure the tiles on the shore are correct, you must manually draw these tiles onto the detail layer. The tiles between the two "posts" is a shore texture, and the "post" tiles go on either side of the bridge (they should not be directly within the bridge's rectangle).
* There is no current way to spawn a bridge pre-built.
### Placing Trees
* The type of tile placed does not matter, any tile placed will count as a tree, and a lack of tile will count as a lack of tree. The example map uses the "middle" tree tile for simplicity, but really any tile can be used.
### Placing Detail
* The main graphical data of the map is the detail layer. This forms the "base" layer of the map.
* The default tileset includes terrain sets for grass, stone, water, and mountains. Use the terrain brush to create large amounts of detail, and it will automatically smooth out the transition between different tile types (for the most part).
* The data is automatically calculated so that you cannot build on stone, water, or mountain tiles.
* Try to avoid using any "weird" tiles, like the bridges, trees, fog, or glitched ones. These may display, but could possibly cause instability.
### Good to Know
* Rectangular entities (bridges, mines, buildings, etc.) use the top-left corner as the position.
* Point entities (units, gold bricks, markers, etc.) use the centre as the position.
* Bridges must be 3, 6, or 9 tiles long.
* The smallest size of trees that will not display as stumps is 2x2.
* Making smart use of hiding layers will ensure you do not accidentally draw or create objects on the wrong layers.
## Known Bugs
* Trees currently appear glitched in-game. It is unknown why this happens.
## Planned Features
* Map names. It is currently not decided how this will be done, as it requires the localisation files of the game to be edited.
* The other two tilesets.
* Integrated ROM patching. Ideally this would circumvent any map size mismatch issues, as well as remove a step in the workflow.
## Credits
* CUE as always for the LZX compression tools.
* Garhoogin for working out the Lego compression.
* Obviously Tiled, for their flexible tilemap editor.
* The Lego Battles community for actually using these tools and helping in development.
