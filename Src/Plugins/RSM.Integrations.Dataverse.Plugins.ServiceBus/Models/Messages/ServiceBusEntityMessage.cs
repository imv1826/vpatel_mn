using System;
using Newtonsoft.Json;
using RSM.Integrations.Dataverse.Common.ExtensionMethods;

namespace RSM.Integrations.Dataverse.Models.Messages
{
    public class ServiceBusEntityMessage : IServiceBusMessage
    {
        [JsonProperty("initiatedOn", Required = Required.Always)]
        public long EventInitiatedOn { get; set; }

        [JsonProperty("messageName", Required = Required.Always)]
        public string MessageName { get; set; }

        [JsonProperty("logLevel", Required = Required.Always)]
        public string LogLevel { get; set; }

        [JsonProperty("tableName", Required = Required.Always)]
        public string TableName { get; set; }

        [JsonProperty("document", Required = Required.Always)]
        public MessageDocument Document { get; set; }

        public ServiceBusEntityMessage()
        {
        }

        public ServiceBusEntityMessage(DateTime eventCreatedOn)
        {
            EventInitiatedOn = ParseEventUnixTime(eventCreatedOn);
        }

        private static long ParseEventUnixTime(DateTime eventCreatedOn)
        {
            if (eventCreatedOn.Year <= 1900) eventCreatedOn = default;

            var initiatedOnUtc = eventCreatedOn == default
                ? DateTime.UtcNow
                : eventCreatedOn.Kind == DateTimeKind.Utc
                    ? eventCreatedOn
                    : eventCreatedOn.ToUniversalTime();

            return initiatedOnUtc.ToUnixTime();
        }
    }
}