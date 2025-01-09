using ContentUnpacker.DataTypes;
using ContentUnpacker.Tilemaps;
using GlobalShared.DataTypes;
using GlobalShared.Tilemaps;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Property;

namespace TiledToLB.Core.LegoBattles
{
    public static class Helpers
    {
        #region Name Functions
        public static string? CalculateName(TilemapEntityData tilemapEntity)
            => CalculateName(tilemapEntity.TypeIndex, tilemapEntity.SubTypeIndex);

        public static string? CalculateName(TiledMapObject mapObject)
        {
            EntityType entityType = mapObject.GetEntityType(EntityType.Hero);

            if (!mapObject.Properties.TryGetValue("SubType", out TiledProperty subTypeProperty) || !int.TryParse(subTypeProperty.Value, out int subType))
                subType = 0;

            return CalculateName(entityType, subType);
        }

        public static string? CalculateName(EntityType entityType, int subType) => entityType switch
        {
            EntityType.Hero or EntityType.Builder or EntityType.Melee or
            EntityType.Ranged or EntityType.Mounted or EntityType.Transport or
            EntityType.Special or EntityType.Base or EntityType.Harvester or
            EntityType.Mine or EntityType.Farm or EntityType.Barracks or
            EntityType.SpecialFactory or EntityType.Tower or EntityType.Tower2 or
            EntityType.Tower3 or EntityType.Shipyard => entityType.ToString(),

            EntityType.Bridge or EntityType.Gate or EntityType.Wall => null,
            EntityType.Pickup => subType switch
            {
                5 => "Red Brick",
                6 => "Minikit",
                7 => "Blue Stud",
                8 => "Golden Brick",
                _ => "Mission Pickup",
            },
            _ => null,
        };
        #endregion

        #region Property Functions
        public static void SetSortKey(this TiledMapObject mapObject, int sortKey)
            => mapObject.Properties.Set("SortKey", sortKey);

        public static void SetEventIDAndSortKey(this TiledMapObject eventObject, int eventId, int sortKey)
        {
            eventObject.Properties.Set("EventID", eventId);
            eventObject.SetSortKey(sortKey);
        }

        public static void SetTriggerIDAndSortKey(this TiledMapObject eventObject, int triggerId, int sortKey)
        {
            eventObject.Properties.Set("TriggerID", triggerId);
            eventObject.SetSortKey(sortKey);
        }

        public static void SetMarkerIDAndSortKey(this TiledMapObject eventObject, int markerId, int sortKey)
        {
            eventObject.Properties.Set("MarkerID", markerId);
            eventObject.SetSortKey(sortKey);
        }

        public static void SetTeamIndex(this TiledMapObject eventObject, int teamIndex)
            => eventObject.Properties.Set("TeamIndex", teamIndex);

        public static void SetEntityType(this TiledMapObject entityObject, EntityType entityType, int subType)
        {
            entityObject.SetEntityType(entityType);
            entityObject.Properties.Set("SubType", subType);
        }

        public static void SetEntityType(this TiledMapObject entityObject, EntityType entityType)
            => entityObject.Properties.Set(new TiledProperty("Type", ((int)entityType).ToString(), TiledPropertyType.Int, "EntityType"));

        public static void SetExtraData(this TiledMapObject entityObject, params byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                entityObject.Properties.Set($"ExtraData{i}", data[i]);
        }

        public static EntityType GetEntityType(this TiledMapObject entityObject, EntityType defaultTo) => entityObject.Properties.GetEntityType(defaultTo);

        public static EntityType GetEntityType(this TiledPropertyCollection properties, EntityType defaultTo)
            => properties.TryGetValue("Type", out TiledProperty typeProperty) && int.TryParse(typeProperty.Value, out int typeValue)
                ? (EntityType)typeValue
                : defaultTo;
        #endregion

