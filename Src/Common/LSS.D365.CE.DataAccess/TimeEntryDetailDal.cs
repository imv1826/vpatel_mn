using System;
using System.Linq;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace LSS.D365.CE.DataAccess
{
    public class TimeEntryDetailDal : BaseDal
    {
        public TimeEntryDetailDal(IOrganizationService orgSvc, ITracingService tracingSvc) : base(orgSvc, tracingSvc)
        {
        }

        public new_serviceagreementdetail GetRelatedServiceAgreementDetails(Guid? employeeBudgetDetailId)
        {
            if (employeeBudgetDetailId == null) return null;

            const string sadAlias = new_serviceagreementdetail.EntityLogicalName;
            var sadIdAlias = $"{sadAlias}.{new_serviceagreementdetail.Fields.new_serviceagreementdetailId}";

            var query = new QueryExpression(pics_employeebudgetdetail.EntityLogicalName)
            {
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            pics_employeebudgetdetail.Fields.pics_employeebudgetdetailId,
                            ConditionOperator.Equal,
                            employeeBudgetDetailId
                        )
                    }
                },
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
                        EntityAlias = sadAlias,
                        Columns = new ColumnSet(
                            new_serviceagreementdetail.Fields.new_EndDate,
                            new_serviceagreementdetail.Fields.new_serviceagreementdetailId,
                            new_serviceagreementdetail.Fields.new_serviceagreementid,
                            new_serviceagreementdetail.Fields.new_serviceexpenseid,
                            new_serviceagreementdetail.Fields.new_StartDate
                        )
                    }
                }
            };

            var result = DataAccessUtils.RetrieveAllPages<pics_employeebudgetdetail>(OrgSvc, query);
            if (result == null || result.Length == 0)
            {
                TracingSvc.Trace(
                    $"No service agreement details found for employee budget detail ID: {employeeBudgetDetailId}"
                );
                return null;
            }

            return result.GroupBy(r => r.TryGetAttributeValue(sadIdAlias, out AliasedValue sadId) ? sadId.Value : Guid.Empty)
                .Select(g =>
                    {
                        var firstRecord = g.FirstOrDefault();
                        if (firstRecord == null) return null;
                        var id = firstRecord.TryGetAttributeValue(sadIdAlias, out AliasedValue sadIdValue)
                            ? sadIdValue.Value as Guid?
                            : null;
                        if (id == null)
                        {
                            TracingSvc.Trace(
                                $"Service agreement detail ID not found for employee budget detail ID: {employeeBudgetDetailId}"
                            );
                            return null;
                        }

                        return new new_serviceagreementdetail
                        {
                            new_serviceagreementdetailId = id,
                            new_serviceagreementid = firstRecord.TryGetAttributeValue(
                                $"{sadAlias}.{new_serviceagreementdetail.Fields.new_serviceagreementid}",
                                out AliasedValue saId
                            )
                                ? saId.Value as EntityReference
                                : null,
                            new_StartDate = firstRecord.TryGetAttributeValue(
                                $"{sadAlias}.{new_serviceagreementdetail.Fields.new_StartDate}",
                                out AliasedValue startDate
                            )
                                ? startDate.Value as DateTime?
                                : null,
                            new_EndDate = firstRecord.TryGetAttributeValue(
                                $"{sadAlias}.{new_serviceagreementdetail.Fields.new_EndDate}",
                                out AliasedValue endDate
                            )
                                ? endDate.Value as DateTime?
                                : null,
                            new_serviceexpenseid = firstRecord.TryGetAttributeValue(
                                $"{sadAlias}.{new_serviceagreementdetail.Fields.new_serviceexpenseid}",
                                out AliasedValue seId
                            )
                                ? seId.Value as EntityReference
                                : null
                        };
                    }
                )
                .FirstOrDefault();
        }
    }
}