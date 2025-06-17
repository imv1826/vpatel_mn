using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace LSS.D365.CE.DataAccess
{
    public static class DataAccessUtils
    {
        public static TEntity[] RetrieveAllPages<TEntity>(IOrganizationService orgSvc, QueryExpression query)
            where TEntity : Entity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var allEntities = new List<TEntity>();
            bool moreRecords;

            do
            {
                var response = orgSvc.RetrieveMultiple(query);
                moreRecords = response.MoreRecords;

                allEntities.AddRange(response.Entities.Cast<TEntity>());

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = response.PagingCookie;
            } while (moreRecords);

            return allEntities.ToArray();
        }

        /// <summary>
        /// Retrieves all pages of results for the given FetchExpression using paging cookies.
        /// </summary>
        /// <typeparam name="TEntity">Type of Entity to return.</typeparam>
        /// <param name="orgSvc">The IOrganizationService to use for the query.</param>
        /// <param name="fetchExpression">The FetchExpression defining the query. Must include at least one order on a unique key.</param>
        /// <param name="pageSize">Number of records per page (max 5000).</param>
        /// <returns>Array of all matching entities.</returns>
        public static TEntity[] RetrieveAllPages<TEntity>(
            IOrganizationService orgSvc,
            FetchExpression fetchExpression,
            int pageSize = 5000
        )
            where TEntity : Entity
        {
            if (fetchExpression == null) throw new ArgumentNullException(nameof(fetchExpression));

            if (pageSize <= 0 || pageSize > 5000)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 5000.");

            var fetchXmlRoot = XElement.Parse(fetchExpression.Query);
            var aggregateAttributeValue = fetchXmlRoot.Attribute("aggregate")?.Value.ToLowerInvariant() ?? "";
            if (aggregateAttributeValue != "true" && !fetchXmlRoot.Descendants("order").Any())
            {
                throw new InvalidOperationException(
                    "FetchExpression must include at least one <order> element for consistent paging."
                );
            }

            var allEntities = new List<TEntity>();
            bool moreRecords;
            string pagingCookie = null;
            var page = 1;

            do
            {
                var response = orgSvc.RetrieveMultiple(
                    new FetchExpression(BuildPagedFetchXml(fetchXmlRoot, page, pagingCookie, pageSize))
                );

                allEntities.AddRange(response.Entities.Cast<TEntity>());

                moreRecords = response.MoreRecords;
                pagingCookie = response.PagingCookie;
                page++;
            } while (moreRecords);

            return allEntities.ToArray();
        }

        public static TEntity[] RetrieveAllPagesAsync<TEntity>(IOrganizationService orgSvc, QueryExpression query)
            where TEntity : Entity
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var allEntities = new List<TEntity>();
            bool moreRecords;

            do
            {
                var response = orgSvc.RetrieveMultiple(query);

                moreRecords = response.MoreRecords;

                allEntities.AddRange(response.Entities.Cast<TEntity>());

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = response.PagingCookie;
            } while (moreRecords);

            return allEntities.ToArray();
        }

        private static string BuildPagedFetchXml(XElement fetchXmlRoot, int page, string pagingCookie, int pageSize)
        {
            fetchXmlRoot.SetAttributeValue("count", pageSize);
            fetchXmlRoot.SetAttributeValue("page", page);

            if (!string.IsNullOrWhiteSpace(pagingCookie))
            {
                fetchXmlRoot.SetAttributeValue("paging-cookie", pagingCookie);
            }
            else
            {
                fetchXmlRoot.Attributes("paging-cookie").Remove();
            }

            return fetchXmlRoot.ToString(SaveOptions.DisableFormatting);
        }
    }
}