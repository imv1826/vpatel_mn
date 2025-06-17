using LSS.D365.CE.Tests.Mocks;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Xunit.Abstractions;

namespace LSS.D365.CE.Tests;

public abstract class BaseD365IntegrationTest
{
    protected readonly IOrganizationService OrgSvc;
    protected readonly ITracingService TracingSvc;

    public BaseD365IntegrationTest(ITestOutputHelper output)
    {
        var connectionString =
            $"AuthType=ClientSecret;Url={Environment.GetEnvironmentVariable("urlDryRun2")};TenantId={Environment.GetEnvironmentVariable("tenantId")};ClientId={Environment.GetEnvironmentVariable("clientId")};ClientSecret={Environment.GetEnvironmentVariable("clientSecret")};";

        var svcClient = new ServiceClient(connectionString);
        OrgSvc = svcClient ?? throw new InvalidOperationException("Failed to create ServiceClient");

        TracingSvc = new TracingServiceMock(output ?? throw new ArgumentNullException(nameof(output)));
        TracingSvc.Trace($"Connected to {svcClient.ConnectedOrgFriendlyName}\n");
    }
}