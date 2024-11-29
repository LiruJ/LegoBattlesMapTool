using TiledToLB.Core.Tiled.Map;

namespace TiledToLB.Core.LegoBattles.DataStructures
{
    internal class EventLayers
    {
        #region Properties
        public IEnumerable<int> AllSortKeys { get; }

        public TiledMapObjectGroup? PatrolPointsLayer { get; }

        public IDictionary<int, List<TiledMapObject>>? PatrolPointsBySortKey { get; }

        public TiledMapObjectGroup? CameraBoundsLayer { get; }

        public IDictionary<int, List<TiledMapObject>>? CameraBoundsBySortKey { get; }

        public TiledMapObjectGroup? EntitiesLayer { get; }

        public IDictionary<int, List<TiledMapObject>>? EntitiesBySortKey { get; }

        public TiledMapObjectGroup? PickupsLayer { get; }

        public IDictionary<int, List<TiledMapObject>>? PickupsBySortKey { get; }

        public TiledMapObjectGroup? WallsLayer { get; }

        public IDictionary<int, List<TiledMapObject>>? WallsBySortKey { get; }
        #endregion

        #region Constructors
        public EventLayers(TiledMapObjectGroup? patrolPointsLayer, TiledMapObjectGroup? cameraBoundsLayer, TiledMapObjectGroup? entitiesLayer, TiledMapObjectGroup? pickupsLayer, TiledMapObjectGroup? wallsLayer)
        {
            PatrolPointsLayer = patrolPointsLayer;
            CameraBoundsLayer = cameraBoundsLayer;
            EntitiesLayer = entitiesLayer;
            PickupsLayer = pickupsLayer;
            WallsLayer = wallsLayer;

            PatrolPointsBySortKey = groupLayerBySortKey(patrolPointsLayer);
            CameraBoundsBySortKey = groupLayerBySortKey(cameraBoundsLayer);
            PickupsBySortKey = groupLayerBySortKey(pickupsLayer);
            EntitiesBySortKey = groupLayerBySortKey(entitiesLayer);
            WallsBySortKey = groupLayerBySortKey(wallsLayer);

            AllSortKeys = getAllKeys(PatrolPointsBySortKey?.Keys, CameraBoundsBySortKey?.Keys, PickupsBySortKey?.Keys, EntitiesBySortKey?.Keys, WallsBySortKey?.Keys).Order();
        }
        #endregion

        #region Get Functions
        public SortKeyEventLayer GetLayerFromSortKey(int sortKey)
        {
            List<TiledMapObject>? patrolPoints = null, cameraBounds = null, entities = null, pickups = null, walls = null;
            PatrolPointsBySortKey?.TryGetValue(sortKey, out patrolPoints);
            CameraBoundsBySortKey?.TryGetValue(sortKey, out cameraBounds);
            EntitiesBySortKey?.TryGetValue(sortKey, out entities);
            PickupsBySortKey?.TryGetValue(sortKey, out pickups);
            WallsBySortKey?.TryGetValue(sortKey, out walls);

            // Ensure at least one layer exists.
            if (patrolPoints == null && cameraBounds == null && entities == null && pickups == null && walls == null)
                throw new ArgumentException("Given sort key has no layers!", nameof(sortKey));

            return new SortKeyEventLayer(sortKey, patrolPoints, cameraBounds, entities, pickups, walls);
        }
        #endregion

        #region Helper Functions
        private static IDictionary<int, List<TiledMapObject>>? groupLayerBySortKey(TiledMapObjectGroup? objectGroup)
            => objectGroup?.Objects
                .GroupBy(x => x.Properties.GetOrDefault("SortKey", int.MaxValue))
                .ToDictionary(x => x.Key, x => x.ToList());

        internal static IEnumerable<T> getAllKeys<T>(params IEnumerable<T>?[] collections)
        {
            IEnumerable<T> allKeys = collections[0] ?? [];
            if (collections.Length == 1)
                return allKeys;

            for (int i = 1; i < collections.Length; i++)
                if (collections[i] != null)
                    allKeys = allKeys.Union(collections[i]!);

            return allKeys;
        }
        #endregion

        #region Load Functions
        public static EventLayers LoadFromTiledMap(TiledMap tiledMap)
        {
            tiledMap.ObjectGroups.TryGetValue("Patrol Points", out TiledMapObjectGroup? patrolPointsLayer);
            tiledMap.ObjectGroups.TryGetValue("Camera Bounds", out TiledMapObjectGroup? cameraBoundsLayer);
            tiledMap.ObjectGroups.TryGetValue("Entities", out TiledMapObjectGroup? entitiesLayer);
            tiledMap.ObjectGroups.TryGetValue("Pickups", out TiledMapObjectGroup? pickupsLayer);
            tiledMap.ObjectGroups.TryGetValue("Walls", out TiledMapObjectGroup? wallsLayer);

            return new(patrolPointsLayer, cameraBoundsLayer, entitiesLayer, pickupsLayer, wallsLayer);
        }
        #endregion
    }
}