        #region Entity Functions
        public static TiledMapObject CreateEntityFrom(TilemapEntityData entityData, TiledMap tiledMap, TiledMapObjectGroup group, int eventID, int sortKey)
        {
            TiledMapObject entityObject = tiledMap.CreateObject(group);
            entityObject.SetEventIDAndSortKey(eventID, sortKey);
            entityObject.Type = "Entity";
            entityObject.Name = CalculateName(entityData);

            // Calculate entity position and size.
            entityObject.SetPositionTopLeftPoint(entityData.X, entityData.Y);
            (int offsetX, int offsetY, int width, int height) = CalculateOffsetAndSize(entityData);
            entityObject.X += offsetX;
            entityObject.Y += offsetY;
            entityObject.Width = width;
            entityObject.Height = height;

            entityObject.SetEntityType(entityData.TypeIndex, entityData.SubTypeIndex);
            entityObject.SetExtraData(entityData.ExtraData);

            return entityObject;
        }

        public static (int offsetX, int offsetY, int width, int height) CalculateOffsetAndSize(TilemapEntityData tilemapEntity)
            => CalculateOffsetAndSize(tilemapEntity.TypeIndex);

        public static (int offsetX, int offsetY, int width, int height) CalculateOffsetAndSize(TiledMapObject entityObject)
        {
            EntityType entityType = entityObject.GetEntityType(EntityType.Hero);
            return CalculateOffsetAndSize(entityType);
        }

        public static (int offsetX, int offsetY, int width, int height) CalculateOffsetAndSize(EntityType entityType) => entityType switch
        {
            EntityType.Hero or EntityType.Builder or EntityType.Melee or
            EntityType.Ranged or EntityType.Mounted or EntityType.Transport or
            EntityType.Special => (12, 8, 0, 0),

            EntityType.Base => (0, 0, 3 * 24, 3 * 16),

            EntityType.Harvester or EntityType.Mine or EntityType.Farm or
            EntityType.Barracks or EntityType.SpecialFactory or EntityType.Shipyard => (0, 0, 2 * 24, 2 * 16),

            EntityType.Bridge => (0, 0, 0, 0),

            EntityType.Gate or EntityType.Wall or EntityType.Tower or
            EntityType.Tower2 or EntityType.Tower3 => (12, 8, 24, 16),

            EntityType.Pickup => (12, 8, 0, 0),
            _ => (0, 0, 0, 0)
        };

        public static (int width, int height) CalculateBridgeSize(LegoTilemap legoMap, bool isHorizontal, int bridgePositionX, int bridgePositionY)
        {
            int width, height;

            // Horizontal bridges.
            if (isHorizontal)
            {
                width = 0;
                height = 2 * 16;

                int x = bridgePositionX;
                while (x < legoMap.Width && legoMap.TileData[(bridgePositionY * legoMap.Width) + x].TileType == TileType.Water)
                {
                    width += 24;
                    x++;
                }
            }
            // Vertical bridges.
            else
            {
                width = 2 * 24;
                height = 0;

                int y = bridgePositionY;
                while (y < legoMap.Height && legoMap.TileData[(y * legoMap.Width) + bridgePositionX].TileType == TileType.Water)
                {
                    height += 16;
                    y++;
                }
            }
            return (width, height);
        }

        public static void SetSizeFromTiles(this TiledMapObject entityObject, int widthInTiles, int heightInTiles)
        {
            entityObject.Width = widthInTiles * 24f;
            entityObject.Height = heightInTiles * 16f;
        }

        public static void SetPositionTopLeftPoint(this TiledMapObject entityObject, Vector2U8 position)
            => entityObject.SetPositionTopLeftPoint(position.X, position.Y);

        public static void SetPositionTopLeftPoint(this TiledMapObject entityObject, int x, int y)
        {
            entityObject.X = x * 24f;
            entityObject.Y = y * 16f;
        }

        public static void SetPositionCentredPoint(this TiledMapObject entityObject, Vector2U8 position)
            => entityObject.SetPositionCentredPoint(position.X, position.Y);

        public static void SetPositionCentredPoint(this TiledMapObject entityObject, int x, int y)
        {
            entityObject.X = (x * 24f) + 12f;
            entityObject.Y = (y * 16f) + 8f;
        }

        public static void SetPositionAndSizeFromRectU8(this TiledMapObject entityObject, RectU8 rect)
        {
            entityObject.X = rect.MinX * 24f;
            entityObject.Y = rect.MinY * 16f;
            entityObject.Width = ((rect.MaxX + 1) - rect.MinX) * 24f;
            entityObject.Height = ((rect.MaxY + 1) - rect.MinY) * 16f;
        }
        #endregion
    }
}
