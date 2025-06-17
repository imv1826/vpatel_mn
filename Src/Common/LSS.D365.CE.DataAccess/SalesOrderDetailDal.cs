using System;
using System.Linq;
using LSS.D365.CE.Common.ExtensionMethods;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace LSS.D365.CE.DataAccess
{
    public class SalesOrderDetailDal : BaseDal
    {
        public SalesOrderDetailDal(IOrganizationService orgSvc, ITracingService tracingSvc) : base(orgSvc, tracingSvc)
        {
        }

        public new_serviceagreementdetail[] GetRelatedServiceAgreementDetails(
            Guid? orderId,
            Guid? productId,
            DateTime? dateOfService
        )
        {
            const string svcAgrmtDetailsAlias = new_serviceagreementdetail.EntityLogicalName;
            var svcAgrmtDetailIdAlias =
                $"{svcAgrmtDetailsAlias}.{new_serviceagreementdetail.Fields.new_serviceagreementdetailId}";

            ValidateArgs(orderId, productId, dateOfService);

            var query = new QueryExpression(SalesOrder.EntityLogicalName)
            {
                NoLock = true,
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            SalesOrder.Fields.SalesOrderId,
                            ConditionOperator.Equal,
                            orderId
                        )
                    }
                },
                LinkEntities =
                {
                    new LinkEntity(
                        SalesOrder.EntityLogicalName,
                        new_serviceagreement.EntityLogicalName,
                        SalesOrder.Fields.new_ParentServiceAgreement,
                        new_serviceagreement.Fields.new_serviceagreementId,
                        JoinOperator.Inner
                    )
                    {
                        EntityAlias = new_serviceagreement.EntityLogicalName,
                        LinkEntities =
                        {
                            new LinkEntity(
                                new_serviceagreement.EntityLogicalName,
                                new_serviceagreementdetail.EntityLogicalName,
                                new_serviceagreement.Fields.new_serviceagreementId,
                                new_serviceagreementdetail.Fields.new_serviceagreementid,
                                JoinOperator.Inner
                            )
                            {
                                EntityAlias = svcAgrmtDetailsAlias,
                                Columns = new ColumnSet(
                                    new_serviceagreementdetail.Fields.new_serviceagreementdetailId,
                                    new_serviceagreementdetail.Fields.new_serviceagreementid,
                                    new_serviceagreementdetail.Fields.new_StartDate,
                                    new_serviceagreementdetail.Fields.new_EndDate,
                                    new_serviceagreementdetail.Fields.new_serviceexpenseid
                                ),
                                LinkCriteria =
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression(
                                            new_serviceagreementdetail.Fields.new_serviceexpenseid,
                                            ConditionOperator.Equal,
                                            productId
                                        ),
                                        new ConditionExpression(
                                            new_serviceagreementdetail.Fields.new_StartDate,
                                            ConditionOperator.LessEqual,
                                            dateOfService
                                        ),
                                        new ConditionExpression(
                                            new_serviceagreementdetail.Fields.new_EndDate,
                                            ConditionOperator.GreaterEqual,
                                            dateOfService
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var res = DataAccessUtils.RetrieveAllPages<Entity>(OrgSvc, query);

            return res
                .GroupBy(e =>
                    e.TryGetAttributeValue(svcAgrmtDetailIdAlias, out AliasedValue serviceAgreementDetailId)
                        ? serviceAgreementDetailId.Value as Guid?
                        : null
                )
                .Select(g =>
                    {
                        var detail = g.FirstOrDefault();
                        var detailId = g.Key ?? Guid.Empty;
                        if (detail == null || detailId == Guid.Empty) return null;

                        return new new_serviceagreementdetail
                        {
                            Id = detailId,
                            new_serviceagreementdetailId = detail.GetAliasedValue(svcAgrmtDetailIdAlias)?.Value as Guid?,
                            new_serviceagreementid = detail.GetAliasedValue(
                                    $"{svcAgrmtDetailsAlias}.{new_serviceagreementdetail.Fields.new_serviceagreementid}"
                                )
                                ?.Value as EntityReference,
                            new_StartDate = detail.GetAliasedValue(
                                    $"{svcAgrmtDetailsAlias}.{new_serviceagreementdetail.Fields.new_StartDate}"
                                )
                                ?.Value as DateTime?,
                            new_EndDate = detail.GetAliasedValue(
                                    $"{svcAgrmtDetailsAlias}.{new_serviceagreementdetail.Fields.new_EndDate}"
                                )
                                ?.Value as DateTime?,
                            new_serviceexpenseid = detail.GetAliasedValue(
                                    $"{svcAgrmtDetailsAlias}.{new_serviceagreementdetail.Fields.new_serviceexpenseid}"
                                )
                                ?.Value as EntityReference
                        };
                    }
                )
                .Where(detail => detail != null)
                .ToArray();
        }

        private void ValidateArgs(Guid? orderId, Guid? productId, DateTime? dateOfService)
        {
            if (orderId == null || orderId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(orderId), "Order ID cannot be null or empty.");
            }

            if (productId == null || productId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(productId), "Product ID cannot be null or empty.");
            }

            if (dateOfService == null)
            {
                throw new ArgumentNullException(nameof(dateOfService), "Date of Service cannot be null.");
            }
        }
    }
}