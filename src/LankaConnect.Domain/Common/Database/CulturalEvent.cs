using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Common.Database
{
    public class CulturalEvent : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CulturalContext { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsReligious { get; set; }
        public string Category { get; set; } = string.Empty;
        public int ExpectedAttendees { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public string Languages { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Buddhist religious events
        public static CulturalEvent Vesak => new CulturalEvent
        {
            Name = "Vesak",
            Description = "Buddhist celebration of the birth, enlightenment and death of the Buddha",
            CulturalContext = "Buddhist",
            Category = "Religious",
            IsReligious = true,
            Languages = "Sinhala,Pali,English",
            IsActive = true
        };

        public static CulturalEvent Poson => new CulturalEvent
        {
            Name = "Poson",
            Description = "Celebration of the arrival of Buddhism in Sri Lanka",
            CulturalContext = "Buddhist",
            Category = "Religious",
            IsReligious = true,
            Languages = "Sinhala,Pali,English",
            IsActive = true
        };
    }
}