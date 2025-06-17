using Microsoft.Xrm.Sdk;

namespace RSM.Integrations.Dataverse.Plugins.ServiceBus
{
    /// <summary>LogDelete Plugin implementation</summary>
    /// <messages>Create, Retrieve, Update, Delete</messages>
    /// <stage>Post-Operation</stage>
    /// <mode>Asynchronous</mode>
    public class LogDelete : PluginBase
    {
        public LogDelete(string unsecureConfiguration, string secureConfiguration) : base(typeof(LogDelete))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            var orgSvc = localPluginContext.PluginUserService;
            var tracingSvc = localPluginContext.TracingService;

            tracingSvc.Trace(
                $"Event - pics_entityid: {localPluginContext.PluginExecutionContext.PrimaryEntityId}\npics_entitylogicalname: {localPluginContext.PluginExecutionContext.PrimaryEntityName}\npics_eventmessagename: {localPluginContext.PluginExecutionContext.MessageName}"
            );

            var eventLog = new Entity("pics_eventlog")
            {
                ["pics_targetentityid"] = localPluginContext.PluginExecutionContext.PrimaryEntityId,
                ["pics_targetentitylogicalname"] = localPluginContext.PluginExecutionContext.PrimaryEntityName,
                ["pics_messagename"] = localPluginContext.PluginExecutionContext.MessageName
            };

            tracingSvc.Trace("Creating event log record...");
            orgSvc.Create(eventLog);
            tracingSvc.Trace("Event log record created successfully.");
        }
    }
}