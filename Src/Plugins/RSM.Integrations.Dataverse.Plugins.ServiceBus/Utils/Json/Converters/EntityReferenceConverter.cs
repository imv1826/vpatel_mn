using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Common.Json.Converters
{
    public class EntityReferenceConverter : JsonConverter<EntityReference>
    {
        public override EntityReference ReadJson(
            JsonReader reader,
            Type objectType,
            EntityReference existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var dict = serializer.Deserialize<Dictionary<string, string>>(reader);
            if (dict == null) return null;

            return new EntityReference(
                dict.TryGetValue("LogicalName", out var logicalName) ? logicalName : "",
                Guid.TryParse(dict.TryGetValue("Id", out var idString) ? idString : "", out var id) ? id : Guid.Empty
            ) { Name = dict.TryGetValue("Name", out var name) ? name : "" };
        }

        public override void WriteJson(JsonWriter writer, EntityReference value, JsonSerializer serializer)
        {
            serializer.Serialize(
                writer,
                new Dictionary<string, string>
                {
                    { "LogicalName", value.LogicalName },
                    { "Id", value.Id.ToString("D") },
                    { "Name", value.Name }
                }
            );
        }
    }
}