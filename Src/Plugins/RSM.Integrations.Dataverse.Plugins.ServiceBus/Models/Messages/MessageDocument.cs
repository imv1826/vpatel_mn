using System.Collections.Generic;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Models.Messages
{
    public class MessageDocument
    {
        [JsonExtensionData] public Dictionary<string, object> Attributes { get; set; }
    }
}