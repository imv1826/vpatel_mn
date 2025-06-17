using System;

namespace RSM.Integrations.Dataverse.Models.Messages
{
    public interface IServiceBusMessage
    {
        long EventInitiatedOn { get; set; }
        string MessageName { get; set; }
        string LogLevel { get; set; }
    }
}