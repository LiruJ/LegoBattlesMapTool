using System.Xml;

namespace TiledToLB.Tilemap
{
    internal class EntityData
    {
        #region Properties
        public byte X { get; }

        public byte Y { get; }

        public byte TeamIndex { get; }

        public EntityType Type { get; }

        public byte HealthPercent { get; }
        #endregion

        #region Constructors
        public EntityData()
        {
        }

        public EntityData(byte x, byte y, byte teamIndex, EntityType type, byte healthPercent)
        {
            X = x;
            Y = y;
            TeamIndex = teamIndex;
            Type = type;
            HealthPercent = healthPercent;
        }
        #endregion

        #region Load Functions
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

            return new(x, y, teamIndex, entityType, healthPercent);
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
                mapWriter.Write((byte)8);
                mapWriter.Write(byte.MaxValue);
                mapWriter.Write((byte)2);
                mapWriter.Write((byte)1);
            }
            else
            {
                mapWriter.Write(TeamIndex);
                mapWriter.Write((byte)Type);
                mapWriter.Write((byte)0);
                mapWriter.Write(HealthPercent);
                mapWriter.Write((byte)1);
                mapWriter.Write((byte)1);
                mapWriter.Write((byte)1);
            }
            
        }
        #endregion
    }
}
