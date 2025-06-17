using LSS.D365.CE.DataAccess;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.Services
{
    public class ServiceAgreementDetailRollupService
    {
        private readonly IOrganizationService _orgSvc;
        private readonly ITracingService _tracingSvc;

        public ServiceAgreementDetailRollupService(
            IOrganizationService orgSvc,
            ITracingService tracingSvc
        )
        {
            _orgSvc = orgSvc;
            _tracingSvc = tracingSvc;
        }

        public void RecalculateTotalSpent(EntityCollection svcAgrmtDetailTargets)
        {
            foreach (var svcAgrmtDetailReceived in svcAgrmtDetailTargets.Entities)
            {
                if (svcAgrmtDetailReceived.LogicalName != new_serviceagreementdetail.EntityLogicalName)
                {
                    throw new InvalidPluginExecutionException(
                        $"Invalid target entity logical name: {svcAgrmtDetailReceived.LogicalName}. Expected: {new_serviceagreementdetail.EntityLogicalName}"
                    );
                }

                if (svcAgrmtDetailReceived == null)
                {
                    throw new InvalidPluginExecutionException(
                        $"Service Agreement Detail with ID '{svcAgrmtDetailReceived.Id}' not found."
                    );
                }

                var svcAgrmtDetailDal = new ServiceAgreementDetailDal(_orgSvc, _tracingSvc);

                var spentToDate =
                    svcAgrmtDetailDal.GetTotalSpentRollupAmount(
                        svcAgrmtDetailReceived.ToEntity<new_serviceagreementdetail>()
                    );

                _orgSvc.Update(
                    new Entity(svcAgrmtDetailReceived.LogicalName, svcAgrmtDetailReceived.Id)
                    {
                        [new_serviceagreementdetail.Fields.new_SpenttoDate] = new Money(spentToDate)
                    }
                );

                _tracingSvc.Trace(
                    $"Recalculated 'Spent to Date' for Service Agreement Detail ID '{svcAgrmtDetailReceived.Id}': {spentToDate}"
                );
            }
        }
    }
}