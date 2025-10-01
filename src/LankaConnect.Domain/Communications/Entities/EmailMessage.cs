using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using UserEmail = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Domain.Communications.Entities;

public class EmailMessage : BaseEntity
{
    private readonly List<string> _recipients = new();
    private readonly List<string> _ccRecipients = new();
    private readonly List<string> _bccRecipients = new();

    public UserEmail FromEmail { get; private set; } = null!;
    public IReadOnlyList<string> ToEmails => _recipients.AsReadOnly();
    public IReadOnlyList<string> CcEmails => _ccRecipients.AsReadOnly();
    public IReadOnlyList<string> BccEmails => _bccRecipients.AsReadOnly();
    
    public EmailSubject Subject { get; private set; } = null!;
    public string TextContent { get; private set; } = string.Empty;
    public string? HtmlContent { get; private set; }
    
    public EmailType Type { get; private set; }
    public EmailStatus Status { get; private set; }
    
    // Legacy compatibility properties for tests
    public EmailDeliveryStatus DeliveryStatus => (EmailDeliveryStatus)Status;
    
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? OpenedAt { get; private set; }
    public DateTime? ClickedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public int AttemptCount => RetryCount;
    public int MaxRetries { get; private set; }
    
    // Email tracking properties
    public bool IsOpened => OpenedAt.HasValue;
    public bool IsClicked => ClickedAt.HasValue;
    
    // Email template properties
    public string? TemplateName { get; private set; }
    public Dictionary<string, object>? TemplateData { get; private set; }
    public string? HtmlBody => HtmlContent;
    public int Priority { get; private set; }
    public string? MessageId { get; private set; }
    
    // Cultural Intelligence Properties
    public LankaConnect.Domain.Communications.ValueObjects.CulturalContext? CulturalContext { get; private set; }
    public bool CulturalTimingOptimized { get; private set; }
    public GeographicRegion? GeographicRegion { get; private set; }
    public DateTime? OptimalSendTime { get; private set; }
    public Dictionary<string, EmailDeliveryStatus>? RecipientStatuses { get; private set; }
    public string? CulturalDelayReason { get; private set; }
    public string? PostponementReason { get; private set; }
    public string? RetryStrategy { get; private set; }
    public List<StateTransition>? AuditTrail { get; private set; }
    public bool DiasporaOptimized { get; private set; }
    public bool ReligiousObservanceConsidered { get; private set; }
    public bool CulturalDelayBypassed { get; private set; }
    public string? BypassReason { get; private set; }
    public string? FestivalContext { get; private set; }
    public DateTime? SendingStartedAt { get; private set; }
    public DateTime? LastStateTransition { get; private set; }
    public bool HasAllRecipientsDelivered { get; private set; }
    public double? BackoffMultiplier { get; private set; }
    public List<RetryAttempt>? RetryHistory { get; private set; }
    public string? PermanentFailureReason { get; private set; }
    public int ConcurrentAccessAttempts { get; private set; }
    public bool DeliveryConfirmationReceived { get; private set; }
    public string? TargetTimezone { get; private set; }
    public bool GeographicOptimization { get; private set; }
    public DateTime? LocalizedSendTime { get; private set; }

    // For EF Core
    private EmailMessage() { }

    // Cultural Intelligence Factory Method
    public static Result<EmailMessage> CreateWithCulturalContext(
        UserEmail fromEmail,
        UserEmail toEmail,
        string subject,
        string body,
        LankaConnect.Domain.Communications.ValueObjects.CulturalContext? culturalContext = null,
        string? htmlBody = null,
        EmailType type = EmailType.Transactional)
    {
        var result = CreateWithEmails(fromEmail, toEmail, subject, body, htmlBody, type);
        if (!result.IsSuccess)
            return result;

        var message = result.Value;
        message.CulturalContext = culturalContext;
        message.RecipientStatuses = new Dictionary<string, EmailDeliveryStatus>();
        message.AuditTrail = new List<StateTransition>();
        message.RetryHistory = new List<RetryAttempt>();
        message.LastStateTransition = DateTime.UtcNow;

        // Initialize recipient status
        message.RecipientStatuses[toEmail.Value] = EmailDeliveryStatus.Pending;

        // Add initial state transition
        message.AuditTrail.Add(new StateTransition(EmailStatus.Pending, DateTime.UtcNow, "Email created with cultural context"));

        return Result<EmailMessage>.Success(message);
    }
    
