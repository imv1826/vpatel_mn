using System.Linq;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

// ReSharper disable MemberCanBePrivate.Global

namespace LSS.D365.CE.DataAccess
{
    public class ServiceAgreementDetailDal : BaseDal
    {
        public ServiceAgreementDetailDal(IOrganizationService orgSvc, ITracingService tracingSvc) : base(orgSvc, tracingSvc)
        {
        }

        public decimal GetTotalSpentRollupAmount(new_serviceagreementdetail svcAgrmtDetail)
        {
            return GetRelatedOrderDetailsAmountsSum(svcAgrmtDetail) +
                   GetRelatedExpenseAmountsSum(svcAgrmtDetail) +
                   GetRelatedMileageEntryAmountsSum(svcAgrmtDetail) +
                   GetRelatedTEDAmountsSum(svcAgrmtDetail) +
                   GetRelatedFeesSum(svcAgrmtDetail);
        }

        public decimal GetRelatedOrderDetailsAmountsSum(new_serviceagreementdetail svcAgrmtDetail)
        {
            const string extendedAmountSumAlias = "claims_extendedamount";

            var detailsQuery = new QueryExpression(SalesOrder.EntityLogicalName)
            {
                NoLock = true,
                Criteria =
                {
                    Conditions =
                    {
                        svcAgrmtDetail.new_serviceagreementid == null
                            ? new ConditionExpression(SalesOrder.Fields.new_ParentServiceAgreement, ConditionOperator.Null)
                            : new ConditionExpression(
                                SalesOrder.Fields.new_ParentServiceAgreement,
                                ConditionOperator.Equal,
                                svcAgrmtDetail.new_serviceagreementid?.Id
                            )
                    }
                },
                LinkEntities =
                {
                    new LinkEntity(
                        SalesOrder.EntityLogicalName,
                        SalesOrderDetail.EntityLogicalName,
                        SalesOrderDetail.Fields.SalesOrderId,
                        SalesOrder.Fields.SalesOrderId,
                        JoinOperator.Inner
                    )
                    {
                        EntityAlias = SalesOrder.Fields.order_details,
                        Columns = new ColumnSet
                        {
                            AttributeExpressions = new DataCollection<XrmAttributeExpression>(1)
                            {
                                new XrmAttributeExpression(
                                    attributeName: SalesOrderDetail.Fields.ExtendedAmount,
                                    alias: extendedAmountSumAlias,
                                    aggregateType: XrmAggregateType.Sum
                                )
                            }
                        },
                        LinkCriteria =
                        {
                            FilterOperator = LogicalOperator.And,
                            Conditions =
                            {
                                new ConditionExpression(
                                    SalesOrderDetail.Fields.new_DateofServiceorExpense,
                                    ConditionOperator.GreaterEqual,
                                    svcAgrmtDetail.new_StartDate
                                ),
                                new ConditionExpression(
                                    SalesOrderDetail.Fields.new_DateofServiceorExpense,
                                    ConditionOperator.LessEqual,
                                    svcAgrmtDetail.new_EndDate
                                ),
                                svcAgrmtDetail.new_serviceexpenseid == null
                                    ? new ConditionExpression(SalesOrderDetail.Fields.ProductId, ConditionOperator.Null)
                                    : new ConditionExpression(
                                        SalesOrderDetail.Fields.ProductId,
                                        ConditionOperator.Equal,
                                        svcAgrmtDetail.new_serviceexpenseid?.Id
                                    )
                            }
                        }
                    }
                }
            };

            var aggregateResponse = DataAccessUtils.RetrieveAllPages<Entity>(OrgSvc, detailsQuery);
            var firstRow = aggregateResponse.FirstOrDefault();
            if (firstRow == null)
            {
                TracingSvc.Trace("No related order details found for Service Agreement Detail ID: {0}", svcAgrmtDetail.Id);
                return 0;
            }

            if (firstRow.TryGetAttributeValue(extendedAmountSumAlias, out AliasedValue aggregateSumAliased) &&
                aggregateSumAliased.Value is Money aggregateSum)
            {
                var totalValue = aggregateSum.Value;

                TracingSvc.Trace(
                    "Total Order Details for Service Agreement Detail: {0}",
                    totalValue
                );

                return totalValue;
            }

            TracingSvc.Trace("Total Order Details for Service Agreement Detail: {0}", 0);
            return 0;
        }

