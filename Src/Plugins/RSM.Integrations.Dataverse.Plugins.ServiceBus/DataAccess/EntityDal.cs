using System;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RSM.Integrations.Dataverse.Common;
using RSM.Integrations.Dataverse.Models;
using RSM.Integrations.Dataverse.Models.Enums;
using RSM.Integrations.Dataverse.Models.Messages;
using RSM.Integrations.Dataverse.Services;

namespace RSM.Integrations.Dataverse.DataAccess
{
    public class EntityDal : BaseDal
    {
        public EntityDal(IOrganizationService orgSvc, ITracingService tracingSvc = null) : base(orgSvc, tracingSvc)
        {
        }
    }

    public class MessageEntityDal : BaseDal
    {
        public MessageEntityDal(IOrganizationService organizationService, ITracingService tracingService) : base(
            organizationService,
            tracingService
        )
        {
        }

        public string FormatFetchXmlQuery(
            ServiceBusSendEntityConfig svcBusConfig,
            Guid targetEntityId,
            string messageName,
            Entity targetEntity
        )
        {
            var fetchXml = svcBusConfig.FetchXml.Replace("{0}", targetEntityId.ToString());
            var fetchDoc = XDocument.Parse(fetchXml);

            var isModifiedOnly = messageName.Equals("update", StringComparison.OrdinalIgnoreCase) &&
                                 (svcBusConfig.AttributeOptions & MessageAttributeOptions.ModifiedOnly) != 0;

            TracingService?.Trace(
                isModifiedOnly
                    ? "Fetch query will only include modified entity attributes."
                    : "All entity attributes will be included in fetch query."
            );

            fetchDoc = FetchXmlHelper.PopulateEntityIdAttributes(fetchDoc);
            if (!isModifiedOnly) return fetchDoc.ToString();

            fetchDoc = FetchXmlHelper.RemoveEntityNonRequiredAttributes(fetchDoc, targetEntity);
            TracingService?.Trace("Fetch query modified to include only modified entity attributes.");

            return fetchDoc.ToString();
        }

        public EntityCollection RetrieveTargetEntity(string fetchXml, int pageSize = 5000, int maxPages = 10)
        {
            if (fetchXml.Contains("{0}"))
            {
                throw new ArgumentException("FetchXml query must contain a placeholder for the target entity id.");
            }

            return RetrieveAllRecords(fetchXml, pageSize, maxPages);
        }

        public MessageDocument ParseMessageEntity(
            string fetchXml,
            EntityCollection fetchResponse,
            MessageAttributeOptions attributeOptions,
            Entity targetEntity = null
        )
        {
            if (fetchResponse.Entities.Count <= 0) return new MessageDocument();

            using (var parser = new EntityFetchResponseParser(TracingService, fetchXml, attributeOptions))
            {
                return parser.ParseMessageEntity(fetchResponse, targetEntity);
            }
        }
    }

    public abstract class BaseDal
    {
        protected readonly IOrganizationService OrganizationService;
        protected readonly ITracingService TracingService;

        protected BaseDal(IOrganizationService organizationService, ITracingService tracingService = null)
        {
            OrganizationService = organizationService;
            TracingService = tracingService;
        }

        public EntityCollection RetrieveAllRecords(QueryExpression query, int maxPages = 10)
        {
            var queryResponse = OrganizationService.RetrieveMultiple(query);

            if (!queryResponse.MoreRecords) return queryResponse;

            var page = 1;
            while (queryResponse.MoreRecords && page <= maxPages)
            {
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = query.PageInfo.PagingCookie;

                queryResponse = OrganizationService.RetrieveMultiple(query);
                page++;
            }

            return queryResponse;
        }

        public EntityCollection RetrieveAllRecords(string fetchXml, int pageSize = 5000, int maxPages = 10)
        {
            var fetchDoc = XDocument.Parse(fetchXml);

            var queryResponse =
                OrganizationService.RetrieveMultiple(new FetchExpression(SetFetchPaging(fetchDoc, pageSize, 1)));

            if (!queryResponse.MoreRecords) return queryResponse;

            var page = 1;
            while (queryResponse.MoreRecords && page <= maxPages)
            {
                queryResponse = OrganizationService.RetrieveMultiple(
                    new FetchExpression(SetFetchPaging(fetchDoc, pageSize, page, queryResponse))
                );

                page++;
            }

            return queryResponse;
        }

        private static string SetFetchPaging(
            XDocument fetchDoc,
            int pageSize,
            int page,
            EntityCollection queryResponse = null
        )
        {
            fetchDoc.Element("fetch")?.SetAttributeValue("count", pageSize);
            fetchDoc.Element("fetch")?.SetAttributeValue("page", page);

            if (!string.IsNullOrWhiteSpace(queryResponse?.PagingCookie))
            {
                fetchDoc.Element("fetch")?.SetAttributeValue("paging-cookie", queryResponse.PagingCookie);
            }

            return fetchDoc.ToString();
        }
    }
}