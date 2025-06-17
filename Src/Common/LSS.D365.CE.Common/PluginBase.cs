using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.PluginTelemetry;

namespace RSM.Integrations.Dataverse.Plugins.ServiceBus
{
    /// <summary>
    /// Base class for all plug-in classes.
    /// Plugin development guide: https://docs.microsoft.com/powerapps/developer/common-data-service/plug-ins
    /// Best practices and guidance: https://docs.microsoft.com/powerapps/developer/common-data-service/best-practices/business-logic/
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        protected string PluginClassName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginBase"/> class.
        /// </summary>
        /// <param name="pluginClassName">The <see cref="Type"/> of the plugin class.</param>
        internal PluginBase(Type pluginClassName)
        {
            PluginClassName = pluginClassName.ToString();
        }

        /// <summary>
        /// Main entry point for the business logic that the plug-in is to execute.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// </remarks>
        [SuppressMessage(
            "Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters",
            Justification = "Execute"
        )]
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new InvalidPluginExecutionException(nameof(serviceProvider));

            // Construct the local plug-in context.
            var localPluginContext = new LocalPluginContext(serviceProvider);

            localPluginContext.TracingService.Trace(
                $"Entered {PluginClassName}.Execute()\nCorrelation Id: {localPluginContext.PluginExecutionContext.CorrelationId},\nInitiating User: {localPluginContext.PluginExecutionContext.InitiatingUserId}\n"
            );

