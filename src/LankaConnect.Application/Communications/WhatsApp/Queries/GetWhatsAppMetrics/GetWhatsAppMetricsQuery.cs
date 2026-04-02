using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;

namespace LankaConnect.Application.Communications.WhatsApp.Queries.GetWhatsAppMetrics;

/// <summary>
/// Phase 7A.2: Query to get WhatsApp message delivery metrics for a date range.
/// </summary>
public record GetWhatsAppMetricsQuery(DateTime From, DateTime To) : IQuery<WhatsAppMetricsDto>;

/// <summary>
/// DTO representing aggregated WhatsApp message delivery metrics.
/// </summary>
public class WhatsAppMetricsDto
{
    public int TotalSent { get; init; }
    public int TotalDelivered { get; init; }
    public int TotalRead { get; init; }
    public int TotalFailed { get; init; }
    public double DeliveryRate { get; init; }
    public double ReadRate { get; init; }
    public Dictionary<string, int> ByTemplate { get; init; } = new();
    public DateTime From { get; init; }
    public DateTime To { get; init; }
}

/// <summary>
/// Phase 7A.2: Handler for GetWhatsAppMetricsQuery.
/// Retrieves aggregated message metrics from the WhatsApp message repository.
/// </summary>
public class GetWhatsAppMetricsQueryHandler : IRequestHandler<GetWhatsAppMetricsQuery, Result<WhatsAppMetricsDto>>
{
    private readonly IWhatsAppMessageRepository _messageRepository;
    private readonly ILogger<GetWhatsAppMetricsQueryHandler> _logger;

    public GetWhatsAppMetricsQueryHandler(
        IWhatsAppMessageRepository messageRepository,
        ILogger<GetWhatsAppMetricsQueryHandler> logger)
    {
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<Result<WhatsAppMetricsDto>> Handle(
        GetWhatsAppMetricsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetWhatsAppMetrics"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetWhatsAppMetrics START: From={From}, To={To}",
                request.From, request.To);

            try
            {
                if (request.From >= request.To)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetWhatsAppMetrics FAILED: Invalid date range - From={From}, To={To}, Duration={ElapsedMs}ms",
                        request.From, request.To, stopwatch.ElapsedMilliseconds);
                    return Result<WhatsAppMetricsDto>.Failure("'From' date must be before 'To' date.");
                }

                var metrics = await _messageRepository.GetMetricsAsync(request.From, request.To, cancellationToken);

                var dto = new WhatsAppMetricsDto
                {
                    TotalSent = metrics.TotalSent,
                    TotalDelivered = metrics.TotalDelivered,
                    TotalRead = metrics.TotalRead,
                    TotalFailed = metrics.TotalFailed,
                    DeliveryRate = metrics.DeliveryRate,
                    ReadRate = metrics.ReadRate,
                    ByTemplate = metrics.ByTemplate,
                    From = request.From,
                    To = request.To
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetWhatsAppMetrics COMPLETE: From={From}, To={To}, TotalSent={TotalSent}, DeliveryRate={DeliveryRate}%, Duration={ElapsedMs}ms",
                    request.From, request.To, metrics.TotalSent, metrics.DeliveryRate.ToString("F1"), stopwatch.ElapsedMilliseconds);

                return Result<WhatsAppMetricsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetWhatsAppMetrics FAILED: Unexpected error - From={From}, To={To}, Duration={ElapsedMs}ms",
                    request.From, request.To, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
