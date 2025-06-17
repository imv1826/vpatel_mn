using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;

namespace RSM.Integrations.Dataverse.Common
{
    public static class FetchXmlHelper
    {
        public static XDocument PopulateEntityIdAttributes(XDocument fetchXml)
        {
            if (fetchXml == null) return null;
            if (fetchXml.Root == null) throw new InvalidPluginExecutionException("Invalid FetchXml, root element not found");

            var entityName = fetchXml.Root.Element("entity")?.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(entityName)) throw new InvalidPluginExecutionException("Invalid FetchXml");

            var entityIdAttribute = $"{entityName}id";

            // If the root entity does not have the entity id attribute, add it
            if (fetchXml.Root.Descendants("attribute").All(a => a.Attribute("name")?.Value != entityIdAttribute))
            {
                fetchXml.Root.Element("entity")?.Add(new XElement("attribute", new XAttribute("name", entityIdAttribute)));
            }

            // For each link-entity, add the entity id attribute if it does not exist
            foreach (var linkEntity in fetchXml.Root.Descendants("link-entity"))
            {
                var linkEntityName = linkEntity.Attribute("name")?.Value;
                if (string.IsNullOrWhiteSpace(linkEntityName)) continue;

                var linkEntityIdAttribute = $"{linkEntityName}id";
                if (linkEntity.Descendants("attribute").All(a => a.Attribute("name")?.Value != linkEntityIdAttribute))
                {
                    linkEntity.Add(new XElement("attribute", new XAttribute("name", linkEntityIdAttribute)));
                }
            }

            return fetchXml;
        }

        public static XDocument RemoveEntityNonRequiredAttributes(XDocument fetchXml, Entity targetEntity)
        {
            if (fetchXml == null || targetEntity == null) return fetchXml;

            if (fetchXml.Root == null) throw new InvalidPluginExecutionException("Invalid FetchXml");

            RemoveNonRequiredAttributes(fetchXml);
            AddTargetEntityAttributes(targetEntity, fetchXml);

            return fetchXml;
        }

        private static void RemoveNonRequiredAttributes(XDocument fetch)
        {
            fetch.Root?.Descendants("attribute")
                .Where(
                    a => !(a.Attribute("required")
                               ?.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ??
                           false)
                )
                .Remove();
        }

        private static void AddTargetEntityAttributes(Entity targetEntity, XDocument fetch)
        {
            if (fetch.Root == null) return;

            foreach (var attribute in targetEntity.Attributes)
            {
                if (fetch.Root.Descendants("attribute").All(a => a.Attribute("name")?.Value != attribute.Key))
                {
                    fetch.Root.Element("entity")?.Add(new XElement("attribute", new XAttribute("name", attribute.Key)));
                }
            }
        }
    }
}