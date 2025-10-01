using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class ConflictCommunicationRequest
    {
        public Guid Id { get; set; }
        public string ConflictId { get; set; } = string.Empty;
        public List<string> ParticipantIds { get; set; } = new();
        public string CommunicationPurpose { get; set; } = string.Empty;
        public Dictionary<string, object> CommunicationContext { get; set; } = new();
        public string PreferredLanguage { get; set; } = string.Empty;
        public DateTime RequestTimestamp { get; set; }
    }

    public class ConflictCommunicationTemplates
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> Templates { get; set; } = new();
        public string CulturalContext { get; set; } = string.Empty;
        public List<string> AvailableLanguages { get; set; } = new();
        public Dictionary<string, object> TemplateVariables { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class CommunityDialogueRequest
    {
        public Guid Id { get; set; }
        public string CommunityId { get; set; } = string.Empty;
        public string DialogueTopic { get; set; } = string.Empty;
        public List<string> Participants { get; set; } = new();
        public Dictionary<string, object> DialogueParameters { get; set; } = new();
        public DateTime RequestedStartTime { get; set; }
        public string DialogueFormat { get; set; } = string.Empty;
    }

    public class CommunityDialogueFacilitationResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public string DialogueOutcome { get; set; } = string.Empty;
        public List<string> KeyDiscussionPoints { get; set; } = new();
        public Dictionary<string, string> ParticipantFeedback { get; set; } = new();
        public List<string> ActionItems { get; set; } = new();
        public DateTime CompletionTimestamp { get; set; }
    }

    public class CulturalAuthorityRequest
    {
        public Guid Id { get; set; }
        public string AuthorityType { get; set; } = string.Empty;
        public string CulturalDomain { get; set; } = string.Empty;
        public string RequestPurpose { get; set; } = string.Empty;
        public Dictionary<string, object> RequestContext { get; set; } = new();
        public string UrgencyLevel { get; set; } = string.Empty;
        public DateTime RequestTimestamp { get; set; }
    }

    public class CulturalAuthorityCoordinationResult
    {
        public Guid Id { get; set; }
        public bool IsCoordinationSuccessful { get; set; }
        public List<string> EngagedAuthorities { get; set; } = new();
        public string CoordinationOutcome { get; set; } = string.Empty;
        public Dictionary<string, string> AuthorityRecommendations { get; set; } = new();
        public List<string> CulturalGuidanceProvided { get; set; } = new();
        public DateTime CoordinationCompletionTimestamp { get; set; }
    }
}