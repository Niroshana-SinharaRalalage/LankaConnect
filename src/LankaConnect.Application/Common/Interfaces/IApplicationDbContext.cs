using LankaConnect.Domain.Business;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.ReferenceData.Entities;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Business Domain
    DbSet<Business> Businesses { get; }
    DbSet<Service> Services { get; }
    DbSet<Review> Reviews { get; }

    // User Domain
    DbSet<User> Users { get; }

    // Events Domain
    DbSet<MetroArea> MetroAreas { get; }
    DbSet<EventTemplate> EventTemplates { get; } // Phase 6A.8
    DbSet<SignUpList> SignUpLists { get; } // Phase 6A.16: Required for cascade deletion
    DbSet<SignUpItem> SignUpItems { get; } // Phase 6A.16: Required for cascade deletion
    DbSet<SignUpCommitment> SignUpCommitments { get; } // Phase 6A.16: Cascade deletion
    DbSet<Registration> Registrations { get; }

    // Communications Domain
    DbSet<EmailMessage> EmailMessages { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<UserEmailPreferences> UserEmailPreferences { get; }

    // Reference Data Domain - Phase 6A.47 (Unified)
    DbSet<ReferenceValue> ReferenceValues { get; } // Phase 6A.47: Unified Reference Data

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}