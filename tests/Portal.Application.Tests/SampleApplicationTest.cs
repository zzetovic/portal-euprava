using FluentAssertions;
using Portal.Application.Interfaces;

namespace Portal.Application.Tests;

public class SampleApplicationTest
{
    [Fact]
    public void ILocalDbAktWriter_Interface_Should_Exist()
    {
        typeof(ILocalDbAktWriter).Should().BeInterface();
    }

    [Fact]
    public void WriteAktCommand_Should_Contain_IdempotencyKey()
    {
        var properties = typeof(WriteAktCommand).GetProperties();
        properties.Select(p => p.Name).Should().Contain("IdempotencyKey",
            "IdempotencyKey is critical for outbox idempotency (CLAUDE.md section 9)");
    }
}
