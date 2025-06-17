using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.Common.ExtensionMethods
{
    public static class EntityExtensions
    {
        public static AliasedValue GetAliasedValue(this Entity entity, string alias)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException("Alias cannot be null or empty.", nameof(alias));

            // Attempt to retrieve the aliased value from the entity
            if (entity.Attributes.TryGetValue(alias, out var value) && value is AliasedValue typedValue)
            {
                return typedValue;
            }

            // If the value is not found or not of the expected type, return null
            return null;
        }

        public static bool IsEqualValues(
            this Entity preImage,
            Entity messageTarget,
            string[] attrsCompared,
            out KeyValuePair<string, (object, object)> changedAttributes
        )
        {
            changedAttributes = default;

            if (preImage == null && messageTarget == null) return true;
            if (preImage == null || messageTarget == null) return false;

            foreach (var attrName in attrsCompared)
            {
                if (preImage.Contains(attrName) && messageTarget.Contains(attrName))
                {
                    var preValue = ParseAttributeValue(preImage.GetAttributeValue<object>(attrName));
                    var currentValue = ParseAttributeValue(messageTarget.GetAttributeValue<object>(attrName));

                    if (!preValue.Equals(currentValue))
                    {
                        changedAttributes = new KeyValuePair<string, (object, object)>(attrName, (preValue, currentValue));
                        return false; // Change detected
                    }

                }
            }

            // No changes were detected
            return true;
        }

        private static object ParseAttributeValue(object value)
        {
            switch (value)
            {
                case Money money:
                    return money.Value;
                case EntityReference entityRef:
                    return entityRef.Id;
                case OptionSetValue optionSet:
                    return optionSet.Value;
                case AliasedValue aliasedValue:
                    return ParseAttributeValue(aliasedValue.Value);
                default:
                    return value;
            }
        }
    }
}