    // Simplified constructor for integration tests - email-based approach
    public EmailMessage(
        UserEmail toEmail,
        string subject,
        string body,
        string? htmlBody = null,
        string? templateName = null,
        Dictionary<string, object>? templateData = null,
        int priority = 5) : this()
    {
        if (string.IsNullOrEmpty(subject))
            throw new ArgumentNullException(nameof(subject));
        if (string.IsNullOrEmpty(body))
            throw new ArgumentNullException(nameof(body));
            
        var fromEmailResult = UserEmail.Create("system@lankaconnect.com");
        var subjectResult = EmailSubject.Create(subject);
        
        if (!fromEmailResult.IsSuccess || !subjectResult.IsSuccess)
            throw new ArgumentException("Invalid email data");
        
        FromEmail = fromEmailResult.Value;
        Subject = subjectResult.Value;
        TextContent = body;
        HtmlContent = htmlBody;
        TemplateName = templateName;
        TemplateData = templateData;
        Priority = priority;
        Type = EmailType.Transactional;
        Status = EmailStatus.Pending;
        MaxRetries = 3;
        RetryCount = 0;
        
        AddRecipient(toEmail);
    }

    // Create method using email addresses (preferred approach)
    public static Result<EmailMessage> CreateWithEmails(
        UserEmail fromEmail,
        UserEmail toEmail,
        string subject,
        string body,
        string? htmlBody = null,
        EmailType type = EmailType.Transactional,
        int priority = 5,
        int maxRetries = 3)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Result<EmailMessage>.Failure("Email subject is required");
        if (string.IsNullOrWhiteSpace(body))
            return Result<EmailMessage>.Failure("Email body is required");

        var subjectResult = EmailSubject.Create(subject);
        if (!subjectResult.IsSuccess)
            return Result<EmailMessage>.Failure(subjectResult.Error);

        var message = new EmailMessage(fromEmail, subjectResult.Value, body, htmlBody, type, maxRetries);
        message.Priority = priority;
        
        var addResult = message.AddRecipient(toEmail);
        if (!addResult.IsSuccess)
            return Result<EmailMessage>.Failure(addResult.Error);

