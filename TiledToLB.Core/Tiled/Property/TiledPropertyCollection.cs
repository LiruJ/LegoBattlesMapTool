using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using TiledToLB.Core.Tiled.Map;

namespace TiledToLB.Core.Tiled.Property
{
    public class TiledPropertyCollection : IEnumerable<TiledProperty>, IEnumerable<string>, IDictionary<string, TiledProperty>
    {
        #region Fields
        private readonly Dictionary<string, TiledProperty> properties = [];
        #endregion

        #region Properties
        public IReadOnlyDictionary<string, TiledProperty> Properties => properties;

        public ICollection<string> Keys => ((IDictionary<string, TiledProperty>)properties).Keys;

        public ICollection<TiledProperty> Values => ((IDictionary<string, TiledProperty>)properties).Values;

        public int Count => ((ICollection<KeyValuePair<string, TiledProperty>>)properties).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, TiledProperty>>)properties).IsReadOnly;
        #endregion

        #region Dictionary Functions
        public TiledProperty this[string key] { get => ((IDictionary<string, TiledProperty>)properties)[key]; set => ((IDictionary<string, TiledProperty>)properties)[key] = value; }

        public bool ContainsKey(string key) => ((IDictionary<string, TiledProperty>)properties).ContainsKey(key);

        public bool Remove(string key) => ((IDictionary<string, TiledProperty>)properties).Remove(key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out TiledProperty value) => ((IDictionary<string, TiledProperty>)properties).TryGetValue(key, out value);

        public void Add(string key, TiledProperty value) => ((IDictionary<string, TiledProperty>)properties).Add(key, value);

        public void Add(KeyValuePair<string, TiledProperty> item) => ((ICollection<KeyValuePair<string, TiledProperty>>)properties).Add(item);

        public void Clear() => ((ICollection<KeyValuePair<string, TiledProperty>>)properties).Clear();

        public bool Contains(KeyValuePair<string, TiledProperty> item) => ((ICollection<KeyValuePair<string, TiledProperty>>)properties).Contains(item);

        public void CopyTo(KeyValuePair<string, TiledProperty>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, TiledProperty>>)properties).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, TiledProperty> item) => ((ICollection<KeyValuePair<string, TiledProperty>>)properties).Remove(item);

        IEnumerator<KeyValuePair<string, TiledProperty>> IEnumerable<KeyValuePair<string, TiledProperty>>.GetEnumerator() => ((IEnumerable<KeyValuePair<string, TiledProperty>>)properties).GetEnumerator();
        #endregion

        #region Enumerator Functions
        IEnumerator IEnumerable.GetEnumerator() => properties.GetEnumerator();

        public IEnumerator<TiledProperty> GetEnumerator() => properties.Values.GetEnumerator();

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => properties.Keys.GetEnumerator();
        #endregion

        #region Get Functions
        public string GetOrDefault(string name, string defaultValue)
            => Properties.TryGetValue(name, out TiledProperty property) ? property.Value : defaultValue;

        public int GetOrDefault(string name, int defaultValue)
            => Properties.TryGetValue(name, out TiledProperty property) && property.Type == TiledPropertyType.Int && int.TryParse(property.Value, out int value)
            ? value
            : defaultValue;

        public float GetOrDefault(string name, float defaultValue)
            => Properties.TryGetValue(name, out TiledProperty property) && property.Type == TiledPropertyType.Float && float.TryParse(property.Value, out float value)
            ? value
            : defaultValue;

        public bool GetOrDefault(string name, bool defaultValue)
            => Properties.TryGetValue(name, out TiledProperty property) && property.Type == TiledPropertyType.Bool && bool.TryParse(property.Value, out bool value) ? value : defaultValue;

        public TiledProperty Get(string name) => Properties[name];
        #endregion

        #region Set Functions
        public void Add(string name, string value)
            => properties.Add(name, new TiledProperty(name, value, TiledPropertyType.String, null));

        public void Add(string name, int value)
            => properties.Add(name, new TiledProperty(name, value.ToString(), TiledPropertyType.Int, null));

        public void Add(string name, float value)
            => properties.Add(name, new TiledProperty(name, value.ToString(), TiledPropertyType.Float, null));

        public void Add(string name, bool value)
            => properties.Add(name, new TiledProperty(name, value.ToString().ToLower(), TiledPropertyType.Bool, null));

        public void Add(TiledProperty property)
            => properties.Add(property.Name, property);

        public void Set(string name, string value)
            => properties[name] = new TiledProperty(name, value, TiledPropertyType.String, null);

        public void Set(string name, int value)
            => properties[name] = new TiledProperty(name, value.ToString(), TiledPropertyType.Int, null);

        public void Set(string name, float value)
            => properties[name] = new TiledProperty(name, value.ToString(), TiledPropertyType.Float, null);

        public void Set(string name, bool value)
            => properties[name] = new TiledProperty(name, value.ToString(), TiledPropertyType.Bool, null);

        public void Set(TiledProperty property)
            => properties[property.Name] = property;
        #endregion

        #region Load Functions
        public static TiledPropertyCollection LoadFromNode(XmlNodeList? propertyNodes)
        {
            TiledPropertyCollection properties = [];
            properties.Load(propertyNodes);
            return properties;
        }

        public void Load(XmlNodeList? propertyNodes)
        {
            if (propertyNodes == null)
                return;

            foreach (XmlNode propertyNode in propertyNodes)
            {
                if (propertyNode.NodeType != XmlNodeType.Element)
                    continue;

                TiledProperty property = TiledProperty.LoadFromNode(propertyNode);
                Add(property);
            }
        }
        #endregion

        #region Save Functions
        public void Save(XmlNode propertiesNode)
        {
            foreach (TiledProperty property in Properties.Values)
                property.SaveToNode(propertiesNode);
        }
        #endregion
    }
}
