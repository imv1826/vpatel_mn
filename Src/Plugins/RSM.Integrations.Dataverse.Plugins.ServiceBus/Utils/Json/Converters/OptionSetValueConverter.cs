using System;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Common.Json.Converters
{
    // TODO: Is duplicate, remove
    public class OptionSetValueConverter : JsonConverter<OptionSetValue>
    {
        public override OptionSetValue ReadJson(
            JsonReader reader,
            Type objectType,
            OptionSetValue existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var dict = serializer.Deserialize<OptionSetValue>(reader);
            return dict;
        }

        public override void WriteJson(JsonWriter writer, OptionSetValue value, JsonSerializer serializer)
        {
            serializer.Serialize(
                writer,
                value.Value
            );
        }
    }
}