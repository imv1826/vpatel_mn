using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Common.Json.Converters
{
    public class AttributeCollectionConverter : JsonConverter<AttributeCollection>
    {
        public override AttributeCollection ReadJson(
            JsonReader reader,
            Type objectType,
            AttributeCollection existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var dict = serializer.Deserialize<Dictionary<string, object>>(reader);
            var attributes = new AttributeCollection();
            if (dict == null) return attributes;

            foreach (var kvp in dict)
            {
                attributes.Add(kvp.Key, kvp.Value);
            }

            return attributes;
        }

        public override void WriteJson(JsonWriter writer, AttributeCollection value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }
}