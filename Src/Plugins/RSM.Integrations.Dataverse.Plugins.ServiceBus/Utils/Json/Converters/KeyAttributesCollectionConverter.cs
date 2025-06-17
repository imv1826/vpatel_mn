using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Common.Json.Converters
{
    public class KeyAttributesCollectionConverter : JsonConverter<KeyAttributeCollection>
    {
        public override void WriteJson(JsonWriter writer, KeyAttributeCollection value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        public override KeyAttributeCollection ReadJson(
            JsonReader reader,
            Type objectType,
            KeyAttributeCollection existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var dict = serializer.Deserialize<Dictionary<string, object>>(reader);
            var attributes = new KeyAttributeCollection();
            if (dict == null) return attributes;

            foreach (var kvp in dict)
            {
                attributes.Add(kvp.Key, kvp.Value);
            }

            return attributes;
        }
    }
}