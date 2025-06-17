using System;

namespace RSM.Integrations.Dataverse.Models.Enums
{
    [Flags]
    public enum MessageAttributeOptions
    {
        ModifiedOnly = 1,
        Formatted = 2,
        Image = 4
    }
}