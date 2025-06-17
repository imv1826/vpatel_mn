using LSS.D365.CE.Common.ExtensionMethods;
using LSS.D365.CE.Models.ProxyClasses;
using LSS.D365.CE.Tests.Mocks;
using Microsoft.Xrm.Sdk;
using Xunit.Abstractions;

namespace LSS.D365.CE.Tests;

[Collection("Unit")]
public class EntityExtensionsTests
{
    protected readonly ITracingService TracingSvc;
    private readonly ITestOutputHelper _output;

    public EntityExtensionsTests(ITestOutputHelper output)
    {
        TracingSvc = new TracingServiceMock(output ?? throw new ArgumentNullException(nameof(output)));
        _output = output;
    }

    [Fact]
    public void EntityExtensions_IsEqualValues_DetectsValueDifferences_WhenDifferent()
    {
        var mileageEntry1 = new new_mileageentry
        {
            new_ExtendedMileageCost = new Money(5)
        };

        var mileageEntry2 = new new_mileageentry
        {
            new_ExtendedMileageCost = new Money(15),
            new_budgetlineid = new EntityReference("budgetline", Guid.NewGuid())
        };

        var monitoredAttributes = new[]
        {
            "new_budgetlineid",
            "new_ExtendedMileageCost".ToLowerInvariant()
        };

        var isEqual = mileageEntry1.IsEqualValues(
            mileageEntry2,
            monitoredAttributes,
            out var changedAttribute
        );

        _output.WriteLine($"Is Equal: {isEqual}");

        Assert.False(isEqual, "Expected changes to be detected.");
    }

    [Fact]
    public void EntityExtensions_IsEqualValues_DetectsValueDifferences_WhenSame()
    {
        var mileageEntry1 = new new_mileageentry
        {
            new_ExtendedMileageCost = new Money(5)
        };

        var mileageEntry2 = new new_mileageentry
        {
            new_ExtendedMileageCost = new Money(5),
            new_budgetlineid = new EntityReference("budgetline", Guid.NewGuid())
        };

        var monitoredAttributes = new[]
        {
            "new_budgetlineid",
            "new_ExtendedMileageCost".ToLowerInvariant()
        };

        var isEqual = mileageEntry1.IsEqualValues(
            mileageEntry2,
            monitoredAttributes,
            out var changedAttribute
        );

        _output.WriteLine($"Is Equal: {isEqual}");

        Assert.True(isEqual, "Expected changes to be detected.");
    }
}