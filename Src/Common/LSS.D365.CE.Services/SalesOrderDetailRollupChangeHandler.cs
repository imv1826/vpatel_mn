using System;
using LSS.D365.CE.Common.ExtensionMethods;
using LSS.D365.CE.DataAccess;
using LSS.D365.CE.Models;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.Services
{
    public class SalesOrderDetailRollupChangeHandler : IRollupEntityChangeHandler
    {
        private readonly IOrganizationService _orgSvc;
        private readonly ITracingService _tracingSvc;

        public SalesOrderDetailRollupChangeHandler(
            IOrganizationService orgSvc,
            ITracingService tracingSvc
        )
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
            if (preImage.LogicalName != SalesOrderDetail.EntityLogicalName)
            {
                throw new InvalidPluginExecutionException(
                    $"Invalid entity type: {preImage.LogicalName}. Expected: {SalesOrderDetail.EntityLogicalName}."
                );
            }

            var salesOrderDetailDal = new SalesOrderDetailDal(_orgSvc, _tracingSvc);

            var orderDetailPreImage = preImage.ToEntity<SalesOrderDetail>();

            if (orderDetailPreImage.SalesOrderId == null ||
                orderDetailPreImage.ProductId == null ||
                orderDetailPreImage.new_DateofServiceorExpense == null)
            {
                _tracingSvc.Trace(
                    "Entity does not have sufficient data to find related service agreements. No recalculation needed."
                );
                return null; // No related service agreements to process
            }

            _tracingSvc.Trace(
                $"Getting related service agreements for Sales Order ID: {orderDetailPreImage.SalesOrderId?.Id}\n,Date of Service/Expense: {orderDetailPreImage.new_DateofServiceorExpense},\nProduct ID: {orderDetailPreImage.ProductId?.Id}"
            );

            var svcAgrmtDetails =
                salesOrderDetailDal.GetRelatedServiceAgreementDetails(
                    orderDetailPreImage.SalesOrderId?.Id,
                    orderDetailPreImage.ProductId?.Id,
                    orderDetailPreImage.new_DateofServiceorExpense
                );

            _tracingSvc.Trace(
                $"Found {svcAgrmtDetails?.Length ?? 0} related service agreements for Sales Order Detail ID: {orderDetailPreImage.Id}"
            );

            if (svcAgrmtDetails == null || svcAgrmtDetails.Length == 0)
            {
                _tracingSvc.Trace("No related service agreements found for the sales order detail.");
                return null; // No related service agreements to process
            }

            return new pics_RecalculateSvcAgrmtDetailSpentToDateRequest
            {
                pics_RecalculateSvcAgrmtDetailSpentToDate_TargetCollection = new EntityCollection(svcAgrmtDetails)
            };
        }
    }
}