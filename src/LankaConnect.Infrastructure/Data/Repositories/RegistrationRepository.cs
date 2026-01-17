using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class RegistrationRepository : Repository<Registration>, IRegistrationRepository
{
    private readonly ILogger<RegistrationRepository> _repoLogger;

    public RegistrationRepository(
        AppDbContext context,
        ILogger<RegistrationRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <summary>
    /// Override GetByIdAsync to enable tracking for scenarios where the entity will be modified
    /// and needs domain event dispatch (e.g., payment completion via Stripe webhook).
    /// Phase 6A.49: Fix for paid event email - ensures domain events are collected from ChangeTracker.
    /// Uses tracking (NOT AsNoTracking) so that when CompletePayment() raises PaymentCompletedEvent,
    /// the event is dispatched via AppDbContext.CommitAsync() → ChangeTracker.Entries&lt;BaseEntity&gt;().
    /// Phase 6A.X: Added comprehensive logging with LogContext, Stopwatch, and PostgreSQL SqlState extraction
    /// </summary>
    public override async Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EntityId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByIdAsync START: EntityId={EntityId}",
                id);

            try
            {
                var result = await _dbSet
                    .Include(r => r.Attendees)
                    .Include(r => r.Contact)
                    .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByIdAsync COMPLETE: EntityId={EntityId}, Found={Found}, Status={Status}, Duration={ElapsedMs}ms",
                    id,
                    result != null,
                    result?.Status,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Registration>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEvent"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEventAsync START: EventId={EventId}", eventId);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.EventId == eventId)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Registration>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUser"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByUserAsync START: UserId={UserId}", userId);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.UserId == userId &&
                               r.Status != RegistrationStatus.Cancelled &&
                               r.Status != RegistrationStatus.Refunded)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByUserAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventAndUser"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventAndUserAsync START: EventId={EventId}, UserId={UserId}",
                eventId,
                userId);

            try
            {
                // Only return active registrations (exclude cancelled and refunded)
                // This fixes the multi-attendee re-registration issue (Session 30)
                // Phase 6A.41: Fixed to return NEWEST registration (OrderByDescending)
                // and include Attendees collection to prevent stale data issues
                var result = await _dbSet
                    .AsNoTracking()
                    .Include(r => r.Attendees)
                    .Include(r => r.Contact)
                    .Where(r => r.EventId == eventId &&
                               r.UserId == userId &&
                               r.Status != RegistrationStatus.Cancelled &&
                               r.Status != RegistrationStatus.Refunded)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventAndUserAsync COMPLETE: EventId={EventId}, UserId={UserId}, Found={Found}, Status={Status}, Duration={ElapsedMs}ms",
                    eventId,
                    userId,
                    result != null,
                    result?.Status,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventAndUserAsync FAILED: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Registration>> GetByStatusAsync(RegistrationStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByStatus"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByStatusAsync START: Status={Status}", status);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.Status == status)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByStatusAsync COMPLETE: Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    status,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByStatusAsync FAILED: Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetTotalQuantityForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTotalQuantity"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetTotalQuantityForEventAsync START: EventId={EventId}", eventId);

            try
            {
                var result = await _dbSet
                    .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
                    .SumAsync(r => r.Quantity, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTotalQuantityForEventAsync COMPLETE: EventId={EventId}, TotalQuantity={TotalQuantity}, Duration={ElapsedMs}ms",
                    eventId,
                    result,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTotalQuantityForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.24: Gets an anonymous registration by event ID and contact email
    /// Used to fetch registration details for anonymous users' confirmation emails
    /// Phase 6A.41: Fixed to return NEWEST registration with includes
    /// Phase 6A.X: Added comprehensive logging with LogContext, Stopwatch, and PostgreSQL SqlState extraction
    /// </summary>
    public async Task<Registration?> GetAnonymousByEventAndEmailAsync(Guid eventId, string email, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAnonymousByEventAndEmail"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("Email", email))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetAnonymousByEventAndEmailAsync START: EventId={EventId}, Email={Email}",
                eventId,
                email);

            try
            {
                // Look for anonymous registrations (UserId is null) with matching contact email
                // Return newest registration with all related data loaded
                var result = await _dbSet
                    .AsNoTracking()
                    .Include(r => r.Attendees)
                    .Include(r => r.Contact)
                    .Where(r => r.EventId == eventId &&
                               r.UserId == null &&
                               r.Contact != null &&
                               r.Contact.Email == email &&
                               r.Status != RegistrationStatus.Cancelled &&
                               r.Status != RegistrationStatus.Refunded)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAnonymousByEventAndEmailAsync COMPLETE: EventId={EventId}, Email={Email}, Found={Found}, Status={Status}, Duration={ElapsedMs}ms",
                    eventId,
                    email,
                    result != null,
                    result?.Status,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetAnonymousByEventAndEmailAsync FAILED: EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    email,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}