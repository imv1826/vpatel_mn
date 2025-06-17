using System;
using LSS.D365.CE.Models;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.Services
{
    public static class RollupEntityChangeHandlerFactory
    {
        public static IRollupEntityChangeHandler GetChangeHandler(
            IOrganizationService orgSvc,
            ITracingService tracingSvc,
            string targetName
        )
        {

            switch (targetName)
            {
                case new_expense.EntityLogicalName:
                    return new ExpenseRollupChangeHandler(orgSvc, tracingSvc);
                case new_mileageentry.EntityLogicalName:
                    return new MileageEntryRollupChangeHandler(orgSvc, tracingSvc);
                case new_timeentrydetail.EntityLogicalName:
                    return new TimeEntryDetailRollupChangeHandler(orgSvc, tracingSvc);
                case pics_fee.EntityLogicalName:
                    return new FeeRollupChangeHandler(orgSvc, tracingSvc);
                case SalesOrderDetail.EntityLogicalName:
                    return new SalesOrderDetailRollupChangeHandler(orgSvc, tracingSvc);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(targetName),
                        $"Entity with monitored attribute '{targetName}' is not yet supported for change detection."
                    );
            }
        }
    }
}