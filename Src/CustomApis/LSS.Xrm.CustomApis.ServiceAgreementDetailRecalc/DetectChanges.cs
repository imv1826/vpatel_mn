using System;
using System.Linq;
using LSS.D365.CE.Models;
using LSS.D365.CE.Models.ProxyClasses;
using LSS.D365.CE.Services;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using RSM.Integrations.Dataverse.Plugins.ServiceBus;

namespace LSS.Xrm.CustomApis.ServiceAgreementDetailRecalc
{
    public class DetectChanges : PluginBase
    {
        private readonly string _unsecureConfig;

        public DetectChanges(string unsecureConfiguration) : base(typeof(DetectChanges))
        {
            _unsecureConfig = unsecureConfiguration;
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null) throw new ArgumentNullException(nameof(localPluginContext));

            var orgSvc = localPluginContext.PluginUserService;
            var tracingSvc = localPluginContext.TracingService;

            var targetRef = localPluginContext.PluginTargetRef;
            var eventMessageName = localPluginContext.PluginExecutionContext.MessageName.ToLowerInvariant();

            var changeHandler = RollupEntityChangeHandlerFactory.GetChangeHandler(
                orgSvc,
                tracingSvc,
                targetRef.LogicalName
            );

            pics_RecalculateSvcAgrmtDetailSpentToDateRequest recalcRequest;

            if (!localPluginContext.TryGetPluginPreImage("PreImage", out Entity preImage))
            {
                if (eventMessageName != "create" && eventMessageName != "delete")
                {
                    throw new InvalidPluginExecutionException("PreImage is required");
                }

                tracingSvc.Trace("Building recalculation request");
                recalcRequest = changeHandler.BuildRecalcRequest(localPluginContext.PluginTargetEntity);
                if (recalcRequest == null)
                {
                    tracingSvc.Trace(
                        $"No recalculation request built for entity '{targetRef.LogicalName}' with ID '{targetRef.Id}'."
                    );

                    return; // No recalculation
                }

                orgSvc.Execute(recalcRequest);
            }

            if (eventMessageName == "update")
            {
                var config = JsonConvert.DeserializeObject<ChangeDetectConfig>(_unsecureConfig) ??
                             throw new InvalidPluginExecutionException("Failed to parse config");
            
                config.SourceAttributesMonitored =
                    config.SourceAttributesMonitored.Select(a => a.Trim().ToLowerInvariant()).ToArray();
            
                if (!changeHandler.HasChanges(config, preImage, localPluginContext.PluginTargetEntity))
                {
                    tracingSvc.Trace(
                        $"No changes detected for entity '{targetRef.LogicalName}' with ID '{targetRef.Id}'."
                    );
            
                    return;
                }
            }

            tracingSvc.Trace(
                $"Changes detected for entity '{targetRef.LogicalName}' with ID '{targetRef.Id}'. Building recalculation request."
            );

            recalcRequest = changeHandler.BuildRecalcRequest(preImage);
            if (recalcRequest == null)
            {
                tracingSvc.Trace(
                    $"No recalculation request built for entity '{targetRef.LogicalName}' with ID '{targetRef.Id}'."
                );

                return; // No recalculation needed
            }

            orgSvc.Execute(recalcRequest);
        }
    }
}