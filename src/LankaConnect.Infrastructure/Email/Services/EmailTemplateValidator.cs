using System.Text.RegularExpressions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Shared.Email.Contracts;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.99: Validates email templates against typed parameter contracts.
/// Runs at application startup to detect parameter mismatches early.
/// </summary>
public partial class EmailTemplateValidator : IEmailTemplateValidator
{
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly ILogger<EmailTemplateValidator> _logger;

    // Regex to extract Handlebars placeholders: {{ParamName}} or {{{ParamName}}}
    [GeneratedRegex(@"\{\{#?/?([a-zA-Z0-9_]+)\}\}|\{\{\{([a-zA-Z0-9_]+)\}\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    // Map of template names to their expected parameters from TypedEmailParams classes
    private static readonly Dictionary<string, HashSet<string>> ExpectedParameters = new()
    {
        // Password templates
        [EmailTemplateContract.TemplateNames.PasswordReset] = new HashSet<string>
        {
            EmailTemplateContract.Common.UserName,
            EmailTemplateContract.Auth.UserEmail,
            EmailTemplateContract.Auth.ResetToken,
            EmailTemplateContract.Auth.ResetLink,
            EmailTemplateContract.Auth.ExpiresAt,
            EmailTemplateContract.Common.CompanyName,
            EmailTemplateContract.Common.SupportEmail,
            EmailTemplateContract.Common.Year
        },
        [EmailTemplateContract.TemplateNames.PasswordChangeConfirmation] = new HashSet<string>
        {
            EmailTemplateContract.Common.UserName,
            EmailTemplateContract.Auth.UserEmail,
            EmailTemplateContract.Auth.ChangedAt,
            EmailTemplateContract.Common.CompanyName,
            EmailTemplateContract.Common.SupportEmail,
            EmailTemplateContract.Auth.LoginUrl,
            EmailTemplateContract.Common.Year
        },

        // Registration templates
        ["template-free-event-registration-confirmation"] = new HashSet<string>
        {
            EmailTemplateContract.Common.UserName,
            EmailTemplateContract.Event.EventTitle,
            EmailTemplateContract.Event.EventStartDate,
            EmailTemplateContract.Event.EventStartTime,
            EmailTemplateContract.Event.EventDateTime,
            EmailTemplateContract.Event.EventLocation,
            EmailTemplateContract.Event.EventDetailsUrl,
            EmailTemplateContract.Event.SignUpListsUrl,
            EmailTemplateContract.Registration.RegistrationDate,
            EmailTemplateContract.Registration.HasAttendeeDetails,
            EmailTemplateContract.Registration.Attendees,
            EmailTemplateContract.OrganizerContact.HasOrganizerContact,
            EmailTemplateContract.OrganizerContact.OrganizerContactName,
            EmailTemplateContract.OrganizerContact.OrganizerContactEmail,
            EmailTemplateContract.OrganizerContact.OrganizerContactPhone,
            EmailTemplateContract.Registration.HasContactInfo,
            EmailTemplateContract.Registration.ContactEmail,
            EmailTemplateContract.Registration.ContactPhone,
            EmailTemplateContract.EventImage.HasEventImage,
            EmailTemplateContract.EventImage.EventImageUrl
        },
        ["template-paid-event-registration-confirmation-with-ticket"] = new HashSet<string>
        {
            EmailTemplateContract.Common.UserName,
            EmailTemplateContract.Event.EventTitle,
            EmailTemplateContract.Event.EventStartDate,
            EmailTemplateContract.Event.EventStartTime,
            EmailTemplateContract.Event.EventDateTime,
            EmailTemplateContract.Event.EventLocation,
            EmailTemplateContract.Event.EventDetailsUrl,
            EmailTemplateContract.Event.SignUpListsUrl,
            EmailTemplateContract.Ticket.TicketType,
            EmailTemplateContract.Payment.AmountPaid,
            EmailTemplateContract.Payment.TotalAmount,
            EmailTemplateContract.Payment.PaymentIntentId,
            EmailTemplateContract.Payment.OrderNumber,
            EmailTemplateContract.Payment.PaymentDate,
            EmailTemplateContract.Registration.RegistrationDate,
            EmailTemplateContract.Registration.Quantity,
            EmailTemplateContract.Registration.HasAttendeeDetails,
            EmailTemplateContract.Registration.Attendees,
            EmailTemplateContract.Ticket.HasTicket,
            EmailTemplateContract.Ticket.TicketCode,
            EmailTemplateContract.Ticket.TicketExpiryDate,
            EmailTemplateContract.Ticket.TicketUrl,
            EmailTemplateContract.OrganizerContact.HasOrganizerContact,
            EmailTemplateContract.OrganizerContact.OrganizerContactName,
            EmailTemplateContract.OrganizerContact.OrganizerContactEmail,
            EmailTemplateContract.OrganizerContact.OrganizerContactPhone,
            EmailTemplateContract.Registration.HasContactInfo,
            EmailTemplateContract.Registration.ContactEmail,
            EmailTemplateContract.Registration.ContactPhone,
            EmailTemplateContract.EventImage.HasEventImage,
            EmailTemplateContract.EventImage.EventImageUrl
        },

        // Refund templates
        [EmailTemplateContract.TemplateNames.RefundRequested] = new HashSet<string>
        {
            EmailTemplateContract.Common.UserName,
            EmailTemplateContract.Event.EventTitle,
            EmailTemplateContract.Event.EventStartDate,
            EmailTemplateContract.Event.EventStartTime,
            EmailTemplateContract.Event.EventDateTime,
            EmailTemplateContract.Refund.RefundAmount,
            EmailTemplateContract.Refund.OriginalAmount,
            EmailTemplateContract.Refund.Currency,
            EmailTemplateContract.Refund.RefundReason,
            EmailTemplateContract.Refund.RefundStatus,
            EmailTemplateContract.Refund.RequestedAt,
            EmailTemplateContract.Refund.ProcessingMethod,
            EmailTemplateContract.Common.SupportEmail,
            EmailTemplateContract.Refund.RefundDetailsUrl,
            EmailTemplateContract.Event.EventDetailsUrl,
            EmailTemplateContract.Refund.StripeRefundId,
            EmailTemplateContract.Refund.ReferenceId,
            EmailTemplateContract.OrganizerContact.HasOrganizerContact,
            EmailTemplateContract.OrganizerContact.OrganizerContactName,
            EmailTemplateContract.OrganizerContact.OrganizerContactEmail,
            EmailTemplateContract.OrganizerContact.OrganizerContactPhone,
            EmailTemplateContract.Common.Year
        },
        [EmailTemplateContract.TemplateNames.RefundCompleted] = new HashSet<string>
        {
            EmailTemplateContract.Common.UserName,
            EmailTemplateContract.Event.EventTitle,
            EmailTemplateContract.Event.EventStartDate,
            EmailTemplateContract.Event.EventStartTime,
            EmailTemplateContract.Event.EventDateTime,
            EmailTemplateContract.Refund.RefundAmount,
            EmailTemplateContract.Refund.OriginalAmount,
            EmailTemplateContract.Refund.Currency,
            EmailTemplateContract.Refund.RefundStatus,
            EmailTemplateContract.Refund.CompletedAt,
            EmailTemplateContract.Refund.ProcessingMethod,
            EmailTemplateContract.Common.SupportEmail,
            EmailTemplateContract.Refund.RefundDetailsUrl,
            EmailTemplateContract.Event.EventDetailsUrl,
            EmailTemplateContract.Refund.StripeRefundId,
            EmailTemplateContract.Refund.ReferenceId,
            EmailTemplateContract.OrganizerContact.HasOrganizerContact,
            EmailTemplateContract.OrganizerContact.OrganizerContactName,
            EmailTemplateContract.OrganizerContact.OrganizerContactEmail,
            EmailTemplateContract.OrganizerContact.OrganizerContactPhone,
            EmailTemplateContract.Common.Year
        },

        // Registration cancellation
        [EmailTemplateContract.TemplateNames.EventRegistrationCancellation] = new HashSet<string>
        {
            EmailTemplateContract.Common.UserName,
            EmailTemplateContract.Event.EventTitle,
            EmailTemplateContract.Event.EventStartDate,
            EmailTemplateContract.Event.EventStartTime,
            EmailTemplateContract.Event.EventDateTime,
            EmailTemplateContract.Event.EventLocation,
            EmailTemplateContract.Registration.CancellationReason,
            EmailTemplateContract.Registration.CancelledAt,
            EmailTemplateContract.Registration.CancellationDate,
            EmailTemplateContract.Refund.RefundStatus,
            EmailTemplateContract.Event.EventDetailsUrl,
            EmailTemplateContract.Common.SupportEmail,
            EmailTemplateContract.Common.Year,
            EmailTemplateContract.OrganizerContact.HasOrganizerContact,
            EmailTemplateContract.OrganizerContact.OrganizerContactName,
            EmailTemplateContract.OrganizerContact.OrganizerContactEmail,
            EmailTemplateContract.OrganizerContact.OrganizerContactPhone
        }
    };

    public EmailTemplateValidator(
        IEmailTemplateRepository templateRepository,
        ILogger<EmailTemplateValidator> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task<EmailTemplateValidationResult> ValidateAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var result = new EmailTemplateValidationResult();

        _logger.LogInformation("Phase 6A.99: Starting email template validation...");

        try
        {
            var templates = await _templateRepository.GetAllAsync(cancellationToken);
            result.TotalTemplatesChecked = templates.Count;

            foreach (var template in templates)
            {
                var templateResult = ValidateTemplate(template.Name, template.HtmlTemplate ?? string.Empty);

                if (templateResult.Errors.Any())
                {
                    result.Errors.AddRange(templateResult.Errors);
                }
                else if (templateResult.Warnings.Any())
                {
                    result.Warnings.AddRange(templateResult.Warnings);
                    result.ValidatedTemplates.Add(template.Name);
                }
                else
                {
                    result.ValidatedTemplates.Add(template.Name);
                }
            }

            if (result.IsValid)
            {
                _logger.LogInformation(
                    "Phase 6A.99: Email template validation completed successfully. {Count} templates validated.",
                    result.TotalTemplatesChecked);
            }
            else if (result.HasWarningsOnly)
            {
                _logger.LogWarning(
                    "Phase 6A.99: Email template validation completed with {WarningCount} warnings.",
                    result.Warnings.Count);
            }
            else
            {
                _logger.LogError(
                    "Phase 6A.99: Email template validation failed with {ErrorCount} errors.",
                    result.Errors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase 6A.99: Email template validation failed with exception");
            result.Errors.Add(new EmailTemplateValidationError
            {
                TemplateName = "N/A",
                Message = $"Validation failed with exception: {ex.Message}"
            });
        }

        return result;
    }

    public async Task<EmailTemplateValidationResult> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByNameAsync(templateName, cancellationToken);

        if (template == null)
        {
            return new EmailTemplateValidationResult
            {
                Errors = new List<EmailTemplateValidationError>
                {
                    new()
                    {
                        TemplateName = templateName,
                        Message = $"Template '{templateName}' not found in database"
                    }
                }
            };
        }

        return ValidateTemplate(templateName, template.HtmlTemplate ?? string.Empty);
    }

    private EmailTemplateValidationResult ValidateTemplate(string templateName, string htmlTemplate)
    {
        var result = new EmailTemplateValidationResult { TotalTemplatesChecked = 1 };

        // Skip validation for templates not in our expected parameters list
        if (!ExpectedParameters.TryGetValue(templateName, out var expectedParams))
        {
            result.Warnings.Add(new EmailTemplateValidationWarning
            {
                TemplateName = templateName,
                Message = "Template not configured for validation (not in ExpectedParameters)"
            });
            return result;
        }

        // Extract placeholders from the HTML template
        var templatePlaceholders = ExtractPlaceholders(htmlTemplate);

        // Find parameters in template but not provided by code
        var missingInCode = templatePlaceholders.Except(expectedParams).ToList();

        // Find parameters expected by code but not in template
        // Note: This is less critical since extra parameters are usually harmless
        var missingInTemplate = expectedParams.Except(templatePlaceholders).ToList();

        // Only report as error if template has placeholders that code doesn't provide
        if (missingInCode.Any())
        {
            // Filter out conditional helper names (if, else, each, unless, etc.)
            var filteredMissingInCode = missingInCode
                .Where(p => !IsHandlebarsHelper(p))
                .ToList();

            if (filteredMissingInCode.Any())
            {
                result.Errors.Add(new EmailTemplateValidationError
                {
                    TemplateName = templateName,
                    Message = $"Template uses parameters not provided by code",
                    MissingInCode = filteredMissingInCode
                });
            }
        }

        // Missing in template is just a warning (extra params are usually OK)
        if (missingInTemplate.Any())
        {
            result.Warnings.Add(new EmailTemplateValidationWarning
            {
                TemplateName = templateName,
                Message = $"Code provides parameters not used in template: {string.Join(", ", missingInTemplate)}"
            });
        }

        return result;
    }

    private static HashSet<string> ExtractPlaceholders(string htmlTemplate)
    {
        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in PlaceholderRegex().Matches(htmlTemplate))
        {
            // Group 1 is for {{param}} and {{#param}}, Group 2 is for {{{param}}}
            var paramName = match.Groups[1].Value;
            if (string.IsNullOrEmpty(paramName))
            {
                paramName = match.Groups[2].Value;
            }

            if (!string.IsNullOrEmpty(paramName) && !IsHandlebarsHelper(paramName))
            {
                placeholders.Add(paramName);
            }
        }

        return placeholders;
    }

    private static bool IsHandlebarsHelper(string name)
    {
        // Common Handlebars helpers/keywords to exclude
        var helpers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "if", "else", "each", "unless", "with", "lookup", "log",
            "blockHelperMissing", "helperMissing", "raw"
        };
        return helpers.Contains(name);
    }
}
