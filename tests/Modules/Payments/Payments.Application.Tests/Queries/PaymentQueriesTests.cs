using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using LankaConnect.Modules.Payments.Application.Queries;
using LankaConnect.Modules.Payments.Contracts;
using Moq;

namespace LankaConnect.Modules.Payments.Application.Tests.Queries;

/// <summary>
/// Unit tests for <see cref="PaymentQueries"/> -- the cross-module Contracts
/// adapter over the legacy refund + Stripe repositories. Wave 4.4.b (2026-06-23).
/// Tests pin the projection logic and the no-id short-circuits.
/// </summary>
public sealed class PaymentQueriesTests
{
    private readonly Mock<IRefundRequestRepository> _refundRepo = new();
    private readonly Mock<IStripeCustomerRepository> _stripeCustomerRepo = new();
    private readonly Mock<IStripeWebhookEventRepository> _webhookRepo = new();
    private readonly PaymentQueries _sut;

    public PaymentQueriesTests()
    {
        _sut = new PaymentQueries(_refundRepo.Object, _stripeCustomerRepo.Object, _webhookRepo.Object);
    }

    private static RefundRequest CreateSampleRequest(
        Guid? id = null,
        Guid? registrationId = null,
        bool addLineItem = true)
    {
        var regId = registrationId ?? Guid.NewGuid();
        var userId = Guid.NewGuid();
        var money = Money.Create(50m, Currency.USD).Value;
        var lineItems = addLineItem
            ? new List<RefundRequestLineItemInput>
            {
                new(RefundLineItemType.Ticket, Guid.NewGuid(), money),
            }
            : new List<RefundRequestLineItemInput>();

        var result = RefundRequest.CreatePending(regId, userId, "reason", lineItems);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task GetRefundRequestByIdAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        _refundRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefundRequest?)null);

        var result = await _sut.GetRefundRequestByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRefundRequestByIdAsync_ProjectsToSummaryDto()
    {
        var req = CreateSampleRequest();
        _refundRepo.Setup(r => r.GetByIdAsync(req.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(req);

        var result = await _sut.GetRefundRequestByIdAsync(req.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(req.Id);
        result.RegistrationId.Should().Be(req.RegistrationId);
        result.Status.Should().Be(RefundRequestStatusDto.Pending);
        result.LineItemCount.Should().Be(1);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetRefundRequestWithLineItemsAsync_ProjectsLineItems_And_PinsMoneyToPrimitives()
    {
        var req = CreateSampleRequest();
        _refundRepo.Setup(r => r.GetByIdAsync(req.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(req);

        var result = await _sut.GetRefundRequestWithLineItemsAsync(req.Id);

        result.Should().NotBeNull();
        result!.LineItems.Should().HaveCount(1);

        var line = result.LineItems[0];
        line.Type.Should().Be(RefundLineItemTypeDto.Ticket);
        line.RequestedAmount.Should().Be(50m);
        line.RequestedCurrency.Should().Be("USD");
        line.ApprovedAmount.Should().BeNull(); // Requested state -> no approved amount yet
    }

    [Fact]
    public async Task GetByRegistrationAsync_MapsAllRequests()
    {
        var regId = Guid.NewGuid();
        var r1 = CreateSampleRequest(registrationId: regId);
        var r2 = CreateSampleRequest(registrationId: regId);
        _refundRepo.Setup(r => r.GetByRegistrationIdAsync(regId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefundRequest> { r1, r2 });

        var result = await _sut.GetByRegistrationAsync(regId);

        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().BeEquivalentTo(new[] { r1.Id, r2.Id });
    }

    [Fact]
    public async Task ListByEventAsync_TranslatesStatusFilter_DtoToDomain()
    {
        var eventId = Guid.NewGuid();
        _refundRepo.Setup(r => r.ListByEventAsync(eventId, RefundRequestStatus.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefundRequest>());

        var result = await _sut.ListByEventAsync(eventId, RefundRequestStatusDto.Approved);

        result.Should().BeEmpty();
        _refundRepo.Verify(r => r.ListByEventAsync(eventId, RefundRequestStatus.Approved, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListByEventAsync_PassesNull_WhenStatusFilterIsNull()
    {
        var eventId = Guid.NewGuid();
        _refundRepo.Setup(r => r.ListByEventAsync(eventId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefundRequest>());

        await _sut.ListByEventAsync(eventId, statusFilter: null);

        _refundRepo.Verify(r => r.ListByEventAsync(eventId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStripeCustomerIdAsync_PassesThrough_ToRepository()
    {
        var userId = Guid.NewGuid();
        _stripeCustomerRepo.Setup(r => r.GetStripeCustomerIdByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("cus_abc123");

        var result = await _sut.GetStripeCustomerIdAsync(userId);

        result.Should().Be("cus_abc123");
    }

    [Fact]
    public async Task IsWebhookEventProcessedAsync_PassesThrough_ToRepository()
    {
        _webhookRepo.Setup(r => r.IsEventProcessedAsync("evt_xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.IsWebhookEventProcessedAsync("evt_xyz");

        result.Should().BeTrue();
        _webhookRepo.Verify(r => r.IsEventProcessedAsync("evt_xyz", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyMostRecentForEventAsync_ProjectsToSummaryDto()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var req = CreateSampleRequest();
        _refundRepo.Setup(r => r.GetMyMostRecentForEventAsync(eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(req);

        var result = await _sut.GetMyMostRecentForEventAsync(eventId, userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(req.Id);
    }
}
