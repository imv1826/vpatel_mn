using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Models.Messages
{
    public class ServiceBusQueueBrokerProperties
    {
        public Guid? MessageId { get; set; }
        public string SessionId { get; set; }
        public Guid? CorrelationId { get; set; }
        public int? TimeToLive { get; set; }
        public int? SequenceNumber { get; set; }
        public int? DeliveryCount { get; set; }
        public string To { get; set; }
        public string ReplyTo { get; set; }
        public DateTime? EnqueuedTimeUtc { get; set; }
        public DateTime? ScheduledEnqueueTimeUtc { get; set; }

        public string BuildJson(bool pretty = false)
        {
            var dict = new Dictionary<string, object>();

            if (MessageId.HasValue) dict.Add("MessageId", MessageId.Value);
            if (!string.IsNullOrWhiteSpace(SessionId)) dict.Add("SessionId", SessionId);
            if (CorrelationId.HasValue) dict.Add("CorrelationId", CorrelationId.Value);
            if (TimeToLive.HasValue) dict.Add("TimeToLive", TimeToLive.Value);
            if (SequenceNumber.HasValue) dict.Add("SequenceNumber", SequenceNumber.Value);
            if (DeliveryCount.HasValue) dict.Add("DeliveryCount", DeliveryCount.Value);
            if (!string.IsNullOrWhiteSpace(To)) dict.Add("To", To);
            if (!string.IsNullOrWhiteSpace(ReplyTo)) dict.Add("ReplyTo", ReplyTo);
            if (EnqueuedTimeUtc.HasValue) dict.Add("EnqueuedTimeUtc", EnqueuedTimeUtc.Value);
            if (ScheduledEnqueueTimeUtc.HasValue) dict.Add("ScheduledEnqueueTimeUtc", ScheduledEnqueueTimeUtc.Value);

            return JsonConvert.SerializeObject(dict, pretty ? Formatting.Indented : Formatting.None);
        }
    }
}