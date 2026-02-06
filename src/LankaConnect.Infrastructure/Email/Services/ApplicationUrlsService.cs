using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Email.Configuration;
using Microsoft.Extensions.Options;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Service for generating application URLs used in emails and redirects.
/// Phase 6A.53: Member Email Verification System
/// </summary>
public sealed class ApplicationUrlsService : IApplicationUrlsService
{
    private readonly ApplicationUrlsOptions _options;

    public ApplicationUrlsService(IOptions<ApplicationUrlsOptions> options)
    {
        _options = options.Value;
    }

    public string FrontendBaseUrl => _options.FrontendBaseUrl;

    public string GetEmailVerificationUrl(string token)
        => _options.GetEmailVerificationUrl(token);

    public string GetEventDetailsUrl(Guid eventId)
        => _options.GetEventDetailsUrl(eventId);

    public string GetUnsubscribeUrl(string token)
        => _options.GetUnsubscribeUrl(token);
}
