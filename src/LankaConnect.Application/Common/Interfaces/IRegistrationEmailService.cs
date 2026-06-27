using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Modules.Identity.Contracts;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Shared service for sending registration confirmation emails.
/// Extracts common email logic to eliminate duplication across handlers.
/// Supports both free and paid event registrations.
/// Wave 4.10.s1c (2026-06-26): User parameter switched from User aggregate to
/// UserContactDto so consumers (ResendAttendeeConfirmation + ResendTicketEmail)
/// can drop their IUserRepository injection in favor of IIdentityQueries.
/// </summary>
public interface IRegistrationEmailService
{
    /// <summary>
    /// Sends registration confirmation email for free events.
    /// </summary>
    /// <param name="registration">The registration entity</param>
    /// <param name="event">The event entity</param>
    /// <param name="user">The user contact projection (null for anonymous registrations)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendFreeEventConfirmationEmailAsync(
        Registration registration,
        Event @event,
        UserContactDto? user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends registration confirmation email for paid events with ticket PDF attachment.
    /// </summary>
    /// <param name="registration">The registration entity</param>
    /// <param name="event">The event entity</param>
    /// <param name="ticket">The ticket entity</param>
    /// <param name="ticketPdf">The ticket PDF bytes for attachment</param>
    /// <param name="user">The user contact projection (null for anonymous registrations)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendPaidEventConfirmationEmailAsync(
        Registration registration,
        Event @event,
        Ticket ticket,
        byte[] ticketPdf,
        UserContactDto? user,
        CancellationToken cancellationToken = default);
}
