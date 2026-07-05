using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Phase 7E.3b (architect review iteration 1, edit #2): focused service that creates a
/// Stripe Checkout session for a registration that has just been created via
/// <see cref="Event.RegisterWithHeadCount"/> or <see cref="Event.RegisterWithAttendees"/>.
///
/// Scope: handles the SIMPLE registration-line-item-only checkout (Mode B paid path —
/// no bundled donations / sponsors / add-ons / collections; per architect plan §5 those
/// are not bundled into B-mode RSVP). Mode A's complex bundled-extras flow currently
/// stays inline in <c>RsvpToEventCommandHandler.HandleMultiAttendeeRsvp</c> — see the
/// commit message for the controlled deviation from architect edit #2.
///
/// Why a service vs. a private helper: two handlers (auth + anonymous) consume Mode B
/// paid checkout; a service avoids the fork between them and gives the money path a
/// single test surface.
/// </summary>
public interface IRegistrationCheckoutService
{
    /// <summary>
    /// Creates a Stripe Checkout session for the registration's <c>TotalPrice</c>, sets the
    /// session ID on the registration via <see cref="LankaConnect.Products.LankaEvents.Domain.Registration.SetStripeCheckoutSession"/>,
    /// and returns the redirect URL.
    /// </summary>
    /// <param name="event">The event the registration belongs to.</param>
    /// <param name="registration">A Preliminary registration with non-null <c>TotalPrice &gt; 0</c>.</param>
    /// <param name="successUrl">Where Stripe redirects on success.</param>
    /// <param name="cancelUrl">Where Stripe redirects on cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Stripe Checkout URL on success; failure with a clear message otherwise.</returns>
    Task<Result<string>> CreateSessionForRegistrationAsync(
        Event @event,
        Registration registration,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default);
}
