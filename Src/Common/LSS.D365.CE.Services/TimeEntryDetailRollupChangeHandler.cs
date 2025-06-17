using System;
using LSS.D365.CE.Common.ExtensionMethods;
using LSS.D365.CE.DataAccess;
using LSS.D365.CE.Models;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.Services
{
    public class TimeEntryDetailRollupChangeHandler : IRollupEntityChangeHandler
    {
        private readonly IOrganizationService _orgSvc;
        private readonly ITracingService _tracingSvc;

        public TimeEntryDetailRollupChangeHandler(IOrganizationService orgSvc, ITracingService tracingSvc)
        {
            _orgSvc = orgSvc ?? throw new ArgumentNullException(nameof(orgSvc), "Organization Service cannot be null.");
            _tracingSvc = tracingSvc ??
                          throw new ArgumentNullException(nameof(tracingSvc), "Tracing Service cannot be null.");
        }

        public bool HasChanges(ChangeDetectConfig config, Entity preImage, Entity messageTarget)
        {
            if (messageTarget == null)
            {
                // On Delete messages entity is unavailable
                return true;
            }

            if (preImage == null) throw new ArgumentNullException(nameof(preImage), "Pre-image cannot be null.");
            if (config == null) throw new ArgumentNullException(nameof(config), "ChangeDetectConfig cannot be null.");

            _tracingSvc.Trace($"PreImage attributes included: {string.Join(", ", preImage.Attributes.Keys)}");
            _tracingSvc.Trace($"MessageTarget attributes included: {string.Join(", ", messageTarget.Attributes.Keys)}");

            if (preImage.IsEqualValues(messageTarget, config.SourceAttributesMonitored, out var kvp)) return false;

            var (preValue, currentValue) = kvp.Value;

            _tracingSvc.Trace(
                $"Change detected for attribute '{kvp.Key}': Pre-image value = {preValue}, Current value = {currentValue}"
            );

            return true; // Changes detected in monitored attributes
        }

        public pics_RecalculateSvcAgrmtDetailSpentToDateRequest BuildRecalcRequest(Entity preImage)
        {
            var tedDal = new TimeEntryDetailDal(_orgSvc, _tracingSvc);

            var ted = preImage.ToEntity<new_timeentrydetail>();

            var sad = tedDal.GetRelatedServiceAgreementDetails(ted.pics_EEBudgetDetailId.Id);
            if (sad == null)
            {
                _tracingSvc.Trace(
                    $"No related Service Agreement Detail found for Time Entry Detail ID: {ted.Id}."
                );
                return null;
            }

            _tracingSvc.Trace(
                $"Building Recalc Request for Service Agreement Detail ID: {sad.new_serviceagreementdetailId}."
            );

            return new pics_RecalculateSvcAgrmtDetailSpentToDateRequest
            {
                pics_RecalculateSvcAgrmtDetailSpentToDate_TargetCollection = new EntityCollection(
                    new Entity[]
                    {
                        sad
                    }
                )
            };
        }
    }
}