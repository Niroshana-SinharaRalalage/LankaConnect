using FluentAssertions;
using LankaConnect.Domain.Events.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Enums;

/// <summary>
/// Phase 8X.1 — pin the wire values of <see cref="EventPaymentMode"/>. The values
/// matter because they map directly to the <c>events.events.payment_mode smallint</c>
/// column added in Phase 8X.2 and to the JSON string serialisation consumed by the
/// frontend (Phase 6A.124 — TS enum values must be strings, but the underlying
/// numeric mapping still controls the DB column).
/// </summary>
public class EventPaymentModeTests
{
    [Fact]
    public void Free_Value_Should_Be_Zero()
    {
        ((short)EventPaymentMode.Free).Should().Be(0);
    }

    [Fact]
    public void OnPlatformPaid_Value_Should_Be_One()
    {
        ((short)EventPaymentMode.OnPlatformPaid).Should().Be(1);
    }

    [Fact]
    public void ExternalPaid_Value_Should_Be_Two()
    {
        ((short)EventPaymentMode.ExternalPaid).Should().Be(2);
    }

    [Fact]
    public void Default_Value_Of_New_Variable_Should_Be_Free()
    {
        EventPaymentMode mode = default;
        mode.Should().Be(EventPaymentMode.Free);
    }
}
