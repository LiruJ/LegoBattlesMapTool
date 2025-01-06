using TiledToLB.Core.Tiled.Map;

namespace TiledToLB.Core.LegoBattles.DataStructures
{
    internal class SortKeyEventLayer
    {
        #region Properties
        public int SortKey { get; }

        public IEnumerable<byte> AllEventIDs { get; }

        public List<TiledMapObject>? PatrolPoints { get; }

        public IDictionary<byte, List<TiledMapObject>>? PatrolPointsByEventID { get; }

        public List<TiledMapObject>? CameraBounds { get; }

        public IDictionary<byte, List<TiledMapObject>>? CameraBoundsByEventID { get; }

        public List<TiledMapObject>? Entities { get; }

        public IDictionary<byte, List<TiledMapObject>>? EntitiesByEventID { get; }

        public List<TiledMapObject>? Pickups { get; }

        public IDictionary<byte, List<TiledMapObject>>? PickupsByEventID { get; }

        public List<TiledMapObject>? Walls { get; }

        public IDictionary<byte, List<TiledMapObject>>? WallsByEventID { get; }
        #endregion

        #region Constructors
        public SortKeyEventLayer(int sortKey, List<TiledMapObject>? patrolPoints, List<TiledMapObject>? cameraBounds, List<TiledMapObject>? entities, List<TiledMapObject>? pickups, List<TiledMapObject>? walls)
        {
            SortKey = sortKey;
            PatrolPoints = patrolPoints;
            CameraBounds = cameraBounds;
            Pickups = pickups;
            Entities = entities;
            Walls = walls;

            PatrolPointsByEventID = groupLayerByEventID(PatrolPoints);
            CameraBoundsByEventID = groupLayerByEventID(CameraBounds);
            PickupsByEventID = groupLayerByEventID(Pickups);
            EntitiesByEventID = groupLayerByEventID(Entities);
            WallsByEventID = groupLayerByEventID(Walls);

            AllEventIDs = EventLayers.getAllKeys(PatrolPointsByEventID?.Keys, CameraBoundsByEventID?.Keys, PickupsByEventID?.Keys, EntitiesByEventID?.Keys, WallsByEventID?.Keys);
        }
        #endregion

        #region Helper Functions
        private static IDictionary<byte, List<TiledMapObject>>? groupLayerByEventID(IEnumerable<TiledMapObject>? layerObjects)
            => layerObjects?
                .GroupBy(x => (byte)x.Properties.GetOrDefault("EventID", 0))
                .ToDictionary(x => x.Key, x => x.ToList());
        #endregion
    }
}