            try
            {
                // Invoke the custom implementation
                ExecuteDataversePlugin(localPluginContext);

                // Now exit - if the derived plugin has incorrectly registered overlapping event registrations, guard against multiple executions.
                // ReSharper disable once RedundantJumpStatement
                return;
            }
            catch (Exception ex)
            {
                localPluginContext.TracingService.Trace($"Error: {ex}");

                throw new InvalidPluginExecutionException(
                    $"Error: {ex.Message}",
                    ex
                );
            }
            finally
            {
                localPluginContext.TracingService.Trace($"Exiting {PluginClassName}.Execute()");
            }
        }

        /// <summary>
        /// Placeholder for a custom plug-in implementation.
        /// </summary>
        /// <param name="localPluginContext">Context for the current plug-in.</param>
        protected virtual void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            // Do nothing.
        }
    }

    /// <summary>
    /// This interface provides an abstraction on top of IServiceProvider for commonly used PowerPlatform Dataverse Plugin development constructs
    /// </summary>
    public interface ILocalPluginContext
    {
        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Current Executing user.
        /// </summary>
        IOrganizationService InitiatingUserService { get; }

        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Account that was registered to run this plugin, This could be the same user as InitiatingUserService.
        /// </summary>
        IOrganizationService PluginUserService { get; }

        /// <summary>
        /// IPluginExecutionContext contains information that describes the run-time environment in which the plug-in executes, information related to the execution pipeline, and entity business information.
        /// </summary>
        IPluginExecutionContext PluginExecutionContext { get; }

        /// <summary>
        /// Synchronous registered plug-ins can post the execution context to the Microsoft Azure Service Bus. <br/>
        /// It is through this notification service that synchronous plug-ins can send brokered messages to the Microsoft Azure Service Bus.
        /// </summary>
        IServiceEndpointNotificationService NotificationService { get; }

        /// <summary>
        /// Provides logging run-time trace information for plug-ins.
        /// </summary>
        ITracingService TracingService { get; }

        /// <summary>
        /// General Service Provide for things not accounted for in the base class.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// OrganizationService Factory for creating connection for other then current user and system.
        /// </summary>
        IOrganizationServiceFactory OrgSvcFactory { get; }

        /// <summary>
        /// ILogger for this plugin.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// The target entity reference for the plugin.
        /// </summary>
        EntityReference PluginTargetRef { get; }

        /// <summary>
        /// The target entity for the plugin, if the target is an Entity.
        /// </summary>
        Entity PluginTargetEntity { get; }

        /// <summary>
        /// Safely retrieves an input parameter from the plugin execution context.
        /// </summary>
        /// <returns>The value of the input parameter if found</returns>
        /// <exception cref="InvalidPluginExecutionException">Thrown if the parameter is required but not found.</exception>
        TValue GetInputParam<TValue>(string paramName, bool isRequired = false);

        bool TryGetPluginPreImage<TEntity>(string imageName, out TEntity image) where TEntity : Entity;

        bool TryGetPluginPostImage<TEntity>(string imageName, out TEntity image) where TEntity : Entity;
    }

    /// <summary>
    /// Plug-in context object.
    /// </summary>
    public class LocalPluginContext : ILocalPluginContext
    {
        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Current Executing user.
        /// </summary>
        public IOrganizationService InitiatingUserService { get; }

        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Account that was registered to run this plugin, This could be the same user as InitiatingUserService.
        /// </summary>
        public IOrganizationService PluginUserService { get; }

        /// <summary>
        /// IPluginExecutionContext contains information that describes the run-time environment in which the plug-in executes, information related to the execution pipeline, and entity business information.
        /// </summary>
        public IPluginExecutionContext PluginExecutionContext { get; }

        /// <summary>
        /// Synchronous registered plug-ins can post the execution context to the Microsoft Azure Service Bus. <br/>
        /// It is through this notification service that synchronous plug-ins can send brokered messages to the Microsoft Azure Service Bus.
        /// </summary>
        public IServiceEndpointNotificationService NotificationService { get; }

        /// <summary>
        /// Provides logging run-time trace information for plug-ins.
        /// </summary>
        public ITracingService TracingService { get; }

        /// <summary>
        /// General Service Provider for things not accounted for in the base class.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// OrganizationService Factory for creating connection for other then current user and system.
        /// </summary>
        public IOrganizationServiceFactory OrgSvcFactory { get; }

        /// <summary>
        /// ILogger for this plugin.
        /// </summary>
        public ILogger Logger { get; }

        public EntityReference PluginTargetRef { get; }

        public Entity PluginTargetEntity { get; }

        /// <summary>
        /// Helper object that stores the services available in this plug-in.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public LocalPluginContext(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new InvalidPluginExecutionException(nameof(serviceProvider));
            }

            ServiceProvider = serviceProvider;

            Logger = serviceProvider.Get<ILogger>();

            PluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();

            TracingService = new LocalTracingService(serviceProvider);

            NotificationService = serviceProvider.Get<IServiceEndpointNotificationService>();

            OrgSvcFactory = serviceProvider.Get<IOrganizationServiceFactory>();

            PluginUserService =
                serviceProvider.GetOrganizationService(
                    PluginExecutionContext.UserId
                ); // User that the plugin is registered to run as, Could be same as current user.

            InitiatingUserService =
                serviceProvider.GetOrganizationService(
                    PluginExecutionContext.InitiatingUserId
                ); //User whose action called the plugin.

            if (PluginExecutionContext.InputParameters.TryGetValue("Target", out var target))
            {
                PluginTargetEntity = target as Entity;
                PluginTargetRef = target as EntityReference ?? PluginTargetEntity?.ToEntityReference();
            }
            else
            {
            }

            TracingService.Trace($"Plugin Target Entity Reference: {PluginTargetRef?.LogicalName} - {PluginTargetRef?.Id}");
            TracingService.Trace($"Plugin Target Entity: {PluginTargetEntity?.LogicalName}");

            if (PluginExecutionContext.PrimaryEntityId != Guid.Empty &&
                !string.IsNullOrWhiteSpace(PluginExecutionContext.PrimaryEntityName))
            {
                PluginTargetRef = new EntityReference(
                    PluginExecutionContext.PrimaryEntityName,
                    PluginExecutionContext.PrimaryEntityId
                );
            }
        }

        /// <inheritdoc/>
        public TValue GetInputParam<TValue>(string paramName, bool isRequired = false)
        {
            var hasParam = PluginExecutionContext.InputParameters.TryGetValue(paramName, out TValue paramValue);
            if (hasParam) return paramValue;

            TracingService.Trace($"Input param '{paramName}' not found.");

            if (isRequired)
            {
                throw new InvalidPluginExecutionException(
                    $"Parameter '{paramName}' is required, but not provided."
                );
            }

            return default;
        }

        public bool TryGetPluginPreImage<TEntity>(string imageName, out TEntity image) where TEntity : Entity
        {
            if (PluginExecutionContext.PreEntityImages.TryGetValue(imageName, out var entity))
            {
                image = entity.ToEntity<TEntity>();
                return true;
            }

            image = null;
            return false;
        }

        public bool TryGetPluginPostImage<TEntity>(string imageName, out TEntity image) where TEntity : Entity
        {
            if (PluginExecutionContext.PostEntityImages.TryGetValue(imageName, out var entity))
            {
                image = entity.ToEntity<TEntity>();
                return true;
            }

            image = null;
            return false;
        }
    }

    /// <summary>
    /// Specialized ITracingService implementation that prefixes all traced messages with a time delta for Plugin performance diagnostics
    /// </summary>
    public class LocalTracingService : ITracingService
    {
        private readonly ITracingService _tracingService;

        private DateTime _previousTraceTime;

        public LocalTracingService(IServiceProvider serviceProvider)
        {
            DateTime utcNow = DateTime.UtcNow;

            var context = (IExecutionContext)serviceProvider.GetService(typeof(IExecutionContext));

            DateTime initialTimestamp = context.OperationCreatedOn;

            if (initialTimestamp > utcNow)
            {
                initialTimestamp = utcNow;
            }

            _tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            _previousTraceTime = initialTimestamp;
        }

        public void Trace(string message, params object[] args)
        {
            var utcNow = DateTime.UtcNow;

            _tracingService.Trace(
                $"[+{utcNow.Subtract(_previousTraceTime).TotalMilliseconds:N0}ms] - {string.Format(message, args)}"
            );

            _previousTraceTime = utcNow;
        }
    }
}