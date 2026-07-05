using System.Reflection;
using LankaConnect.Modules.Payments.Contracts;

namespace LankaConnect.Modules.Payments.Contracts.Tests;

/// <summary>
/// Shape-pinning tests for <see cref="IPaymentQueries"/>,
/// <see cref="IPaymentCommands"/>, the 4 DTOs, and the 2 enums.
/// Wave 4.4.a (2026-06-23). Purpose: catch silent ABI drift - if the
/// cross-module surface changes accidentally, the post-4.4.d.1
/// LankaConnect.Application consumers (EventCancellationEmailJob,
/// CancelRsvpCommandHandler, StripeWebhookHandler) would break.
/// </summary>
public sealed class IPaymentQueriesShapeTests
{
    [Fact]
    public void IPaymentQueries_Has_EightMethods()
    {
        // Wave 4.4.b scope correction (2026-06-23): original 4.4.a draft pinned
        // 9 methods including a speculative GetStripeCustomerAsync. Dropped per
        // YAGNI -- legacy IStripeCustomerRepository has no full-record query
        // and no real consumer was identified in the 4.4.a survey.
        var methods = typeof(IPaymentQueries).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "GetRefundRequestByIdAsync",
            "GetRefundRequestWithLineItemsAsync",
            "GetByRegistrationAsync",
            "GetMyMostRecentForEventAsync",
            "ListByEventAsync",
            "ListStuckApprovedAsync",
            "GetStripeCustomerIdAsync",
            "IsWebhookEventProcessedAsync",
        });
    }

    [Fact]
    public void IPaymentCommands_Is_EmptyMarker_AtWave44a()
    {
        // Concrete methods are added in Wave 4.4.d.1 after the
        // LankaConnect.Application audit identifies which cross-module mutator
        // paths need to go through Contracts. If the 4.4.d.1 audit finds zero,
        // this interface is removed in 4.4.d.3.
        var methods = typeof(IPaymentCommands).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        methods.Should().BeEmpty();
    }

    [Fact]
    public void RefundRequestSummaryDto_Carries_CoreFields()
    {
        var props = typeof(RefundRequestSummaryDto).GetProperties();

        props.Select(p => p.Name).Should().BeEquivalentTo(new[]
        {
            nameof(RefundRequestSummaryDto.Id),
            nameof(RefundRequestSummaryDto.RegistrationId),
            nameof(RefundRequestSummaryDto.RequestedByUserId),
            nameof(RefundRequestSummaryDto.IsOrganizerInitiated),
            nameof(RefundRequestSummaryDto.RequestedAt),
            nameof(RefundRequestSummaryDto.Status),
            nameof(RefundRequestSummaryDto.RequesterReason),
            nameof(RefundRequestSummaryDto.ReviewedByUserId),
            nameof(RefundRequestSummaryDto.ReviewedAt),
            nameof(RefundRequestSummaryDto.OrganizerNotes),
            nameof(RefundRequestSummaryDto.RejectionReason),
            nameof(RefundRequestSummaryDto.CompletedAt),
            nameof(RefundRequestSummaryDto.LineItemCount),
            nameof(RefundRequestSummaryDto.IsActive),
        });
    }

    [Fact]
    public void RefundRequestDetailDto_Carries_LineItems_AsReadOnlyList()
    {
        var lineItemsProp = typeof(RefundRequestDetailDto).GetProperty(nameof(RefundRequestDetailDto.LineItems));
        lineItemsProp.Should().NotBeNull();
        lineItemsProp!.PropertyType.Should().Be(typeof(IReadOnlyList<RefundLineItemDto>));
    }

    [Fact]
    public void RefundLineItemDto_Money_Is_Projected_To_Primitives()
    {
        // Architect ruling (plan §"Contract surface"): Money projects to
        // (decimal Amount + string Currency) instead of carrying
        // LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects.Money directly - Contracts
        // must not pull LankaConnect.Domain.
        var props = typeof(RefundLineItemDto).GetProperties();

        props.Should().Contain(p => p.Name == nameof(RefundLineItemDto.RequestedAmount) && p.PropertyType == typeof(decimal));
        props.Should().Contain(p => p.Name == nameof(RefundLineItemDto.RequestedCurrency) && p.PropertyType == typeof(string));
        props.Should().Contain(p => p.Name == nameof(RefundLineItemDto.ApprovedAmount) && p.PropertyType == typeof(decimal?));
        props.Should().Contain(p => p.Name == nameof(RefundLineItemDto.ApprovedCurrency) && p.PropertyType == typeof(string));
    }

    [Fact]
    public void RefundRequestStatusDto_Mirrors_DomainStatus_ByteWidth()
    {
        // Mapping is 1:1 on the underlying byte value - a pure cast at the
        // PaymentContractMappings seam (Wave 4.4.b). If the byte values drift,
        // this test fires and the mapping breaks silently.
        Enum.GetUnderlyingType(typeof(RefundRequestStatusDto)).Should().Be(typeof(byte));

        ((byte)RefundRequestStatusDto.Pending).Should().Be(0);
        ((byte)RefundRequestStatusDto.Approved).Should().Be(1);
        ((byte)RefundRequestStatusDto.Processing).Should().Be(2);
        ((byte)RefundRequestStatusDto.Completed).Should().Be(3);
        ((byte)RefundRequestStatusDto.Rejected).Should().Be(4);
        ((byte)RefundRequestStatusDto.Withdrawn).Should().Be(5);
    }

    [Fact]
    public void RefundLineItemTypeDto_Mirrors_DomainType_ByteWidth()
    {
        Enum.GetUnderlyingType(typeof(RefundLineItemTypeDto)).Should().Be(typeof(byte));

        ((byte)RefundLineItemTypeDto.Ticket).Should().Be(0);
        ((byte)RefundLineItemTypeDto.AddOn).Should().Be(1);
        ((byte)RefundLineItemTypeDto.Collection).Should().Be(2);
        ((byte)RefundLineItemTypeDto.Sponsor).Should().Be(3);
    }

}
