using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using RSM.Integrations.Dataverse.Common.ExtensionMethods;
using RSM.Integrations.Dataverse.Models.Enums;
using RSM.Integrations.Dataverse.Models.Messages;

namespace RSM.Integrations.Dataverse.Services
{
    public class EntityFetchResponseParser : IDisposable
    {
        private readonly ITracingService _tracingSvc;
        private readonly MessageAttributeOptions _attributeOptions;
        private readonly EntityFetchResponseMap _responseMap;

        public EntityFetchResponseParser(
            ITracingService tracingSvc,
            string fetchXml,
            MessageAttributeOptions attributeOptions
        )
        {
            _tracingSvc = tracingSvc;
            _attributeOptions = attributeOptions;
            _responseMap = new EntityFetchResponseMap(tracingSvc, fetchXml);
        }

        public void Dispose()
        {
            _responseMap?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Parses the entity fetch response, and create/update target, and returns a service bus queue message entity.
        /// </summary>
        /// <param name="fetchResponse"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public MessageDocument ParseMessageEntity(
            EntityCollection fetchResponse,
            Entity targetEntity = null
        )
        {
            var messageEntity = new MessageDocument();
            if (fetchResponse.Entities.Count == 0) return messageEntity;

            var fetchEntity = fetchResponse.Entities.First();

            _tracingSvc.Trace($"Svc Bus Config MessageAttributeOptions: {_attributeOptions}");
            var isModifiedOnly = (_attributeOptions & MessageAttributeOptions.ModifiedOnly) != 0 && targetEntity != null;
            _tracingSvc.Trace($"isModifiedOnly: {isModifiedOnly}");
            _tracingSvc.Trace(
                isModifiedOnly
                    ? "Only modified attributes will be included in the message entity."
                    : "All attributes will be included in the message entity."
            );

            _tracingSvc.Trace($"Is target entity null: {targetEntity == null}");

            // var rootAttributes = _responseMap.Attributes.Select(a => a.Name).ToArray();

            var rootAttributes = isModifiedOnly
                ? targetEntity.Attributes.Keys.ToArray()
                : _responseMap.Attributes.Select(a => a.Name).ToArray();

            _tracingSvc.Trace($"Root attributes count: {rootAttributes.Length}");

            var attributes = fetchEntity
                .Attributes.Where(attr => rootAttributes.Contains(attr.Key))
                .ToParsedDictionary();

            _tracingSvc.Trace($"Message root attributes count: {attributes.Keys.Count}");

            var includeFormatted = (_attributeOptions & MessageAttributeOptions.Formatted) != 0;
            _tracingSvc.Trace(
                includeFormatted
                    ? "Including formatted values in the message entity."
                    : "Formatted values NOT included in the svc bus message entity."
            );

            if (includeFormatted)
            {
                foreach (var kvp in fetchEntity.FormattedValues.Where(v => !v.Key.Contains('.'))
                             .ToParsedFormattedDictionary(fetchEntity.Attributes))
                {
                    if (!attributes.ContainsKey(kvp.Key))
                    {
                        attributes.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            // Order the attributes by name
            messageEntity.Attributes = attributes.OrderBy(a => a.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var rootGroup = fetchResponse.Entities
                .GroupBy(e => GetPrimaryIdAttribute(e, _responseMap.PrimaryIdAttribute.Name))
                .ToArray();

            if (rootGroup.Length > 1)
            {
                throw new ApplicationException("Multiple root entities found in an entity specific fetch response.");
            }

            if (_responseMap.LinkedEntities.Count <= 0) return messageEntity;

            var joinedEntitiesDict = ParseJoinedEntities(_responseMap, rootGroup.First());

            foreach (var kvp in joinedEntitiesDict)
            {
                if (kvp.Key.StartsWith("#manyToOne") && kvp.Value is Dictionary<string, object> manyToOne)
                {
                    foreach (var k in manyToOne)
                    {
                        if (!messageEntity.Attributes.ContainsKey(k.Key))
                        {
                            messageEntity.Attributes.Add(k.Key, k.Value);
                        }
                    }

                    continue;
                }

                if (!messageEntity.Attributes.ContainsKey(kvp.Key))
                {
                    messageEntity.Attributes.Add(kvp.Key, kvp.Value);
                }
            }

            return messageEntity;
        }

        private Dictionary<string, object> ParseJoinedEntities(
            EntityFetchResponseMap fetchMap,
            IGrouping<Guid?, Entity> group
        )
        {
            var joinedEntities = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var manyToOneJoinsIndex = 0;
            foreach (var linkedMap in fetchMap.LinkedEntities)
            {
                var makeFlat = linkedMap.LinkInfo.RelationshipToParent == EntityRelationship.OneToMany;

                var entityGroup = group
                    .Where(e => HasPrimaryIdAttribute(e, linkedMap.PrimaryIdAttribute))
                    .GroupBy(grp => GetPrimaryIdAttribute(grp, linkedMap.PrimaryIdAttribute))
                    .ToArray();

                var joinEntityDicts = new List<Dictionary<string, object>>();
                foreach (var subGroup in entityGroup)
                {
                    var entity = subGroup.First();

                    var entityDict = makeFlat
                        ? entity.Attributes
                            .Where(attr => linkedMap.Attributes.Any(a => a.Value.Equals(attr.Key)))
                            .Select(
                                attr =>
                                {
                                    var aliasedAttr = linkedMap.Attributes.FirstOrDefault(a => a.Value.Equals(attr.Key));
                                    return new KeyValuePair<string, object>(
                                        $"{linkedMap.LinkInfo.FullAlias}.{aliasedAttr.Name}",
                                        attr.Value
                                    );
                                }
                            )
                            .ToParsedDictionary()
                        : entity.Attributes
                            .Where(attr => linkedMap.Attributes.Any(a => a.Value.Equals(attr.Key)))
                            .Select(
                                attr => new KeyValuePair<string, object>(attr.Key.Split('.').LastOrDefault(), attr.Value)
                            )
                            .ToParsedDictionary();

                    var includeFormatted = (_attributeOptions & MessageAttributeOptions.Formatted) != 0;

                    _tracingSvc.Trace(
                        includeFormatted
                            ? $"Including formatted values in the linked entity {linkedMap.LinkInfo.FetchAlias}."
                            : $"Formatted values NOT included in the svc bus linked entity {linkedMap.LinkInfo.FetchAlias}."
                    );

                    if (includeFormatted)
                    {
                        var formatted = makeFlat
                            ? entity.FormattedValues
                                .Where(attr => linkedMap.Attributes.Any(a => a.Value.Equals(attr.Key)))
                                .Select(
                                    attr =>
                                    {
                                        var aliasedAttr = linkedMap.Attributes.FirstOrDefault(a => a.Value.Equals(attr.Key));
                                        return new KeyValuePair<string, string>(
                                            $"{linkedMap.LinkInfo.FullAlias}.{aliasedAttr.Name}",
                                            attr.Value
                                        );
                                    }
                                )
                                .ToParsedFormattedDictionary(entity.Attributes)
                            : entity.FormattedValues
                                .Where(attr => linkedMap.Attributes.Any(a => a.Value.Equals(attr.Key)))
                                .Select(
                                    attr => new KeyValuePair<string, string>(attr.Key.Split('.').LastOrDefault(), attr.Value)
                                )
                                .ToParsedFormattedDictionary(entity.Attributes);

                        foreach (var kvp in formatted)
                        {
                            if (!entityDict.ContainsKey(kvp.Key))
                            {
                                entityDict.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }

                    if (linkedMap.LinkedEntities.Count > 0)
                    {
                        var dict = ParseJoinedEntities(linkedMap, subGroup);

                        foreach (var kvp in dict.Where(kvp => !entityDict.ContainsKey(kvp.Key)))
                        {
                            if (kvp.Key.StartsWith("#manyToOne") && kvp.Value is Dictionary<string, object> manyToOne)
                            {
                                foreach (var k in manyToOne)
                                {
                                    if (!entityDict.ContainsKey(k.Key))
                                    {
                                        entityDict.Add(k.Key, k.Value);
                                    }
                                }
                            }
                            else
                            {
                                if (!entityDict.ContainsKey(kvp.Key))
                                {
                                    entityDict.Add(kvp.Key, kvp.Value);
                                }
                            }
                        }
                    }

                    joinEntityDicts.Add(entityDict);
                }

                // Many to One joins are flattened to a single dictionary
                var manToOneKeyCurrent = $"#manyToOne{manyToOneJoinsIndex}";
                if (makeFlat && joinEntityDicts.Count == 1 && !joinedEntities.ContainsKey(manToOneKeyCurrent))
                {
                    joinedEntities.Add(manToOneKeyCurrent, joinEntityDicts.First());
                    manyToOneJoinsIndex++;
                    continue;
                }

                // All other joins are grouped by the fetch alias into an array of dictionaries
                if (!joinedEntities.ContainsKey(linkedMap.LinkInfo.FetchAlias))
                {
                    joinedEntities.Add(linkedMap.LinkInfo.FetchAlias, joinEntityDicts.ToArray());
                }
            }

            // Push the many to one joins to the bottom
            return joinedEntities.OrderBy(k => !k.Key.StartsWith("#manyToOne"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private static bool HasPrimaryIdAttribute(Entity entity, AliasedAttribute idAttr)
        {
            return entity.TryGetAttributeValue(idAttr.Value, out AliasedValue aliasedId) &&
                   aliasedId.Value is Guid;
        }

        private static Guid? GetPrimaryIdAttribute(Entity entity, string idAttr)
        {
            return entity.TryGetAttributeValue(idAttr, out Guid? id) ? id : null;
        }

        private static Guid? GetPrimaryIdAttribute(Entity entity, AliasedAttribute idAttr)
        {
            return entity.TryGetAttributeValue(idAttr.Value, out AliasedValue aliasedId) && aliasedId.Value is Guid id
                ? id
                : (Guid?)null;
        }
    }
}