using System;
using System.Linq;
using LSS.D365.CE.Models;
using LSS.D365.CE.Services;
using Microsoft.Xrm.Sdk;
using RSM.Integrations.Dataverse.Plugins.ServiceBus;

namespace LSS.Xrm.CustomApis.ServiceAgreementDetailRecalc
{
    public class RecalculateSpentToDate : PluginBase
    {
        public RecalculateSpentToDate(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(DetectChanges))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null) throw new ArgumentNullException(nameof(localPluginContext));

            var rollupService = new ServiceAgreementDetailRollupService(
                localPluginContext.PluginUserService,
                localPluginContext.TracingService
            );

            var svcAgrmtDetailTargets = localPluginContext.GetInputParam<EntityCollection>(
                "pics_RecalculateSvcAgrmtDetailSpentToDate_TargetCollection",
                true
            );

            rollupService.RecalculateTotalSpent(svcAgrmtDetailTargets);

            localPluginContext.TracingService.Trace(
                $"SpentToDate recalculation completed for {svcAgrmtDetailTargets.Entities.Count} records: {string.Join(", ", svcAgrmtDetailTargets.Entities.Select(e => e.Id))}."
            );
        }
    }
}