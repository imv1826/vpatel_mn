using Microsoft.Xrm.Sdk;
using Xunit.Abstractions;

namespace LSS.D365.CE.Tests.Mocks;

public class TracingServiceMock : ITracingService
{
    private readonly ITestOutputHelper _output;

    public TracingServiceMock(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void Trace(string format, params object[] args)
    {
        _output.WriteLine(format, args);
    }
}