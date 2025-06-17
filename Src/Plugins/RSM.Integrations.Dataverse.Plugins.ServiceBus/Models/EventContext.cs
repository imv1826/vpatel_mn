using System;
using RSM.Integrations.Dataverse.Models.Messages;

namespace RSM.Integrations.Dataverse.Models
{
    public interface IEventContext
    {
        string SessionId { get; set; }
        DateTime EventInitiatedOn { get; set; }
        string MessageName { get; set; }
        string LogLevel { get; set; }
        string TableName { get; set; }
        MessageDocument MessageDocument { get; set; }
    }

    public class EventContext : IEventContext
    {
        public string SessionId { get; set; }
        public DateTime EventInitiatedOn { get; set; }
        public string MessageName { get; set; }
        public string LogLevel { get; set; }
        public string TableName { get; set; }
        public MessageDocument MessageDocument { get; set; }
    }
}