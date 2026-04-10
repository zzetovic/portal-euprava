using FluentAssertions;
using Portal.Domain.Enums;

namespace Portal.Domain.Tests;

public class SampleDomainTest
{
    [Fact]
    public void RequestStatus_Should_Include_ProcessingRegistry()
    {
        var values = Enum.GetNames<RequestStatus>();

        values.Should().Contain("ProcessingRegistry",
            "ProcessingRegistry is a required internal intermediate status for outbox idempotency");
    }
}