        public decimal GetRelatedExpenseAmountsSum(new_serviceagreementdetail svcAgrmtDetail)
        {
            const string totalAmountSumAlias = "sum_totalamount";
            const string feeSumAlias = "sum_fee";

            var query = new QueryExpression(new_expense.EntityLogicalName)
            {
                NoLock = true,
                ColumnSet = new ColumnSet
                {
                    AttributeExpressions = new DataCollection<XrmAttributeExpression>(2)
                    {
                        new XrmAttributeExpression(
                            attributeName: new_expense.Fields.pics_TotalAmount,
                            alias: totalAmountSumAlias,
                            aggregateType: XrmAggregateType.Sum
                        ),
                        new XrmAttributeExpression(
                            attributeName: new_expense.Fields.pics_Fee,
                            alias: feeSumAlias,
                            aggregateType: XrmAggregateType.Sum
                        ),
                    }
                },
                Criteria =
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression(
                            new_expense.Fields.new_budgetlineid,
                            ConditionOperator.Equal,
                            svcAgrmtDetail.Id
                        ),
                        new ConditionExpression(
                            new_expense.Fields.statecode,
                            ConditionOperator.Equal,
                            (int)new_expense_statecode.Active
                        )
                    }
                }
            };

            var aggregateResponse = DataAccessUtils.RetrieveAllPages<Entity>(OrgSvc, query);
            if (aggregateResponse.Length == 0)
            {
                TracingSvc.Trace("No related expenses found for Service Agreement Detail");
                return 0;
            }

            var totals = aggregateResponse.Sum(exp =>
                             exp.TryGetAttributeValue(totalAmountSumAlias, out AliasedValue totalAliased)
                                 ? totalAliased.Value is Money totalMoney ? totalMoney.Value : 0M
                                 : 0M
                         ) +
                         aggregateResponse.Sum(exp =>
                             exp.TryGetAttributeValue(feeSumAlias, out AliasedValue totalAliased)
                                 ? totalAliased.Value is Money totalMoney ? totalMoney.Value : 0M
                                 : 0M
                         );

