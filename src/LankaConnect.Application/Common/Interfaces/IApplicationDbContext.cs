using LankaConnect.Domain.Business;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
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

    // Communications Domain
    DbSet<EmailMessage> EmailMessages { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<UserEmailPreferences> UserEmailPreferences { get; }

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}