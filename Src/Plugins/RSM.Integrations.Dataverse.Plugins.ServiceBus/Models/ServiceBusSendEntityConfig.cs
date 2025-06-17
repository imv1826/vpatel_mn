using System;
using Newtonsoft.Json;
using RSM.Integrations.Dataverse.Models.Enums;

namespace RSM.Integrations.Dataverse.Models
{
    public class ServiceBusSendEntityConfig : ServiceBusIntegrationConfig
    {
        /// <summary>
        /// The FetchXML query _<i>format</i>_ to send to the Service Bus. This string should be formatted with the
        /// target entity ID as the first and only parameter.
        /// </summary>
        [JsonProperty("fetchXml")]
        public string FetchXml { get; set; }

        [JsonProperty("ignoreChangesFrom")] public Guid IgnoreChangesFrom { get; set; }

        [JsonProperty("attributeOptions")] public MessageAttributeOptions AttributeOptions { get; set; }
    }
}