            TracingSvc.Trace("Total Expenses for Service Agreement Detail: {0}", totals);
            return totals;
        }

        public decimal GetRelatedMileageEntryAmountsSum(new_serviceagreementdetail svcAgrmtDetail)
        {
            const string extendedMileageCostSumAlias = "sum_extendedmileagecost";
            const string feeSumAlias = "sum_fee";

            var query = new QueryExpression(new_mileageentry.EntityLogicalName)
            {
                NoLock = true,
                ColumnSet = new ColumnSet
                {
                    AttributeExpressions = new DataCollection<XrmAttributeExpression>(2)
                    {
                        new XrmAttributeExpression(
                            attributeName: new_mileageentry.Fields.new_ExtendedMileageCost,
                            alias: extendedMileageCostSumAlias,
                            aggregateType: XrmAggregateType.Sum
                        ),
                        new XrmAttributeExpression(
                            attributeName: new_mileageentry.Fields.pics_Fee,
                            alias: feeSumAlias,
                            aggregateType: XrmAggregateType.Sum
                        ),
                    }
                },
                Criteria =
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression(
                            new_expense.Fields.new_budgetlineid,
                            ConditionOperator.Equal,
                            svcAgrmtDetail.Id
                        ),
                        new ConditionExpression(
                            new_mileageentry.Fields.statecode,
                            ConditionOperator.Equal,
                            (int)new_mileageentry_statecode.Active
                        )
                    }
                }
            };

            var aggregateResponse = DataAccessUtils.RetrieveAllPages<Entity>(OrgSvc, query);
            if (aggregateResponse.Length == 0)
            {
                TracingSvc.Trace("No related Mileage Entries found for Service Agreement Detail");
                return 0;
            }

            var totals = aggregateResponse.Sum(exp =>
                             exp.TryGetAttributeValue(extendedMileageCostSumAlias, out AliasedValue totalAliased)
                                 ? totalAliased.Value is Money totalMoney ? totalMoney.Value : 0M
                                 : 0M
                         ) +
                         aggregateResponse.Sum(exp =>
                             exp.TryGetAttributeValue(feeSumAlias, out AliasedValue totalAliased)
                                 ? totalAliased.Value is Money totalMoney ? totalMoney.Value : 0M
                                 : 0M
                         );

            TracingSvc.Trace("Total Mileage Entries for Service Agreement Detail: {0}", totals);
            return totals;
        }

        public decimal GetRelatedTEDAmountsSum(new_serviceagreementdetail svcAgrmtDetail)
        {
            const string totalCostSumAlias = "sum_totalcost";
            const string feeSumAlias = "sum_fee";

            var query = new QueryExpression(new_timeentrydetail.EntityLogicalName)
            {
                NoLock = true,
                ColumnSet = new ColumnSet
                {
                    AttributeExpressions = new DataCollection<XrmAttributeExpression>(2)
                    {
                        new XrmAttributeExpression(
                            attributeName: new_timeentrydetail.Fields.new_TotalCost,
                            alias: totalCostSumAlias,
                            aggregateType: XrmAggregateType.Sum
                        ),
                        new XrmAttributeExpression(
                            attributeName: new_timeentrydetail.Fields.pics_Fee,
                            alias: feeSumAlias,
                            aggregateType: XrmAggregateType.Sum
                        ),
                    }
                },
                Criteria =
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression(
                            new_expense.Fields.new_budgetlineid,
                            ConditionOperator.Equal,
                            svcAgrmtDetail.Id
                        ),
                        new ConditionExpression(
                            new_timeentrydetail.Fields.statecode,
                            ConditionOperator.Equal,
                            (int)new_timeentrydetail_statecode.Active
                        )
                    }
                },
                LinkEntities =
                {
                    new LinkEntity(
                        new_timeentrydetail.EntityLogicalName,
                        pics_employeebudgetdetail.EntityLogicalName,
                        new_timeentrydetail.Fields.pics_EEBudgetDetailId,
                        pics_employeebudgetdetail.Fields.pics_employeebudgetdetailId,
                        JoinOperator.Inner
                    )
                    {
                        EntityAlias = pics_employeebudgetdetail.EntityLogicalName,
                        LinkEntities =
                        {
                            new LinkEntity(
                                pics_employeebudgetdetail.EntityLogicalName,
                                new_serviceagreementdetail.EntityLogicalName,
                                pics_employeebudgetdetail.Fields.pics_SABudgetDetailId,
                                new_serviceagreementdetail.Fields.new_serviceagreementdetailId,
                                JoinOperator.Inner
                            )
                            {
                                EntityAlias = new_serviceagreementdetail.EntityLogicalName,
                                LinkCriteria =
                                {
                                    FilterOperator = LogicalOperator.And,
                                    Conditions =
                                    {
                                        new ConditionExpression(
                                            new_serviceagreementdetail.Fields.new_serviceagreementdetailId,
                                            ConditionOperator.Equal,
                                            svcAgrmtDetail.Id
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var aggregateResponse = DataAccessUtils.RetrieveAllPages<Entity>(OrgSvc, query);
            if (aggregateResponse.Length == 0)
            {
                TracingSvc.Trace("No related TEDs found for Service Agreement Detail");
                return 0;
            }

            var totals = aggregateResponse.Sum(exp =>
                             exp.TryGetAttributeValue(totalCostSumAlias, out AliasedValue totalAliased)
                                 ? totalAliased.Value is Money totalMoney ? totalMoney.Value : 0M
                                 : 0M
                         ) +
                         aggregateResponse.Sum(exp =>
                             exp.TryGetAttributeValue(feeSumAlias, out AliasedValue totalAliased)
                                 ? totalAliased.Value is Money totalMoney ? totalMoney.Value : 0M
                                 : 0M
                         );

            TracingSvc.Trace("Total TEDs for Service Agreement Detail: {0}", totals);
            return totals;
        }

        public decimal GetRelatedFeesSum(new_serviceagreementdetail svcAgrmtDetail)
        {
            const string feeAmountSumAlias = "sum_amount";

            var query = new QueryExpression(pics_fee.EntityLogicalName)
            {
                NoLock = true,
                ColumnSet = new ColumnSet
                {
                    AttributeExpressions = new DataCollection<XrmAttributeExpression>(2)
                    {
                        new XrmAttributeExpression(
                            attributeName: pics_fee.Fields.pics_Amount,
                            alias: feeAmountSumAlias,
                            aggregateType: XrmAggregateType.Sum
                        )
                    }
                },
                Criteria =
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression(
                            pics_fee.Fields.pics_SADetailId,
                            ConditionOperator.Equal,
                            svcAgrmtDetail.Id
                        )
                    }
                }
            };

            var aggregateResponse = DataAccessUtils.RetrieveAllPages<Entity>(OrgSvc, query);
            if (aggregateResponse.Length == 0)
            {
                TracingSvc.Trace("No related Fees found for Service Agreement Detail");
                return 0;
            }

            var totals = aggregateResponse.Sum(exp =>
                exp.TryGetAttributeValue(feeAmountSumAlias, out AliasedValue totalAliased)
                    ? totalAliased.Value is Money totalMoney ? totalMoney.Value : 0M
                    : 0M
            );

            TracingSvc.Trace("Total Fees for Service Agreement Detail: {0}", totals);
            return totals;
        }
    }
}