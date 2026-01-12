using LankaConnect.Application.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.71: Repository implementation for tracking sent event reminders
/// Uses direct SQL with Npgsql for efficiency and to avoid complex EF Core setup
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
        try
        {
            var connection = _context.Database.GetDbConnection() as NpgsqlConnection;
            if (connection == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.71] Unable to get database connection for idempotency check, allowing send (fail-open)");
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
            return exists != null && (bool)exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Phase 6A.71] Error checking if reminder already sent for event {EventId}, registration {RegistrationId}, type {ReminderType}. Allowing send (fail-open).",
                eventId, registrationId, reminderType);
            return false; // Fail-open: if we can't check, allow the send
        }
    }

    public async Task RecordReminderSentAsync(
        Guid eventId,
        Guid registrationId,
        string reminderType,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = _context.Database.GetDbConnection() as NpgsqlConnection;
            if (connection == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.71] Unable to get database connection for recording reminder sent");
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

            cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("@eventId", eventId);
            cmd.Parameters.AddWithValue("@registrationId", registrationId);
            cmd.Parameters.AddWithValue("@reminderType", reminderType);
            cmd.Parameters.AddWithValue("@sentAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@recipientEmail", recipientEmail);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Phase 6A.71] Error recording reminder sent for event {EventId}, registration {RegistrationId}, type {ReminderType}",
                eventId, registrationId, reminderType);
        }
    }
}
