using System.Text.Json;
using LankaConnect.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
namespace LankaConnect.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Records an audit entry for every <see cref="ICommand{TResponse}"/>
/// execution to the cross-module <c>platform.audit_events</c> table.
/// Failures in audit writing are logged but never propagate — auditing
/// must not break the user-facing operation.
/// </summary>
/// <remarks>
/// <para>
/// Per architect review of ADR-002, audit is the platform-wide observability
/// backbone: who did what, when, to what entity, with what outcome.
/// Cross-module audit reporting is a single-table query against
/// <c>platform.audit_events</c>.
/// </para>
/// <para>
/// The behavior captures both success and failure outcomes. The
/// <c>DetailsJson</c> payload is intentionally minimal — request type name +
/// outcome — to keep PII risk low. Modules can extend with explicit
/// <c>IAuditableCommand</c> contributing additional fields in a future task.
/// </para>
/// </remarks>
public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentActor _currentActor;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(
        IAuditLogger auditLogger,
        ICurrentActor currentActor,
        ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditLogger = auditLogger;
        _currentActor = currentActor;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var operationName = typeof(TRequest).Name;
        var actorId = _currentActor.ActorId;

        try
        {
            var response = await next();
            await TryLogAsync(operationName, actorId, outcome: "Success", details: null, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            // Audit the failure with the exception type name (NOT the message —
            // exception messages may contain PII / internal paths). Then rethrow.
            var details = JsonSerializer.Serialize(new { ExceptionType = ex.GetType().Name });
            await TryLogAsync(operationName, actorId, outcome: "Failure", details, cancellationToken);
            throw;
        }
    }

    private async Task TryLogAsync(
        string operationName,
        string? actorId,
        string outcome,
        string? details,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = new AuditEntry(operationName, actorId, DateTime.UtcNow, outcome, details);
            await _auditLogger.LogAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit-write failures must NOT crash the operation. Log locally
            // and continue — the audit gap will surface in dashboards but the
            // user-facing operation completes.
            _logger.LogError(
                ex,
                "Audit log write FAILED for {OperationName} (outcome={Outcome}); operation will continue",
                operationName,
                outcome);
        }
    }
}
