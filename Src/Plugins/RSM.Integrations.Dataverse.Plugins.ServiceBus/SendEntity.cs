using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using RSM.Integrations.Dataverse.Common.ExtensionMethods;
using RSM.Integrations.Dataverse.DataAccess;
using RSM.Integrations.Dataverse.Models;
using RSM.Integrations.Dataverse.Models.Enums;
using RSM.Integrations.Dataverse.Models.Messages;
using RSM.Integrations.Dataverse.Services;

namespace RSM.Integrations.Dataverse.Plugins.ServiceBus
{
    /// <summary>SendEntity Plugin implementation</summary>
    /// <messages>Create, Update</messages>
    /// <stage>Post-Operation</stage>
    /// <mode>Asynchronous</mode>
    public class SendEntity : PluginBase
    {
        private readonly string _secureConfig;

        public SendEntity(string unsecureConfiguration, string secureConfiguration) : base(typeof(SendEntity))
        {
            _secureConfig = secureConfiguration;
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            var tracingSvc = localPluginContext.TracingService;

            tracingSvc.Trace("Executing SendEntity plugin with config: {0}...", _secureConfig.Substring(0, 50));

            ValidateContext(localPluginContext);

            tracingSvc.Trace(
                $"Target Entity attributes included: {string.Join(", ", localPluginContext.PluginTargetEntity?.Attributes.Keys ?? Array.Empty<string>())}"
            );

            // TODO: Consider moving ServiceBusSendEntityConfig to a custom entity table, retrieve and cache for performance with a timeout for refresh
            if (!_secureConfig.TryDeserialize(out ServiceBusSendEntityConfig svcBusConfig, tracingSvc))
            {
                throw new InvalidPluginExecutionException("Secure configuration is invalid");
            }

            if (svcBusConfig.IgnoreChangesFrom == localPluginContext.PluginExecutionContext.InitiatingUserId)
            {
                tracingSvc.Trace("Ignoring changes from user: {0}", svcBusConfig.IgnoreChangesFrom);
                return;
            }

            var document = GetEventTarget(localPluginContext, svcBusConfig);
            if (document == null)
            {
                tracingSvc.Trace("No target entity found. Exiting...");
                return;
            }

            tracingSvc.Trace("Building Service Bus message...");
            var svcBusMessage = new ServiceBusEntityMessage(localPluginContext.PluginExecutionContext.OperationCreatedOn)
            {
                MessageName = localPluginContext.PluginExecutionContext.MessageName,
                LogLevel = svcBusConfig.LogLevel.ToString(),
                TableName = localPluginContext.PluginTargetRef.LogicalName,
                Document = document
            };

            tracingSvc.Trace("Serializing Service Bus message...");
            var svcBusMessageSerialized = ServiceBusService.GetSerializedEntityMessage(svcBusMessage);
            if (string.IsNullOrWhiteSpace(svcBusMessageSerialized))
            {
                tracingSvc.Trace("No records returned from fetch query. Exiting...");
                return;
            }

            tracingSvc.Trace("Service Bus message serialized to JSON: {0}...", svcBusMessageSerialized.Substring(0, 50));

            tracingSvc.Trace("Creating Service Bus message service...");
            using (var serviceBusService = new ServiceBusService(tracingSvc, svcBusConfig))
            {
                tracingSvc.Trace("Sending entity data to Service Bus queue...");
                serviceBusService.SendMessage(
                    svcBusMessageSerialized,
                    localPluginContext.PluginExecutionContext.CorrelationId
                );
                tracingSvc.Trace("Entity data sent to Service Bus queue.");
            }
        }

        private static MessageDocument GetEventTarget(ILocalPluginContext pluginCtx, ServiceBusSendEntityConfig svcBusConfig)
        {
            var tracingSvc = pluginCtx.TracingService;
            var targetRef = pluginCtx.PluginTargetRef;
            var targetEntity = pluginCtx.PluginTargetEntity;

            if (!string.IsNullOrWhiteSpace(svcBusConfig.FetchXml))
            {
                return GetParsedMessageEntity(pluginCtx, svcBusConfig, tracingSvc, targetRef, targetEntity);
            }

            tracingSvc.Trace("Config FetchXml not found. No query will be executed.");
            var message = new MessageDocument();
            if (targetEntity == null) // This is a delete operation
            {
                message.Attributes = new Dictionary<string, object>(1) { { $"{targetRef.LogicalName}id", targetRef.Id } };
                return message;
            }

            var dictionaryAttrs = targetEntity.Attributes.OrderBy(a => a.Key).ToParsedDictionary();

            if ((svcBusConfig.AttributeOptions & MessageAttributeOptions.Formatted) != 0)
            {
                var formattedDict = targetEntity.FormattedValues.OrderBy(f => f.Key)
                    .ToParsedFormattedDictionary(targetEntity.Attributes);

                tracingSvc.Trace(
                    $"Including formatted values in message attributes: {string.Join(",", formattedDict.Keys)}"
                );

                dictionaryAttrs.AddRange(formattedDict);
            }
            else
            {
                tracingSvc.Trace("Formatted values are excluded from message attributes");
            }

            message.Attributes = dictionaryAttrs;

            return message;
        }

        private static MessageDocument GetParsedMessageEntity(
            ILocalPluginContext pluginCtx,
            ServiceBusSendEntityConfig svcBusConfig,
            ITracingService tracingSvc,
            EntityReference targetRef,
            Entity targetEntity
        )
        {
            var entityDal = new MessageEntityDal(pluginCtx.PluginUserService, tracingSvc);

            var formattedFetch = entityDal.FormatFetchXmlQuery(
                svcBusConfig,
                targetRef.Id,
                pluginCtx.PluginExecutionContext.MessageName,
                targetEntity
            );

            tracingSvc.Trace($"Formatted FetchXml:\t{formattedFetch}");

            var targetFetchResponse = entityDal.RetrieveTargetEntity(formattedFetch);
            tracingSvc.Trace($"{targetFetchResponse.Entities.Count} records returned from fetch query");

            if (targetFetchResponse?.Entities == null || targetFetchResponse.Entities.Count < 1)
            {
                tracingSvc.Trace("No records returned from fetch query");
                return null;
            }

            return entityDal.ParseMessageEntity(
                formattedFetch,
                targetFetchResponse,
                svcBusConfig.AttributeOptions,
                targetEntity
            );
        }

        private static void ValidateContext(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null) throw new ArgumentNullException(nameof(localPluginContext));

            switch (localPluginContext.PluginExecutionContext.MessageName.Trim().ToLowerInvariant())
            {
                case "create":
                case "update":
                case "delete":
                    break;
                default:
                    throw new InvalidPluginExecutionException(
                        $"Invalid message name for {nameof(SendEntity)} plugin: {localPluginContext.PluginExecutionContext.MessageName}"
                    );
            }

            if (localPluginContext.PluginExecutionContext.Stage != 40)
            {
                localPluginContext.TracingService.Trace(
                    $"{nameof(SendEntity)} plugin is recommended to run in Post-Operation stage for best performance."
                );
            }
        }
    }
}