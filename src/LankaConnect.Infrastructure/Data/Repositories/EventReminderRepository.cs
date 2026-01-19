using LankaConnect.Application.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.71: Repository implementation for tracking sent event reminders
/// Uses direct SQL with Npgsql for efficiency and to avoid complex EF Core setup
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class EventReminderRepository : IEventReminderRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<EventReminderRepository> _logger;

    public EventReminderRepository(
        AppDbContext context,
        ILogger<EventReminderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsReminderAlreadySentAsync(
        Guid eventId,
        Guid registrationId,
        string reminderType,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "IsReminderAlreadySent"))
        using (LogContext.PushProperty("EntityType", "EventReminder"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        using (LogContext.PushProperty("ReminderType", reminderType))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "IsReminderAlreadySentAsync START: EventId={EventId}, RegistrationId={RegistrationId}, ReminderType={ReminderType}",
                eventId, registrationId, reminderType);

            try
            {
                var connection = _context.Database.GetDbConnection() as NpgsqlConnection;
                if (connection == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "IsReminderAlreadySentAsync FAILED: Unable to get database connection for idempotency check, allowing send (fail-open), Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return false;
                }

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                await using var cmd = new NpgsqlCommand(
                    @"SELECT EXISTS(
                        SELECT 1 FROM events.event_reminders_sent
                        WHERE event_id = @eventId
                          AND registration_id = @registrationId
                          AND reminder_type = @reminderType
                    )",
                    connection);

                cmd.Parameters.AddWithValue("@eventId", eventId);
                cmd.Parameters.AddWithValue("@registrationId", registrationId);
                cmd.Parameters.AddWithValue("@reminderType", reminderType);

                var exists = await cmd.ExecuteScalarAsync(cancellationToken);
                var result = exists != null && (bool)exists;

                stopwatch.Stop();

                _logger.LogInformation(
                    "IsReminderAlreadySentAsync COMPLETE: EventId={EventId}, RegistrationId={RegistrationId}, ReminderType={ReminderType}, AlreadySent={AlreadySent}, Duration={ElapsedMs}ms",
                    eventId,
                    registrationId,
                    reminderType,
                    result,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogWarning(ex,
                    "IsReminderAlreadySentAsync FAILED: EventId={EventId}, RegistrationId={RegistrationId}, ReminderType={ReminderType}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}. Allowing send (fail-open).",
                    eventId,
                    registrationId,
                    reminderType,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as PostgresException)?.SqlState ?? "N/A");

                return false; // Fail-open: if we can't check, allow the send
            }
        }
    }

    public async Task RecordReminderSentAsync(
        Guid eventId,
        Guid registrationId,
        string reminderType,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "RecordReminderSent"))
        using (LogContext.PushProperty("EntityType", "EventReminder"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        using (LogContext.PushProperty("ReminderType", reminderType))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RecordReminderSentAsync START: EventId={EventId}, RegistrationId={RegistrationId}, ReminderType={ReminderType}, RecipientEmail={RecipientEmail}",
                eventId, registrationId, reminderType, recipientEmail);

            try
            {
                var connection = _context.Database.GetDbConnection() as NpgsqlConnection;
                if (connection == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RecordReminderSentAsync FAILED: Unable to get database connection for recording reminder sent, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return;
                }

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO events.event_reminders_sent
                        (id, event_id, registration_id, reminder_type, sent_at, recipient_email)
                      VALUES
                        (@id, @eventId, @registrationId, @reminderType, @sentAt, @recipientEmail)
                      ON CONFLICT (event_id, registration_id, reminder_type) DO NOTHING",
                    connection);

                var newId = Guid.NewGuid();
                cmd.Parameters.AddWithValue("@id", newId);
                cmd.Parameters.AddWithValue("@eventId", eventId);
                cmd.Parameters.AddWithValue("@registrationId", registrationId);
                cmd.Parameters.AddWithValue("@reminderType", reminderType);
                cmd.Parameters.AddWithValue("@sentAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@recipientEmail", recipientEmail);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RecordReminderSentAsync COMPLETE: EventId={EventId}, RegistrationId={RegistrationId}, ReminderType={ReminderType}, RecipientEmail={RecipientEmail}, RowsAffected={RowsAffected}, Duration={ElapsedMs}ms",
                    eventId,
                    registrationId,
                    reminderType,
                    recipientEmail,
                    rowsAffected,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogWarning(ex,
                    "RecordReminderSentAsync FAILED: EventId={EventId}, RegistrationId={RegistrationId}, ReminderType={ReminderType}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    registrationId,
                    reminderType,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as PostgresException)?.SqlState ?? "N/A");
            }
        }
    }
}
