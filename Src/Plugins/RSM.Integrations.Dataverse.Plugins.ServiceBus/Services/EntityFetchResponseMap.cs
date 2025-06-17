using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Xrm.Sdk;
using RSM.Integrations.Dataverse.Models.Enums;

namespace RSM.Integrations.Dataverse.Services
{
    public struct AliasedAttribute
    {
        public string Alias { get; set; }
        public string Name { get; set; }
        public string Value => string.IsNullOrWhiteSpace(Alias) ? Name : $"{Alias}.{Name}";

        public AliasedAttribute(string name, string alias = null)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                Alias = alias;
                Name = name;
                return;
            }

            var splitName = name.Split('.');
            Alias = splitName.Length > 2
                ? string.Join(".", splitName.Take(splitName.Length - 1))
                : splitName.Length > 1
                    ? splitName.FirstOrDefault()
                    : null;

            Name = splitName.Length > 1 ? splitName.LastOrDefault() : name;
        }
    }

    public class LinkInfo
    {
        public bool IsLink { get; set; }
        public EntityFetchResponseMap ParentEntityMap { get; private set; }
        public EntityRelationship? RelationshipToParent { get; private set; }
        public string From { get; private set; }
        public string To { get; private set; }
        public string FetchAlias { get; private set; }

        public string FullAlias
        {
            get => GetFullAlias();
            private set => _fullAlias = value;
        }

        private string GetFullAlias()
        {
            if (_hasValidFullAlias) return _fullAlias;

            var pMap = ParentEntityMap;
            while (pMap != null)
            {
                if (string.IsNullOrWhiteSpace(pMap.LinkInfo.FetchAlias)) break;

                _fullAlias = pMap.LinkInfo.FetchAlias + "." + _fullAlias;
                pMap = pMap.LinkInfo.ParentEntityMap;
            }

            var aliasSplit = _fullAlias?.Split('.').Distinct().ToArray();

            if (aliasSplit?.Length > 1)
            {
                _fullAlias = string.Join(".", aliasSplit);
            }

            var parentRelation = ParentEntityMap?.LinkInfo.RelationshipToParent;

            if (parentRelation == null || parentRelation == EntityRelationship.OneToMany) return _fullAlias;

            var dotIndex = _fullAlias?.IndexOf('.') ?? (_fullAlias?.Length ?? 0);

            // Trim the root alias if it's not needed.
            _fullAlias = dotIndex > 0 ? _fullAlias?.Substring(dotIndex + 1) : null;

            _hasValidFullAlias = true;
            return _fullAlias;
        }

        private string _fullAlias;
        private bool _hasValidFullAlias;

        public LinkInfo(XmlReader reader = null)
        {
            if (reader == null) return;
            From = reader.GetAttribute("from");
            To = reader.GetAttribute("to");
            FetchAlias = reader.GetAttribute("alias");
            FullAlias = FetchAlias;
        }

        public void SetParentMap(EntityFetchResponseMap parentMap)
        {
            IsLink = parentMap != null;
            ParentEntityMap = parentMap;

            if (!string.IsNullOrWhiteSpace(ParentEntityMap?.PrimaryIdAttribute.Name) && !string.IsNullOrWhiteSpace(To))
            {
                RelationshipToParent = ParentEntityMap.PrimaryIdAttribute.Name == To
                    ? EntityRelationship.ManyToOne
                    : EntityRelationship.OneToMany;
            }
        }
    }

    public class EntityFetchResponseMap : IDisposable
    {
        public LinkInfo LinkInfo { get; private set; }
        public string TableName { get; private set; }
        public AliasedAttribute PrimaryIdAttribute { get; private set; }
        public IReadOnlyCollection<AliasedAttribute> Attributes => _attributes;
        public IReadOnlyCollection<AliasedAttribute> RequiredAttributes => _requiredAttributes;
        public IReadOnlyCollection<EntityFetchResponseMap> LinkedEntities => _linkedEntities;

        private readonly ITracingService _tracingSvc;
        private readonly XmlReader _reader;

        private readonly List<AliasedAttribute> _attributes = new List<AliasedAttribute>();
        private readonly List<AliasedAttribute> _requiredAttributes = new List<AliasedAttribute>();
        private readonly List<EntityFetchResponseMap> _linkedEntities = new List<EntityFetchResponseMap>();

        /// <summary>
        /// Public constructor that creates and owns the readers.
        /// </summary>
        public EntityFetchResponseMap(ITracingService tracingSvc, string fetchXml)
        {
            _tracingSvc = tracingSvc;
            _reader = XmlReader.Create(new StringReader(fetchXml));

            // Advance until the first <entity> element is found.
            while (_reader.Read())
            {
                if (_reader.NodeType != XmlNodeType.Element || _reader.Name != "entity") continue;

                ParseElement();
                break;
            }

            Validate();
        }

        /// <summary>
        /// Private constructor used for recursive parsing of subtrees.
        /// In this case the reader is provided by the parent, so we don't store it.
        /// </summary>
        private EntityFetchResponseMap(
            ITracingService tracingSvc,
            XmlReader reader,
            EntityFetchResponseMap parentMap
        )
        {
            _tracingSvc = tracingSvc;

            // Parse attributes of the current element.
            if (reader.HasAttributes)
            {
                TableName = reader.GetAttribute("name");
                LinkInfo = new LinkInfo(reader);
                PrimaryIdAttribute = new AliasedAttribute(
                    string.IsNullOrWhiteSpace(LinkInfo?.FetchAlias)
                        ? $"{TableName}id"
                        : $"{LinkInfo.FetchAlias}.{TableName}id"
                );
            }

            if (reader.IsEmptyElement) return;

            // Read inner content.
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement &&
                    (reader.Name == "entity" || reader.Name == "link-entity"))
                {
                    break;
                }

                if (reader.NodeType != XmlNodeType.Element) continue;

                switch (reader.Name)
                {
                    case "attribute":
                        var attrName = reader.GetAttribute("name");
                        if (!string.IsNullOrWhiteSpace(attrName))
                        {
                            _attributes.Add(new AliasedAttribute(attrName, LinkInfo?.FetchAlias));

                            if (reader.GetAttribute("required")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)
                            {
                                _requiredAttributes.Add(new AliasedAttribute(attrName, LinkInfo?.FetchAlias));
                            }
                        }

                        if (!reader.IsEmptyElement) reader.Skip();
                        break;
                    case "link-entity":
                        using (var subReader = reader.ReadSubtree())
                        {
                            subReader.Read(); // position at the <link-entity> start element
                            var childMapping = new EntityFetchResponseMap(tracingSvc, subReader, this);
                            _linkedEntities.Add(childMapping);
                        }

                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (_attributes.All(a => a.Name != PrimaryIdAttribute.Name) && _attributes.Any(a => a.Name.Equals("activityid")))
            {
                PrimaryIdAttribute = new AliasedAttribute(
                    string.IsNullOrWhiteSpace(LinkInfo?.FetchAlias) ? "activityid" : $"{LinkInfo.FetchAlias}.activityid"
                );
            }

            LinkInfo?.SetParentMap(parentMap);

            Validate();
        }

        private void Validate()
        {
            if (_attributes.All(a => a.Name != PrimaryIdAttribute.Name))
            {
                var forJoin = string.IsNullOrWhiteSpace(LinkInfo?.FetchAlias)
                    ? ""
                    : $" for '{TableName}'{(string.IsNullOrWhiteSpace(LinkInfo?.FetchAlias) ? "" : $" with alias '{LinkInfo.FetchAlias}'")}";

                throw new InvalidConstraintException(
                    $"Primary Id attribute '{PrimaryIdAttribute.Name}' (or activityid, if applicable) not found in FetchXml attributes{forJoin}"
                );
            }
        }

        // IDisposable implementation.
        public void Dispose()
        {
            _reader?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Parses the current <see cref="Entity"/> element using the owned _reader.
        /// </summary>
        private void ParseElement()
        {
            // At <entity> or <link-entity>, extract attributes.
            if (_reader.HasAttributes)
            {
                TableName = _reader.GetAttribute("name");
                LinkInfo = new LinkInfo(_reader);
                PrimaryIdAttribute = new AliasedAttribute(
                    string.IsNullOrWhiteSpace(LinkInfo?.FetchAlias)
                        ? $"{TableName}id"
                        : $"{LinkInfo.FetchAlias}.{TableName}id"
                );
            }

            if (_reader.IsEmptyElement) return;

            // Process inner elements.
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.EndElement &&
                    (_reader.Name == "entity" || _reader.Name == "link-entity"))
                {
                    break;
                }

                if (_reader.NodeType != XmlNodeType.Element) continue;

                switch (_reader.Name)
                {
                    case "attribute":
                    {
                        var attrName = _reader.GetAttribute("name");
                        if (!string.IsNullOrWhiteSpace(attrName))
                            _attributes.Add(new AliasedAttribute(attrName, LinkInfo?.FetchAlias));
                        if (!_reader.IsEmptyElement) _reader.Skip();
                        break;
                    }
                    case "link-entity":
                    {
                        // For nested link-entities, use ReadSubtree to get an independent reader.
                        using (var subReader = _reader.ReadSubtree())
                        {
                            subReader.Read(); // position on the <link-entity>
                            var childMapping = new EntityFetchResponseMap(_tracingSvc, subReader, this);
                            _linkedEntities.Add(childMapping);
                        }

                        break;
                    }
                    default:
                        _reader.Skip();
                        break;
                }
            }

            if (_attributes.All(a => a.Name != PrimaryIdAttribute.Name) && _attributes.Any(a => a.Name.Equals("activityid")))
            {
                PrimaryIdAttribute = new AliasedAttribute(
                    string.IsNullOrWhiteSpace(LinkInfo?.FetchAlias) ? "activityid" : $"{LinkInfo.FetchAlias}.activityid"
                );
            }

            LinkInfo?.SetParentMap(this);
        }
    }
}