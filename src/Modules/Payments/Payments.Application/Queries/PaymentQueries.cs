using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Modules.Payments.Application.Mappings;
using LankaConnect.Modules.Payments.Contracts;
namespace LankaConnect.Modules.Payments.Application.Queries;

/// <summary>
/// Implementation of <see cref="IPaymentQueries"/>. Wave 4.4.b (2026-06-23).
/// Thin adapter over the legacy <see cref="IRefundRequestRepository"/> +
/// <see cref="IStripeCustomerRepository"/> + <see cref="IStripeWebhookEventRepository"/>
/// that projects Domain aggregates to Contracts DTOs.
/// </summary>
/// <remarks>
/// Per architect Risk #1 Option A ruling (2026-06-23), the wrapped Refund repository
/// signature continues returning <c>LankaConnect.Products.LankaEvents.Domain.Entities.RefundRequest</c>
/// indefinitely. The transitional <c>Payments.Application -&gt; LankaConnect.Domain</c>
/// ProjectReference in Payments.Application.csproj is what makes this wiring compile;
/// it is PERMANENT structural compromise for this wave, not transitional.
/// </remarks>
public sealed class PaymentQueries : IPaymentQueries
{
    private readonly IRefundRequestRepository _refundRepository;
    private readonly IStripeCustomerRepository _stripeCustomerRepository;
    private readonly IStripeWebhookEventRepository _webhookEventRepository;

    public PaymentQueries(
        IRefundRequestRepository refundRepository,
        IStripeCustomerRepository stripeCustomerRepository,
        IStripeWebhookEventRepository webhookEventRepository)
    {
        _refundRepository = refundRepository;
        _stripeCustomerRepository = stripeCustomerRepository;
        _webhookEventRepository = webhookEventRepository;
    }

    public async Task<RefundRequestSummaryDto?> GetRefundRequestByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var request = await _refundRepository.GetByIdAsync(id, ct);
        return request?.ToSummaryDto();
    }

    public async Task<RefundRequestDetailDto?> GetRefundRequestWithLineItemsAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var request = await _refundRepository.GetByIdAsync(id, ct);
        return request?.ToDetailDto();
    }

    public async Task<IReadOnlyList<RefundRequestSummaryDto>> GetByRegistrationAsync(
        Guid registrationId,
        CancellationToken ct = default)
    {
        var requests = await _refundRepository.GetByRegistrationIdAsync(registrationId, ct);
        var result = new List<RefundRequestSummaryDto>(requests.Count);
        foreach (var req in requests)
        {
            result.Add(req.ToSummaryDto());
        }
        return result;
    }

    public async Task<RefundRequestSummaryDto?> GetMyMostRecentForEventAsync(
        Guid eventId,
        Guid userId,
        CancellationToken ct = default)
    {
        var request = await _refundRepository.GetMyMostRecentForEventAsync(eventId, userId, ct);
        return request?.ToSummaryDto();
    }

    public async Task<IReadOnlyList<RefundRequestSummaryDto>> ListByEventAsync(
        Guid eventId,
        RefundRequestStatusDto? statusFilter,
        CancellationToken ct = default)
    {
        var domainFilter = statusFilter?.FromDto();
        var requests = await _refundRepository.ListByEventAsync(eventId, domainFilter, ct);
        var result = new List<RefundRequestSummaryDto>(requests.Count);
        foreach (var req in requests)
        {
            result.Add(req.ToSummaryDto());
        }
        return result;
    }

    public async Task<IReadOnlyList<RefundRequestSummaryDto>> ListStuckApprovedAsync(
        DateTime olderThanUtc,
        CancellationToken ct = default)
    {
        var requests = await _refundRepository.ListStuckApprovedAsync(olderThanUtc, ct);
        var result = new List<RefundRequestSummaryDto>(requests.Count);
        foreach (var req in requests)
        {
            result.Add(req.ToSummaryDto());
        }
        return result;
    }

    public Task<string?> GetStripeCustomerIdAsync(Guid userId, CancellationToken ct = default) =>
        _stripeCustomerRepository.GetStripeCustomerIdByUserIdAsync(userId, ct);

    public Task<bool> IsWebhookEventProcessedAsync(string stripeEventId, CancellationToken ct = default) =>
        _webhookEventRepository.IsEventProcessedAsync(stripeEventId, ct);
}
