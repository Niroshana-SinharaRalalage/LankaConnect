using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for adapting email content for Buddhist cultural context
/// </summary>
public class BuddhistContentAdapter
{
    private readonly IMultiLanguageTemplateService _templateService;

    public BuddhistContentAdapter(IMultiLanguageTemplateService templateService)
    {
        _templateService = templateService;
    }

    public Result<CulturallyAdaptedContent> AdaptContent(string originalContent, RecipientCulturalProfile profile)
    {
        var adaptedContent = _templateService.GetCulturallyAdaptedContent(originalContent, profile.CulturalBackground);
        
        return Result<CulturallyAdaptedContent>.Success(adaptedContent);
    }
}