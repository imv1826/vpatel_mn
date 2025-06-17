using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace RSM.Integrations.Dataverse.Common.ExtensionMethods
{
    public static class AttributeCollectionExtensions
    {
        public static Dictionary<string, object> ToParsedDictionary(
            this IEnumerable<KeyValuePair<string, object>> attributes
        )
        {
            var attrsArray = attributes as KeyValuePair<string, object>[] ?? attributes.ToArray();

            var dict = new Dictionary<string, object>(attrsArray.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var attr in attrsArray)
            {
                if (!dict.ContainsKey(attr.Key)) dict.Add(attr.Key, ParseAttributeValue(attr.Value));
            }

            return dict;
        }

        public static Dictionary<string, object> ToParsedFormattedDictionary(
            this IEnumerable<KeyValuePair<string, string>> formattedAttributes,
            AttributeCollection attributes
        )
        {
            var formattedValsArray = formattedAttributes as KeyValuePair<string, string>[] ?? formattedAttributes.ToArray();

            var dict = new Dictionary<string, object>(formattedValsArray.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var attr in formattedValsArray)
            {
                var key = $"{attr.Key}formatted";

                var foundAttr = attributes.FirstOrDefault(a => a.Key == attr.Key || a.Key.Contains($".{attr.Key}"));
                if (!string.IsNullOrWhiteSpace(foundAttr.Key))
                {
                    switch (foundAttr.Value)
                    {
                        case EntityReference _:
                        case OptionSetValue _:
                        case AliasedValue av when av.Value is EntityReference || av.Value is OptionSetValue:
                            key = $"{attr.Key}name";
                            break;
                    }
                }

                if (!dict.ContainsKey(key)) dict.Add(key, attr.Value);
            }

            return dict;
        }

        public static TValue ParseAttributeValue<TValue>(TValue value) where TValue : class
        {
            switch (value)
            {
                case AliasedValue aliased:
                    return ParseAttributeValue(aliased.Value as TValue);
                case EntityReference entityRef:
                    return entityRef.Id as TValue;
                case OptionSetValue optSet:
                    return optSet.Value as TValue;
                case Money money:
                    return money.Value as TValue;
                case Guid guid:
                    return guid == Guid.Empty ? null : guid as TValue;
                default:
                    return value;
            }
        }
    }
}