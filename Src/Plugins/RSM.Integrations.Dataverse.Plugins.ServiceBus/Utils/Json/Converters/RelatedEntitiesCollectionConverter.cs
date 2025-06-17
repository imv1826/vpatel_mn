using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Common.Json.Converters
{
    public class RelatedEntitiesCollectionConverter : JsonConverter<RelatedEntityCollection>
    {
        public override void WriteJson(JsonWriter writer, RelatedEntityCollection value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        public override RelatedEntityCollection ReadJson(
            JsonReader reader,
            Type objectType,
            RelatedEntityCollection existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var dict = serializer.Deserialize<Dictionary<Relationship, EntityCollection>>(reader);
            var attributes = new RelatedEntityCollection();
            if (dict == null) return attributes;

            foreach (var kvp in dict)
            {
                attributes.Add(kvp.Key, kvp.Value);
            }

            return attributes;
        }
    }
}