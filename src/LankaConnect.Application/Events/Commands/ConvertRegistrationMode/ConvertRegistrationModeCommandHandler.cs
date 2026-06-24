using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Text.Json;

namespace LankaConnect.Application.Events.Commands.ConvertRegistrationMode;

/// <summary>
/// Phase 7F-B (architect-approved 2026-04-30): handler for the registration-mode conversion
/// command. Wires up the per-aggregate work the domain method needs:
/// - Resolve the organiser identity (current user) for the audit row.
/// - Query <c>RegistrationAddition</c> rows in pending state — the domain rejects matching
///   registrations to prevent mid-payment mode flips (architect Q8).
/// - Call <see cref="Event.ConvertRegistrationMode"/>, then on success persist the audit
///   tables (one aggregate row + one row per migrated/skipped registration).
/// - When <c>DryRun</c> is true, the audit aggregate row is recorded with a "preview" flag
///   in the OutcomeReason equivalent (<c>FailedCount</c> stays 0 and the rows table is
///   skipped — preview shouldn't pollute the audit).
/// </summary>
public class ConvertRegistrationModeCommandHandler
    : ICommandHandler<ConvertRegistrationModeCommand, ConvertRegistrationModeResult>
{
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationDbContext _db;
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConvertRegistrationModeCommandHandler> _logger;

    public ConvertRegistrationModeCommandHandler(
        IEventRepository eventRepository,
        IApplicationDbContext db,
        IRegistrationAdditionRepository additionRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        ILogger<ConvertRegistrationModeCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _db = db;
        _additionRepository = additionRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ConvertRegistrationModeResult>> Handle(
        ConvertRegistrationModeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ConvertRegistrationMode"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("TargetMode", request.TargetMode))
        using (LogContext.PushProperty("DryRun", request.DryRun))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[7F-B] ConvertRegistrationMode START — EventId={EventId} TargetMode={TargetMode} DryRun={DryRun}",
                request.EventId, request.TargetMode, request.DryRun);

            try
            {
                // 1. Authenticated organiser required.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
                    return Result<ConvertRegistrationModeResult>.Failure("Authentication required");

                // 2. Load the event with change tracking so domain mutations propagate to EF.
                var @event = await _eventRepository.GetByIdAsync(
                    request.EventId, trackChanges: !request.DryRun, cancellationToken);
                if (@event == null)
                    return Result<ConvertRegistrationModeResult>.Failure("Event not found");

                // 3. Authorisation — only the organiser can convert mode.
                if (@event.OrganizerId != _currentUser.UserId)
                {
                    _logger.LogWarning(
                        "[7F-B] ConvertRegistrationMode REJECTED — caller {CallerId} is not organiser {OrganizerId}",
                        _currentUser.UserId, @event.OrganizerId);
                    return Result<ConvertRegistrationModeResult>.Failure(
                        "Only the event organiser can change the registration mode.");
                }

                // 4. Architect Q8: query pending RegistrationAddition rows so the domain can
                //    skip matching registrations.
                var pendingAdditionRegIds = await _db.RegistrationAdditions
                    .Where(a => a.EventId == request.EventId
                                && (a.Status == RegistrationAdditionStatus.Pending
                                    || a.Status == RegistrationAdditionStatus.PaymentCompleted))
                    .Select(a => a.RegistrationId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "[7F-B] Pending additions count={Count}", pendingAdditionRegIds.Count);

                // 5. Build the policy and call domain.
                var policy = new ConversionPolicy
                {
                    OrganiserId = _currentUser.UserId,
                    DryRun = request.DryRun,
                    RegistrationIdsWithPendingAdditions = pendingAdditionRegIds.ToHashSet(),
                };

                var startedAt = DateTime.UtcNow;
                var fromMode = @event.RegistrationMode;
                var convertResult = @event.ConvertRegistrationMode(request.TargetMode, policy);
                if (convertResult.IsFailure)
                {
                    _logger.LogWarning(
                        "[7F-B] ConvertRegistrationMode REJECTED — Errors: {Errors}",
                        string.Join("; ", convertResult.Errors!));
                    return Result<ConvertRegistrationModeResult>.Failure(convertResult.Errors!);
                }

                var report = convertResult.Value!;
                var completedAt = DateTime.UtcNow;

                // 6. Persist audit + commit (only when NOT a dry run).
                Guid? aggregateId = null;
                if (!request.DryRun)
                {
                    aggregateId = await PersistAuditAsync(
                        request.EventId, _currentUser.UserId,
                        fromMode, request.TargetMode,
                        startedAt, completedAt, report, cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);
                    _logger.LogInformation(
                        "[7F-B] ConvertRegistrationMode COMMITTED — EventId={EventId} Migrated={Migrated} Skipped={Skipped} AggregateId={AggregateId}",
                        request.EventId, report.Migrated.Count, report.Skipped.Count, aggregateId);
                }
                else
                {
                    _logger.LogInformation(
                        "[7F-B] ConvertRegistrationMode DRYRUN — EventId={EventId} WouldMigrate={Migrated} WouldSkip={Skipped}",
                        request.EventId, report.Migrated.Count, report.Skipped.Count);
                }

                stopwatch.Stop();

                var result = new ConvertRegistrationModeResult(
                    AggregateConversionId: aggregateId,
                    TotalProcessed: report.TotalProcessed,
                    MigratedCount: report.Migrated.Count,
                    SkippedCount: report.Skipped.Count,
                    Migrated: report.Migrated.Select(m => new ConvertedRegistrationRow(
                        m.RegistrationId,
                        BeforeAttendeeCount: m.BeforeAttendees?.Count ?? m.BeforeHeadCount?.Total ?? 0,
                        AfterAttendeeCount: m.AfterAttendees?.Count ?? m.AfterHeadCount?.Total ?? 0)).ToList(),
                    Skipped: report.Skipped.Select(s => new SkippedRegistrationRow(
                        s.RegistrationId, s.ReasonCode, s.Reason)).ToList(),
                    WasDryRun: request.DryRun);

                return Result<ConvertRegistrationModeResult>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[7F-B] ConvertRegistrationMode FAILED — EventId={EventId} Duration={Duration}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private async Task<Guid> PersistAuditAsync(
        Guid eventId, Guid organiserId,
        LankaConnect.Domain.Events.Enums.RegistrationMode fromMode,
        LankaConnect.Domain.Events.Enums.RegistrationMode toMode,
        DateTime startedAt, DateTime completedAt,
        ConversionReport report, CancellationToken ct)
    {
        var aggregate = RegistrationModeConversion.Create(
            eventId, organiserId, fromMode, toMode,
            startedAt, completedAt,
            totalCount: report.TotalProcessed,
            migratedCount: report.Migrated.Count,
            skippedCount: report.Skipped.Count,
            failedCount: 0);
        await _db.RegistrationModeConversions.AddAsync(aggregate, ct);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        foreach (var migrated in report.Migrated)
        {
            var beforeJson = JsonSerializer.Serialize(new
            {
                attendees = migrated.BeforeAttendees,
                headCount = migrated.BeforeHeadCount,
            }, jsonOptions);
            var afterJson = JsonSerializer.Serialize(new
            {
                attendees = migrated.AfterAttendees,
                headCount = migrated.AfterHeadCount,
                leadAttendeeName = migrated.AfterLeadAttendeeName,
            }, jsonOptions);

            var row = RegistrationModeConversionRow.ForMigrated(
                aggregate.Id, migrated.RegistrationId,
                beforeJson, afterJson, completedAt);
            await _db.RegistrationModeConversionRows.AddAsync(row, ct);
        }

        foreach (var skipped in report.Skipped)
        {
            var row = RegistrationModeConversionRow.ForSkipped(
                aggregate.Id, skipped.RegistrationId, skipped.ReasonCode, completedAt);
            await _db.RegistrationModeConversionRows.AddAsync(row, ct);
        }

        return aggregate.Id;
    }
}
