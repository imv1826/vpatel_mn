using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Common.Json.Converters
{
    public class FormattedValueCollectionConverter : JsonConverter<FormattedValueCollection>
    {
        public override FormattedValueCollection ReadJson(
            JsonReader reader,
            Type objectType,
            FormattedValueCollection existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var dict = serializer.Deserialize<Dictionary<string, object>>(reader);
            var attributes = new FormattedValueCollection();
            if (dict == null) return attributes;

            foreach (var kvp in dict)
            {
                attributes.Add(kvp.Key, kvp.Value.ToString());
            }

            return attributes;
        }

        public override void WriteJson(JsonWriter writer, FormattedValueCollection value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }
}