        return Result<EmailMessage>.Success(message);
    }

    private EmailMessage(
        UserEmail fromEmail,
        EmailSubject subject,
        string textContent,
        string? htmlContent,
        EmailType type,
        int maxRetries = 3)
    {
        FromEmail = fromEmail;
        Subject = subject;
        TextContent = textContent;
        HtmlContent = htmlContent;
        Type = type;
        Status = EmailStatus.Pending;
        MaxRetries = maxRetries;
        RetryCount = 0;
    }

    public static Result<EmailMessage> Create(
        UserEmail fromEmail,
        EmailSubject subject,
        string textContent,
        string? htmlContent = null,
        EmailType type = EmailType.Transactional,
        int maxRetries = 3)
    {
        if (string.IsNullOrWhiteSpace(textContent))
            return Result<EmailMessage>.Failure("Email text content is required");

        if (maxRetries < 0 || maxRetries > 10)
            return Result<EmailMessage>.Failure("Max retries must be between 0 and 10");

        var message = new EmailMessage(fromEmail, subject, textContent, htmlContent, type, maxRetries);
        return Result<EmailMessage>.Success(message);
    }

    public Result AddRecipient(UserEmail email)
    {
        if (_recipients.Contains(email.Value))
            return Result.Failure("Email already added as recipient");

        _recipients.Add(email.Value);
        return Result.Success();
    }

    public Result AddCcRecipient(UserEmail email)
    {
        if (_ccRecipients.Contains(email.Value))
            return Result.Failure("Email already added as CC recipient");

        _ccRecipients.Add(email.Value);
        return Result.Success();
    }

    public Result AddBccRecipient(UserEmail email)
    {
        if (_bccRecipients.Contains(email.Value))
            return Result.Failure("Email already added as BCC recipient");

        _bccRecipients.Add(email.Value);
        return Result.Success();
    }

    public Result MarkAsQueued()
    {
        if (Status != EmailStatus.Pending)
            return Result.Failure($"Cannot mark email as queued when status is {Status}");

        Status = EmailStatus.Queued;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result MarkAsSending()
    {
        if (Status != EmailStatus.Queued && Status != EmailStatus.Failed)
            return Result.Failure($"Cannot mark email as sending when status is {Status}");

        Status = EmailStatus.Sending;
        MarkAsUpdated();
        return Result.Success();
    }


    /// <summary>
    /// Mark as sent with Result pattern - for comprehensive test compatibility
    /// </summary>
    public Result MarkAsSentResult()
    {
        // Only allow from Sending status (strict workflow for comprehensive tests)
        if (Status != EmailStatus.Sending)
            return Result.Failure($"Cannot mark email as sent when status is {Status}");
            
        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result MarkAsDeliveredResult()
    {
        if (Status != EmailStatus.Sent)
            return Result.Failure($"Cannot mark email as delivered when status is {Status}");

        Status = EmailStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result MarkAsFailed(string errorMessage, DateTime? nextRetryAt)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return Result.Failure("Error message is required when marking email as failed");

        Status = EmailStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        FailedAt = DateTime.UtcNow;
        NextRetryAt = nextRetryAt;
        MarkAsUpdated();
        return Result.Success();
    }


    public bool CanRetry()
    {
        // Can only retry if status is failed and within retry limits
        if (Status != EmailStatus.Failed)
            return false;
            
        // Check if we haven't exceeded max retries
        if (RetryCount > MaxRetries)
            return false;
            
        // If NextRetryAt is null, cannot retry
        if (!NextRetryAt.HasValue)
            return false;
            
        // Can only retry if NextRetryAt is in the past/present
        return NextRetryAt <= DateTime.UtcNow;
    }

    public Result Retry()
    {
        if (!CanRetry())
            return Result.Failure("Email cannot be retried");

        Status = EmailStatus.Queued;
        ErrorMessage = null;
        MarkAsUpdated();
        return Result.Success();
    }
    
    // Alternative methods for test compatibility
    
    /// <summary>
    /// Mark as clicked - for email tracking analytics
    /// </summary>
    public void MarkAsClicked()
    {
        if (!ClickedAt.HasValue)
        {
            ClickedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }
    }
    
    /// <summary>
    /// Mark as opened - for email tracking analytics
    /// </summary>
    public void MarkAsOpened()
    {
        if (!OpenedAt.HasValue)
        {
            OpenedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }
    }
    
    /// <summary>
    /// Mark as sent with message ID - alternative signature
    /// </summary>
    public void MarkAsSent(string? messageId = null)
    {
        // Allow transitioning from Pending, Queued, or Sending to Sent
        if (Status == EmailStatus.Sent || Status == EmailStatus.Delivered || Status == EmailStatus.Failed)
            return;
            
        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
        MessageId = messageId;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Mark as delivered - alternative signature
    /// </summary>
    public void MarkAsDelivered()
    {
        // Allow marking as delivered from Sent status only
        if (Status != EmailStatus.Sent)
            return;
            
        Status = EmailStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
    
    
    
    /// <summary>
    /// Mark as failed without retry time - void method for test compatibility
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = EmailStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        FailedAt = DateTime.UtcNow;
        NextRetryAt = null; // Explicitly set to null for no retry
        MarkAsUpdated();
    }
    
    
    /// <summary>
    /// Retry email - alternative signature
    /// </summary>
    public void RetryEmail()
    {
        if (Status == EmailStatus.Failed && CanRetryNow())
        {
            Status = EmailStatus.Pending;
            NextRetryAt = null;
            ErrorMessage = null;
            MarkAsUpdated();
        }
    }
    
    /// <summary>
    /// Check if can retry with max retries parameter
    /// </summary>
    public bool CanRetry(int maxRetries = 3)
    {
        // This overloaded version is more permissive - allows retry if not yet at max attempts
        // Can retry from Pending (not yet attempted) or Failed status
        if (Status == EmailStatus.Pending || Status == EmailStatus.Failed)
        {
            return AttemptCount < maxRetries;
        }
        return false;
    }
    
    /// <summary>
    /// Check if can retry based on retry time
    /// </summary>
    private bool CanRetryNow()
    {
        return NextRetryAt == null || NextRetryAt <= DateTime.UtcNow;
    }

    // Cultural Intelligence Methods
    
    /// <summary>
    /// Queue email with cultural timing optimization
    /// </summary>
    public Result QueueWithCulturalOptimization(DateTime? culturallyOptimizedTime = null)
    {
        if (culturallyOptimizedTime.HasValue)
        {
            OptimalSendTime = culturallyOptimizedTime.Value;
            CulturalTimingOptimized = true;
        }

        var result = MarkAsQueued();
        if (result.IsSuccess)
        {
            AddStateTransition("Queued with cultural optimization");
        }

        return result;
    }

    /// <summary>
    /// Mark specific recipient as delivered
    /// </summary>
    public void MarkRecipientAsDelivered(string recipientEmail, DateTime? deliveredAt = null)
    {
        RecipientStatuses ??= new Dictionary<string, EmailDeliveryStatus>();
        RecipientStatuses[recipientEmail] = EmailDeliveryStatus.Delivered;

        // Check if all recipients are delivered
        CheckAllRecipientsDelivered();

        AddStateTransition($"Recipient {recipientEmail} marked as delivered");
    }

    /// <summary>
    /// Begin sending process with state tracking
    /// </summary>
    public Result BeginSending()
    {
        SendingStartedAt = DateTime.UtcNow;
        AddStateTransition("Sending started");
        return MarkAsSending();
    }

    /// <summary>
    /// Enhanced failure marking with retry logic
    /// </summary>
    public Result MarkAsFailedWithRetryLogic(string errorMessage, string? retryStrategy = null)
    {
        RetryStrategy = retryStrategy ?? "exponential";
        BackoffMultiplier = CalculateBackoffMultiplier();

        var result = MarkAsFailed(errorMessage, DateTime.UtcNow);
        if (result.IsSuccess)
        {
            AddRetryAttempt(errorMessage);
            AddStateTransition($"Failed with retry logic: {errorMessage}");
        }

        return result;
    }

    /// <summary>
    /// Execute retry with cultural awareness
    /// </summary>
    public Result ExecuteRetryWithCulturalAwareness(DateTime? culturallyOptimizedRetryTime = null)
    {
        if (culturallyOptimizedRetryTime.HasValue)
        {
            OptimalSendTime = culturallyOptimizedRetryTime.Value;
            CulturalTimingOptimized = true;
        }

        var result = Retry();
        if (result.IsSuccess)
        {
            AddStateTransition("Retry executed with cultural awareness");
        }

        return result;
    }

    /// <summary>
    /// Execute retry using standard method
    /// </summary>
    public Result ExecuteRetry()
    {
        return Retry();
    }

    /// <summary>
    /// Set email priority
    /// </summary>
    public void SetPriority(int priority)
    {
        Priority = Math.Max(1, Math.Min(10, priority)); // Clamp between 1-10
        AddStateTransition($"Priority set to {Priority}");
        MarkAsUpdated();
    }

    /// <summary>
    /// Set email type
    /// </summary>
    public void SetEmailType(EmailType emailType)
    {
        Type = emailType;
        AddStateTransition($"Email type set to {emailType}");
        MarkAsUpdated();
    }

    /// <summary>
    /// Optimize for diaspora communities
    /// </summary>
    public void OptimizeForDiaspora(GeographicRegion targetRegion, DateTime? optimalLocalTime = null)
    {
        GeographicRegion = targetRegion;
        DiasporaOptimized = true;
        
        if (optimalLocalTime.HasValue)
        {
            LocalizedSendTime = optimalLocalTime.Value;
        }

        AddStateTransition($"Optimized for diaspora region: {targetRegion}");
        MarkAsUpdated();
    }

    /// <summary>
    /// Optimize for geographic region
    /// </summary>
    public void OptimizeForGeography(GeographicRegion region, string? timezone = null)
    {
        GeographicRegion = region;
        GeographicOptimization = true;
        TargetTimezone = timezone;
        
        AddStateTransition($"Optimized for geography: {region}");
        MarkAsUpdated();
    }

    /// <summary>
    /// Mark as sent with multi-recipient tracking
    /// </summary>
    public Result MarkAsSentWithMultiRecipientTracking()
    {
        var result = MarkAsSentResult();
        if (result.IsSuccess)
        {
            // Initialize all recipient statuses as sent
            RecipientStatuses ??= new Dictionary<string, EmailDeliveryStatus>();
            foreach (var email in ToEmails)
            {
                RecipientStatuses[email] = EmailDeliveryStatus.Sent;
            }
            
            AddStateTransition("Marked as sent with multi-recipient tracking");
        }

        return result;
    }

    /// <summary>
    /// Get recipient delivery status
    /// </summary>
    public EmailDeliveryStatus GetRecipientStatus(string recipientEmail)
    {
        return RecipientStatuses?.GetValueOrDefault(recipientEmail, EmailDeliveryStatus.Pending) 
               ?? EmailDeliveryStatus.Pending;
    }

    /// <summary>
    /// Get recipient delivery time
    /// </summary>
    public DateTime? GetRecipientDeliveryTime(string recipientEmail)
    {
        if (GetRecipientStatus(recipientEmail) == EmailDeliveryStatus.Delivered)
        {
            return DeliveredAt; // Simplified - in practice would track per recipient
        }
        return null;
    }

    /// <summary>
    /// Process delivery confirmation webhook
    /// </summary>
    public void ProcessDeliveryWebhook(string recipientEmail, EmailDeliveryStatus status)
    {
        DeliveryConfirmationReceived = true;
        MarkRecipientAsDelivered(recipientEmail);
        AddStateTransition($"Delivery webhook processed for {recipientEmail}");
    }

    /// <summary>
    /// Get complete state transition history
    /// </summary>
    public List<StateTransition> GetTransitionHistory()
    {
        return AuditTrail?.ToList() ?? new List<StateTransition>();
    }

    // Private helper methods
    
    private void AddStateTransition(string description)
    {
        AuditTrail ??= new List<StateTransition>();
        AuditTrail.Add(new StateTransition(Status, DateTime.UtcNow, description));
        LastStateTransition = DateTime.UtcNow;
    }

    private void AddRetryAttempt(string errorMessage)
    {
        RetryHistory ??= new List<RetryAttempt>();
        RetryHistory.Add(new RetryAttempt(DateTime.UtcNow, errorMessage, RetryCount));
    }

    private double CalculateBackoffMultiplier()
    {
        return Math.Pow(2, RetryCount); // Exponential backoff
    }

    private void CheckAllRecipientsDelivered()
    {
        if (RecipientStatuses != null)
        {
            HasAllRecipientsDelivered = RecipientStatuses.Values.All(status => status == EmailDeliveryStatus.Delivered);
        }
    }
}

// Supporting classes for cultural intelligence features

/// <summary>
/// State transition audit record
/// </summary>
public record StateTransition(EmailStatus Status, DateTime Timestamp, string Description);

/// <summary>
/// Retry attempt record
/// </summary>
public record RetryAttempt(DateTime Timestamp, string ErrorMessage, int AttemptNumber);