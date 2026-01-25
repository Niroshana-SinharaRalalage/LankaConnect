using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.X: Shared service for sending registration confirmation emails.
/// Extracts common email logic to eliminate duplication across handlers.
/// Supports both free and paid event registrations.
/// </summary>
public interface IRegistrationEmailService
{
    /// <summary>
    /// Sends registration confirmation email for free events.
    /// </summary>
    /// <param name="registration">The registration entity</param>
    /// <param name="event">The event entity</param>
    /// <param name="user">The user entity (null for anonymous registrations)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendFreeEventConfirmationEmailAsync(
        Registration registration,
        Event @event,
        User? user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends registration confirmation email for paid events with ticket PDF attachment.
    /// </summary>
    /// <param name="registration">The registration entity</param>
    /// <param name="event">The event entity</param>
    /// <param name="ticket">The ticket entity</param>
    /// <param name="ticketPdf">The ticket PDF bytes for attachment</param>
    /// <param name="user">The user entity (null for anonymous registrations)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendPaidEventConfirmationEmailAsync(
        Registration registration,
        Event @event,
        Ticket ticket,
        byte[] ticketPdf,
        User? user,
        CancellationToken cancellationToken = default);
}
