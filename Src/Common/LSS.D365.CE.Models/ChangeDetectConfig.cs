using Newtonsoft.Json;

namespace LSS.D365.CE.Models
{
    /// <summary>
    /// Configuration for detecting changes in a source entity and applying them to a target entity.
    /// </summary>
    public class ChangeDetectConfig
    {
        [JsonProperty("sourceAttributesMonitored", Required = Required.Always)]
        public string[] SourceAttributesMonitored { get; set; }
    }
}