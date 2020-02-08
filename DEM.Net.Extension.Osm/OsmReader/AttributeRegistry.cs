using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DEM.Net.Extension.Osm.Schema;

namespace DEM.Net.Extension.Osm
{   
    public class AttributeRegistry
    {
        private readonly ConcurrentDictionary<OsmAttribute, ConcurrentDictionary<string, int>> _attributeValueIds;

        public AttributeRegistry()
        {
            _attributeValueIds = new ConcurrentDictionary<OsmAttribute, ConcurrentDictionary<string, int>>();
        }

        public int GetAttributeValueId(OsmAttribute osmAttribute, string value)
        {
            value = value.Trim();

            ConcurrentDictionary<string, int> valueToId = null;

            if (!_attributeValueIds.TryGetValue(osmAttribute, out valueToId))
            {
                lock (_attributeValueIds)
                {
                    if (!_attributeValueIds.TryGetValue(osmAttribute, out valueToId))
                    {
                        _attributeValueIds.TryAdd(
                            osmAttribute,
                            valueToId = new ConcurrentDictionary<string, int>(StringComparer.InvariantCultureIgnoreCase));
                    }
                }
            }

            int attributeValueId;
            if (valueToId.TryGetValue(value, out attributeValueId))
                return attributeValueId;

            lock (valueToId)
            {
                attributeValueId = valueToId.Count;
                valueToId.TryAdd(value, attributeValueId);
                return attributeValueId;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> GetAttributeValues(OsmAttribute attribute)
        {
            ConcurrentDictionary<string, int> valueIds;

            if (!_attributeValueIds.TryGetValue(attribute, out valueIds))
                yield break;

            foreach (var pair in valueIds)
                yield return new KeyValuePair<int, string>(pair.Value, pair.Key);
        }
    }
}