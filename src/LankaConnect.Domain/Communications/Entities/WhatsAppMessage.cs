using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// WhatsApp Business API Message entity with cultural intelligence integration
/// Supports Buddhist/Hindu calendar awareness, multi-language content, and diaspora community targeting
/// </summary>
public class WhatsAppMessage : BaseEntity
{
    private readonly List<string> _recipients = new();
    private readonly Dictionary<string, object> _templateParameters = new();
    private readonly Dictionary<string, string> _culturalMetadata = new();

    public string FromPhoneNumber { get; private set; } = null!;
    public IReadOnlyList<string> ToPhoneNumbers => _recipients.AsReadOnly();
    public string MessageContent { get; private set; } = string.Empty;
    
    public WhatsAppMessageType MessageType { get; private set; }
    public WhatsAppMessageStatus Status { get; private set; }
    
    // Cultural Intelligence Properties
    public WhatsAppCulturalContext CulturalContext { get; private set; } = null!;
    public string Language { get; private set; } = "en"; // Default to English
    public bool RequiresCulturalValidation { get; private set; }
    public double CulturalAppropriatnessScore { get; private set; }
    
    // Template Properties
    public string? TemplateName { get; private set; }
    public IReadOnlyDictionary<string, object> TemplateParameters => _templateParameters.AsReadOnly();
    
    // Geographic Properties
    public string? DiasporaRegion { get; private set; }
    public string? TimeZone { get; private set; }
    
    // Delivery Properties
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;
    
    // Cultural Metadata
    public IReadOnlyDictionary<string, string> CulturalMetadata => _culturalMetadata.AsReadOnly();
    
    // Business Properties
    public bool IsRead => ReadAt.HasValue;
    public bool IsDelivered => DeliveredAt.HasValue;
    public bool IsFailed => FailedAt.HasValue;
    public bool IsScheduled => ScheduledFor.HasValue && ScheduledFor > DateTime.UtcNow;
    public bool CanRetry => RetryCount < MaxRetries && IsFailed;

    // EF Core constructor
    private WhatsAppMessage() { }

    private WhatsAppMessage(
        string fromPhoneNumber,
        IEnumerable<string> toPhoneNumbers,
        string messageContent,
        WhatsAppMessageType messageType,
        WhatsAppCulturalContext culturalContext,
        string language = "en")
    {
        FromPhoneNumber = fromPhoneNumber;
        _recipients.AddRange(toPhoneNumbers);
        MessageContent = messageContent;
        MessageType = messageType;
        CulturalContext = culturalContext;
        Language = language;
        Status = WhatsAppMessageStatus.Draft;
        RequiresCulturalValidation = DetermineIfCulturalValidationRequired(messageType, culturalContext);
    }

    public static Result<WhatsAppMessage> Create(
        string fromPhoneNumber,
        IEnumerable<string> toPhoneNumbers,
        string messageContent,
        WhatsAppMessageType messageType,
        WhatsAppCulturalContext culturalContext,
        string language = "en")
    {
        if (string.IsNullOrWhiteSpace(fromPhoneNumber))
            return Result<WhatsAppMessage>.Failure("From phone number is required");

        var recipients = toPhoneNumbers?.ToList() ?? new List<string>();
        if (!recipients.Any())
            return Result<WhatsAppMessage>.Failure("At least one recipient is required");

        if (string.IsNullOrWhiteSpace(messageContent))
            return Result<WhatsAppMessage>.Failure("Message content is required");

        if (culturalContext == null)
            return Result<WhatsAppMessage>.Failure("Cultural context is required");

        var message = new WhatsAppMessage(fromPhoneNumber, recipients, messageContent, messageType, culturalContext, language);
        return Result<WhatsAppMessage>.Success(message);
    }

    public static Result<WhatsAppMessage> CreateFromTemplate(
        string fromPhoneNumber,
        IEnumerable<string> toPhoneNumbers,
        string templateName,
        Dictionary<string, object> templateParameters,
        WhatsAppCulturalContext culturalContext,
        string language = "en")
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return Result<WhatsAppMessage>.Failure("Template name is required");

