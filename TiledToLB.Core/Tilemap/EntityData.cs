using GlobalShared.DataTypes;
using System.Xml;
using TiledToLB.Core.Tiled;
using TiledToLB.Core.Tiled.Map;

namespace TiledToLB.Core.Tilemap
{
    public class EntityData
    {
        #region Properties
        public byte EventID { get; }

        public byte X { get; }

        public byte Y { get; }

        public byte TeamIndex { get; }

        public EntityType Type { get; }

        public byte SubType { get; }

        public byte HealthPercent { get; }
        #endregion

        #region Constructors
        public EntityData()
        {
        }

        public EntityData(byte eventID, byte x, byte y, byte teamIndex, EntityType type, byte subType, byte healthPercent)
        {
            EventID = eventID;
            X = x;
            Y = y;
            TeamIndex = teamIndex;
            Type = type;
            HealthPercent = healthPercent;
        }
        #endregion

        #region Load Functions
        public static EntityData LoadFromTiledMapObject(TiledMapObject mapObject)
        {
            byte x = (byte)MathF.Floor(mapObject.X / 24);
            byte y = (byte)MathF.Floor(mapObject.Y / 16);

            byte eventID = mapObject.Properties.TryGetValue("EventID", out TiledProperty eventIDProperty) && byte.TryParse(eventIDProperty.Value, out byte result) 
                ? result 
                : (byte)0;

            byte teamIndex = mapObject.Properties.TryGetValue("TeamIndex", out TiledProperty teamIndexProperty) && byte.TryParse(teamIndexProperty.Value, out  result)
                ? result
                : (byte)0;

            EntityType entityType = mapObject.Properties.TryGetValue("Type", out TiledProperty typeProperty) && byte.TryParse(typeProperty.Value, out result)
                ? (EntityType)result
                : EntityType.Hero;

            byte subType = mapObject.Properties.TryGetValue("SubType", out TiledProperty subTypeProperty) && byte.TryParse(subTypeProperty.Value, out result)
                 ? result
                 : (byte)8;

            byte healthPercent = mapObject.Properties.TryGetValue("HealthPercent", out TiledProperty healthProperty) && float.TryParse(healthProperty.Value, out float floatResult)
            ? (byte)MathF.Min(MathF.Max(floatResult * 100, 0), 100)
            : (byte)100;

            return new(eventID, x, y, teamIndex, entityType, subType, healthPercent);
        }


        public static EntityData LoadFromTiledNode(XmlNode entityNode)
        {
            byte x = float.TryParse(entityNode.Attributes?["x"]?.Value, out float xValue) ? (byte)MathF.Floor(xValue / 24f) : throw new Exception("Entity has missing x position!");
            byte y = float.TryParse(entityNode.Attributes?["y"]?.Value, out float yValue) ? (byte)MathF.Floor(yValue / 16f) : throw new Exception("Entity has missing y position!");


            XmlNode? teamIndexNode = entityNode.SelectSingleNode("properties/property[@name='TeamIndex']");
            byte teamIndex = byte.TryParse(teamIndexNode?.Attributes?["value"]?.Value, out teamIndex) ? teamIndex : (byte)0;

            XmlNode? typeNode = entityNode.SelectSingleNode("properties/property[@name='Type']");
            EntityType entityType = byte.TryParse(typeNode?.Attributes?["value"]?.Value, out byte entityTypeValue) ? (EntityType)entityTypeValue : EntityType.Hero;

            XmlNode? healthNode = entityNode.SelectSingleNode("properties/property[@name='HealthPercent']");
            byte healthPercent = float.TryParse(healthNode?.Attributes?["value"]?.Value, out float healthPercentValue) ? (byte)MathF.Min(MathF.Max(healthPercentValue * 100, 0), 100) : (byte)100;

            return new(0, x, y, teamIndex, entityType, (byte)(entityType == EntityType.Pickup ? 8 : 0), healthPercent);
        }
        #endregion

        #region Save Functions
        public void SaveToWriter(BinaryWriter mapWriter)
        {
            mapWriter.Write(X);
            mapWriter.Write(Y);

            if (Type == EntityType.Pickup)
            {
                mapWriter.Write((byte)0);
                mapWriter.Write((byte)Type);
                mapWriter.Write(SubType);
                mapWriter.Write(byte.MaxValue);
                mapWriter.Write((byte)2);
                mapWriter.Write((byte)1);
            }
            else
            {
                mapWriter.Write(TeamIndex);
                mapWriter.Write((byte)Type);
                mapWriter.Write(SubType);
                mapWriter.Write(HealthPercent);
                mapWriter.Write((byte)1);
                mapWriter.Write((byte)1);
                mapWriter.Write((byte)1);
            }

        }
        #endregion
    }
}
