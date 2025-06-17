using System;
using Newtonsoft.Json;
using RSM.Integrations.Dataverse.Models.Enums;

namespace RSM.Integrations.Dataverse.Models
{
    public class ServiceBusIntegrationConfig
    {
        /// <summary>
        /// The base URL for the Service Bus
        /// </summary>
        [JsonProperty("baseUrl", Required = Required.Always)]
        public string BaseUrl { get; set; }

        /// <summary>
        /// The Service Bus queue name where the message will be sent
        /// </summary>
        [JsonProperty("queueName", Required = Required.Always)]
        public string QueueName { get; set; }

        /// <summary>
        /// The Shared Access Key for the Service Bus
        /// </summary>
        [JsonProperty("sharedAccessKey", Required = Required.Always)]
        public string SharedAccessKey { get; set; }

        /// <summary>
        /// The Shared Access Key Name for the Service Bus
        /// </summary>
        [JsonProperty("sharedAccessKeyName", Required = Required.Always)]
        public string SharedAccessKeyName { get; set; }

        /// <summary>
        /// Logging level for the integration overall. Default is Error.
        /// </summary>
        [JsonProperty("logLevel")]
        public LogLevel LogLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// The expiry time in seconds for the Service Bus message request token
        /// </summary>
        [JsonProperty("expirySeconds")]
        public int ExpirySeconds { get; set; } = 1_200;

        /// <summary>
        /// The amount of time in seconds to wait for any Service Bus HTTP requests to complete
        /// </summary>
        [JsonProperty("serviceBusHttpTimeoutSeconds")]
        public int ServiceBusHttpTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// The amount of time in seconds to wait for any Service Bus HTTP requests to complete
        /// </summary>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        /// <summary>
        /// Gets the expiry TimeSpan based on the provided ExpirySeconds
        /// </summary>
        [JsonIgnore]
        public TimeSpan ExpiryTime => TimeSpan.FromSeconds(ExpirySeconds);

        /// <summary>
        /// Gets the ServiceBusHttpTimeout TimeSpan based on the provided ServiceBusHttpTimeoutSeconds
        /// </summary>
        [JsonIgnore]
        public TimeSpan ServiceBusHttpTimeout => TimeSpan.FromSeconds(ServiceBusHttpTimeoutSeconds);
    }
}