        var recipients = toPhoneNumbers?.ToList() ?? new List<string>();
        if (!recipients.Any())
            return Result<WhatsAppMessage>.Failure("At least one recipient is required");

        var message = new WhatsAppMessage(fromPhoneNumber, recipients, "", WhatsAppMessageType.Template, culturalContext, language)
        {
            TemplateName = templateName
        };

        if (templateParameters != null)
        {
            foreach (var param in templateParameters)
            {
                message._templateParameters[param.Key] = param.Value;
            }
        }

        return Result<WhatsAppMessage>.Success(message);
    }

    public Result ScheduleForOptimalCulturalTiming(DateTime requestedTime)
    {
        if (requestedTime <= DateTime.UtcNow)
            return Result.Failure("Cannot schedule message in the past");

        ScheduledFor = requestedTime;
        Status = WhatsAppMessageStatus.Scheduled;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result SetCulturalAppropriatnessScore(double score)
    {
        if (score < 0 || score > 1)
            return Result.Failure("Cultural appropriateness score must be between 0 and 1");

        CulturalAppropriatnessScore = score;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result AddCulturalMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Result.Failure("Cultural metadata key is required");

        _culturalMetadata[key] = value ?? string.Empty;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result SetDiasporaRegion(string region, string timeZone)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result.Failure("Diaspora region is required");

        if (string.IsNullOrWhiteSpace(timeZone))
            return Result.Failure("Time zone is required");

        DiasporaRegion = region;
        TimeZone = timeZone;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result Send()
    {
        if (Status != WhatsAppMessageStatus.Draft && Status != WhatsAppMessageStatus.Scheduled)
            return Result.Failure("Only draft or scheduled messages can be sent");

        if (RequiresCulturalValidation && CulturalAppropriatnessScore < 0.7)
            return Result.Failure("Message failed cultural appropriateness validation");

        Status = WhatsAppMessageStatus.Sending;
        SentAt = DateTime.UtcNow;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result MarkAsDelivered()
    {
        if (Status != WhatsAppMessageStatus.Sending && Status != WhatsAppMessageStatus.Sent)
            return Result.Failure("Only sending or sent messages can be marked as delivered");

        Status = WhatsAppMessageStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result MarkAsRead()
    {
        if (Status != WhatsAppMessageStatus.Delivered)
            return Result.Failure("Only delivered messages can be marked as read");

        ReadAt = DateTime.UtcNow;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result MarkAsFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return Result.Failure("Error message is required when marking as failed");

        Status = WhatsAppMessageStatus.Failed;
        FailedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage.Trim();
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result Retry()
    {
        if (!CanRetry)
            return Result.Failure("Message cannot be retried - either max retries reached or not in failed state");

        RetryCount++;
        Status = WhatsAppMessageStatus.Draft;
        ErrorMessage = null;
        FailedAt = null;
        MarkAsUpdated();
        
        return Result.Success();
    }

    private static bool DetermineIfCulturalValidationRequired(WhatsAppMessageType messageType, WhatsAppCulturalContext culturalContext)
    {
        // Always require validation for broadcast messages to diaspora communities
        if (messageType == WhatsAppMessageType.Broadcast)
            return true;

        // Require validation for template messages with cultural content
        if (messageType == WhatsAppMessageType.Template && culturalContext.HasReligiousContent)
            return true;

        // Require validation for event-related messages
        if (messageType == WhatsAppMessageType.EventNotification)
            return true;

        return false;
    }
}

/// <summary>
/// Cultural context information for WhatsApp messages
/// </summary>
public class WhatsAppCulturalContext : ValueObject
{
    public bool HasReligiousContent { get; }
    public bool IsFestivalRelated { get; }
    public string? PrimaryReligion { get; }
    public string? FestivalName { get; }
    public DateTime? ReligiousObservanceDate { get; }
    public bool RequiresBuddhistCalendarAwareness { get; }
    public bool RequiresHinduCalendarAwareness { get; }

    public WhatsAppCulturalContext(
        bool hasReligiousContent = false,
        bool isFestivalRelated = false,
        string? primaryReligion = null,
        string? festivalName = null,
        DateTime? religiousObservanceDate = null,
        bool requiresBuddhistCalendarAwareness = false,
        bool requiresHinduCalendarAwareness = false)
    {
        HasReligiousContent = hasReligiousContent;
        IsFestivalRelated = isFestivalRelated;
        PrimaryReligion = primaryReligion;
        FestivalName = festivalName;
        ReligiousObservanceDate = religiousObservanceDate;
        RequiresBuddhistCalendarAwareness = requiresBuddhistCalendarAwareness;
        RequiresHinduCalendarAwareness = requiresHinduCalendarAwareness;
    }

    public static WhatsAppCulturalContext None => new();

    public static WhatsAppCulturalContext ForBuddhistFestival(string festivalName, DateTime observanceDate)
    {
        return new WhatsAppCulturalContext(
            hasReligiousContent: true,
            isFestivalRelated: true,
            primaryReligion: "Buddhism",
            festivalName: festivalName,
            religiousObservanceDate: observanceDate,
            requiresBuddhistCalendarAwareness: true);
    }

    public static WhatsAppCulturalContext ForHinduFestival(string festivalName, DateTime observanceDate)
    {
        return new WhatsAppCulturalContext(
            hasReligiousContent: true,
            isFestivalRelated: true,
            primaryReligion: "Hinduism",
            festivalName: festivalName,
            religiousObservanceDate: observanceDate,
            requiresHinduCalendarAwareness: true);
    }

    /// <summary>
    /// Converts WhatsAppCulturalContext to CulturalContext for cultural calendar services
    /// </summary>
    public LankaConnect.Domain.Communications.ValueObjects.CulturalContext ToCulturalContext()
    {
        var culturalBackground = PrimaryReligion switch
        {
            "Buddhism" => LankaConnect.Domain.Communications.Enums.CulturalBackground.SinhalaBuddhist,
            "Hinduism" => LankaConnect.Domain.Communications.Enums.CulturalBackground.TamilHindu,
            "Islam" => LankaConnect.Domain.Communications.Enums.CulturalBackground.SriLankanMuslim,
            "Christianity" => LankaConnect.Domain.Communications.Enums.CulturalBackground.SriLankanChristian,
            _ => LankaConnect.Domain.Communications.Enums.CulturalBackground.Other
        };

        var religiousContext = PrimaryReligion switch
        {
            "Buddhism" => LankaConnect.Domain.Communications.Enums.ReligiousContext.BuddhistPoyaday,
            "Hinduism" => LankaConnect.Domain.Communications.Enums.ReligiousContext.HinduFestival,
            "Islam" => LankaConnect.Domain.Communications.Enums.ReligiousContext.Ramadan,
            "Christianity" => LankaConnect.Domain.Communications.Enums.ReligiousContext.ChristianSabbath,
            _ => LankaConnect.Domain.Communications.Enums.ReligiousContext.None
        };

        return LankaConnect.Domain.Communications.ValueObjects.CulturalContext.Create(
            LankaConnect.Domain.Communications.Enums.SriLankanLanguage.English,
            culturalBackground,
            LankaConnect.Domain.Common.Enums.GeographicRegion.SriLanka,
            religiousContext,
            HasReligiousContent,
            HasReligiousContent).Value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return HasReligiousContent;
        yield return IsFestivalRelated;
        yield return PrimaryReligion ?? string.Empty;
        yield return FestivalName ?? string.Empty;
        yield return ReligiousObservanceDate ?? DateTime.MinValue;
        yield return RequiresBuddhistCalendarAwareness;
        yield return RequiresHinduCalendarAwareness;
    }
}