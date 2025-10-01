using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for selecting culturally appropriate email templates based on recipient profile
/// </summary>
public class CulturalTemplateSelector
{
    private readonly IMultiLanguageTemplateService _templateService;

    public CulturalTemplateSelector(IMultiLanguageTemplateService templateService)
    {
        _templateService = templateService;
    }

    public Result<LocalizedEmailTemplate> SelectTemplate(EmailMessage email, RecipientCulturalProfile recipientProfile)
    {
        var template = _templateService.GetLocalizedTemplate(email.Type, recipientProfile.Language);
        
        return Result<LocalizedEmailTemplate>.Success(template);
    }
}