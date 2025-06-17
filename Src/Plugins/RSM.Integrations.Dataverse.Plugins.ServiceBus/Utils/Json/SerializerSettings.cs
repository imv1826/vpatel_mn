using Newtonsoft.Json;
using RSM.Integrations.Dataverse.Common.Json.Converters;

namespace RSM.Integrations.Dataverse.Common.Json
{
    public class SerializerSettings
    {
        public static readonly JsonSerializerSettings DataverseEntity = new JsonSerializerSettings
        {
            Converters = new JsonConverter[]
            {
                new AttributeCollectionConverter(), new EntityReferenceConverter(),
                new FormattedValueCollectionConverter(), new KeyAttributesCollectionConverter(),
                new OptionSetValueConverter(), new RelatedEntitiesCollectionConverter()
            }
        };
    }
}