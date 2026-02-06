using System;
using System.Collections.Generic;
using LankaConnect.Infrastructure.TempModels;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LankaConnect.Infrastructure.TempContext;

public partial class LankaConnectDbContext : DbContext
{
    public LankaConnectDbContext(DbContextOptions<LankaConnectDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aggregatedcounter> Aggregatedcounters { get; set; }

    public virtual DbSet<Badge> Badges { get; set; }

    public virtual DbSet<Business> Businesses { get; set; }

    public virtual DbSet<BusinessImage> BusinessImages { get; set; }

    public virtual DbSet<Counter> Counters { get; set; }

    public virtual DbSet<EmailGroup> EmailGroups { get; set; }

    public virtual DbSet<EmailMessage> EmailMessages { get; set; }

    public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventAnalytic> EventAnalytics { get; set; }

    public virtual DbSet<EventBadge> EventBadges { get; set; }

    public virtual DbSet<EventEmailGroup> EventEmailGroups { get; set; }

    public virtual DbSet<EventImage> EventImages { get; set; }

    public virtual DbSet<EventTemplate> EventTemplates { get; set; }

    public virtual DbSet<EventVideo> EventVideos { get; set; }

    public virtual DbSet<EventVideo1> EventVideos1 { get; set; }

    public virtual DbSet<EventViewRecord> EventViewRecords { get; set; }

    public virtual DbSet<EventWaitingList> EventWaitingLists { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<Hash> Hashes { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<Jobparameter> Jobparameters { get; set; }

    public virtual DbSet<Jobqueue> Jobqueues { get; set; }

    public virtual DbSet<List> Lists { get; set; }

    public virtual DbSet<Lock> Locks { get; set; }

    public virtual DbSet<MetroArea> MetroAreas { get; set; }

    public virtual DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<ReferenceValue> ReferenceValues { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<Reply> Replies { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Schema> Schemas { get; set; }

    public virtual DbSet<Server> Servers { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Set> Sets { get; set; }

    public virtual DbSet<SignUpCommitment> SignUpCommitments { get; set; }

    public virtual DbSet<SignUpItem> SignUpItems { get; set; }

    public virtual DbSet<SignUpList> SignUpLists { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<StripeCustomer> StripeCustomers { get; set; }

    public virtual DbSet<StripeWebhookEvent> StripeWebhookEvents { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserCulturalInterest> UserCulturalInterests { get; set; }

    public virtual DbSet<UserEmailPreference> UserEmailPreferences { get; set; }

    public virtual DbSet<UserLanguage> UserLanguages { get; set; }

    public virtual DbSet<UserPreferredMetroArea> UserPreferredMetroAreas { get; set; }

    public virtual DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("hstore")
            .HasPostgresExtension("postgis");

        modelBuilder.Entity<Aggregatedcounter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("aggregatedcounter_pkey");

            entity.ToTable("aggregatedcounter", "hangfire");

            entity.HasIndex(e => e.Key, "aggregatedcounter_key_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Expireat).HasColumnName("expireat");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Badge>(entity =>
        {
            entity.ToTable("badges", "badges");

            entity.HasIndex(e => e.DefaultDurationDays, "IX_Badges_DefaultDurationDays").HasFilter("(\"DefaultDurationDays\" IS NOT NULL)");

            entity.HasIndex(e => e.DisplayOrder, "IX_Badges_DisplayOrder");

            entity.HasIndex(e => e.IsActive, "IX_Badges_IsActive");

            entity.HasIndex(e => e.IsSystem, "IX_Badges_IsSystem");

            entity.HasIndex(e => e.Name, "IX_Badges_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BlobName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSystem).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Position).HasMaxLength(20);
            entity.Property(e => e.PositionXDetail)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("1.0")
                .HasColumnName("position_x_detail");
            entity.Property(e => e.PositionXFeatured)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("1.0")
                .HasColumnName("position_x_featured");
            entity.Property(e => e.PositionXListing)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("1.0")
                .HasColumnName("position_x_listing");
            entity.Property(e => e.PositionYDetail)
                .HasPrecision(5, 4)
                .HasColumnName("position_y_detail");
            entity.Property(e => e.PositionYFeatured)
                .HasPrecision(5, 4)
                .HasColumnName("position_y_featured");
            entity.Property(e => e.PositionYListing)
                .HasPrecision(5, 4)
                .HasColumnName("position_y_listing");
            entity.Property(e => e.RotationDetail)
                .HasPrecision(5, 2)
                .HasColumnName("rotation_detail");
            entity.Property(e => e.RotationFeatured)
                .HasPrecision(5, 2)
                .HasColumnName("rotation_featured");
            entity.Property(e => e.RotationListing)
                .HasPrecision(5, 2)
                .HasColumnName("rotation_listing");
            entity.Property(e => e.SizeHeightDetail)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("0.21")
                .HasColumnName("size_height_detail");
            entity.Property(e => e.SizeHeightFeatured)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("0.26")
                .HasColumnName("size_height_featured");
            entity.Property(e => e.SizeHeightListing)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("0.26")
                .HasColumnName("size_height_listing");
            entity.Property(e => e.SizeWidthDetail)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("0.21")
                .HasColumnName("size_width_detail");
            entity.Property(e => e.SizeWidthFeatured)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("0.26")
                .HasColumnName("size_width_featured");
            entity.Property(e => e.SizeWidthListing)
                .HasPrecision(5, 4)
                .HasDefaultValueSql("0.26")
                .HasColumnName("size_width_listing");
        });

        modelBuilder.Entity<Business>(entity =>
        {
            entity.ToTable("businesses", "business");

            entity.HasIndex(e => e.Category, "IX_Business_Category");

            entity.HasIndex(e => e.CreatedAt, "IX_Business_CreatedAt");

            entity.HasIndex(e => e.IsVerified, "IX_Business_IsVerified");

            entity.HasIndex(e => e.OwnerId, "IX_Business_OwnerId");

            entity.HasIndex(e => e.Rating, "IX_Business_Rating");

            entity.HasIndex(e => e.Status, "IX_Business_Status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AddressCity)
                .HasMaxLength(50)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.AddressCountry)
                .HasMaxLength(50)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.AddressState)
                .HasMaxLength(50)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.AddressStreet)
                .HasMaxLength(200)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.AddressZipCode)
                .HasMaxLength(10)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.BusinessHours).HasColumnType("json");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.ContactEmail).HasMaxLength(100);
            entity.Property(e => e.ContactFacebook).HasMaxLength(100);
            entity.Property(e => e.ContactInstagram).HasMaxLength(50);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.ContactTwitter).HasMaxLength(50);
            entity.Property(e => e.ContactWebsite).HasMaxLength(200);
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.LocationLatitude).HasPrecision(10, 8);
            entity.Property(e => e.LocationLongitude).HasPrecision(11, 8);
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.Rating).HasPrecision(3, 2);
            entity.Property(e => e.ReviewCount).HasDefaultValue(0);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Version).HasDefaultValue(0L);
        });

        modelBuilder.Entity<BusinessImage>(entity =>
        {
            entity.ToTable("BusinessImage");

            entity.HasIndex(e => e.BusinessId, "IX_BusinessImage_BusinessId");

            entity.HasOne(d => d.Business).WithMany(p => p.BusinessImages).HasForeignKey(d => d.BusinessId);
        });

        modelBuilder.Entity<Counter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("counter_pkey");

            entity.ToTable("counter", "hangfire");

            entity.HasIndex(e => e.Expireat, "ix_hangfire_counter_expireat");

            entity.HasIndex(e => e.Key, "ix_hangfire_counter_key");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Expireat).HasColumnName("expireat");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<EmailGroup>(entity =>
        {
            entity.ToTable("email_groups", "communications");

            entity.HasIndex(e => e.IsActive, "IX_EmailGroups_IsActive");

            entity.HasIndex(e => e.OwnerId, "IX_EmailGroups_OwnerId");

            entity.HasIndex(e => new { e.OwnerId, e.IsActive }, "IX_EmailGroups_Owner_IsActive");

            entity.HasIndex(e => new { e.OwnerId, e.Name }, "IX_EmailGroups_Owner_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.EmailAddresses).HasColumnName("email_addresses");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.ToTable("email_messages", "communications");

            entity.HasIndex(e => e.CreatedAt, "IX_EmailMessages_CreatedAt");

            entity.HasIndex(e => e.Priority, "IX_EmailMessages_Priority");

            entity.HasIndex(e => new { e.RetryCount, e.Status }, "IX_EmailMessages_RetryCount_Status");

            entity.HasIndex(e => e.Status, "IX_EmailMessages_Status");

            entity.HasIndex(e => new { e.Status, e.NextRetryAt }, "IX_EmailMessages_Status_NextRetryAt");

            entity.HasIndex(e => e.Type, "IX_EmailMessages_Type");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BccEmails)
                .HasColumnType("jsonb")
                .HasColumnName("bcc_emails");
            entity.Property(e => e.CcEmails)
                .HasColumnType("jsonb")
                .HasColumnName("cc_emails");
            entity.Property(e => e.ClickedAt).HasColumnName("clicked_at");
            entity.Property(e => e.ConcurrentAccessAttempts).HasDefaultValue(0);
            entity.Property(e => e.CulturalContext)
                .HasColumnType("jsonb")
                .HasColumnName("cultural_context");
            entity.Property(e => e.CulturalDelayBypassed).HasDefaultValue(false);
            entity.Property(e => e.CulturalTimingOptimized).HasDefaultValue(false);
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.DeliveryConfirmationReceived).HasDefaultValue(false);
            entity.Property(e => e.DiasporaOptimized).HasDefaultValue(false);
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000)
                .HasColumnName("error_message");
            entity.Property(e => e.FailedAt).HasColumnName("failed_at");
            entity.Property(e => e.FromEmail)
                .HasMaxLength(255)
                .HasColumnName("from_email");
            entity.Property(e => e.GeographicOptimization).HasDefaultValue(false);
            entity.Property(e => e.HasAllRecipientsDelivered).HasDefaultValue(false);
            entity.Property(e => e.HtmlContent).HasColumnName("html_content");
            entity.Property(e => e.MaxRetries).HasColumnName("max_retries");
            entity.Property(e => e.NextRetryAt).HasColumnName("next_retry_at");
            entity.Property(e => e.OpenedAt).HasColumnName("opened_at");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.RecipientStatuses)
                .HasColumnType("jsonb")
                .HasColumnName("recipient_statuses");
            entity.Property(e => e.ReligiousObservanceConsidered).HasDefaultValue(false);
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(200)
                .HasColumnName("subject");
            entity.Property(e => e.TemplateData)
                .HasColumnType("jsonb")
                .HasColumnName("template_data");
            entity.Property(e => e.TemplateName)
                .HasMaxLength(100)
                .HasColumnName("template_name");
            entity.Property(e => e.TextContent).HasColumnName("text_content");
            entity.Property(e => e.ToEmails)
                .HasColumnType("jsonb")
                .HasColumnName("to_emails");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("email_templates", "communications");

            entity.HasIndex(e => e.Category, "IX_EmailTemplates_Category");

            entity.HasIndex(e => new { e.Category, e.IsActive }, "IX_EmailTemplates_Category_IsActive");

            entity.HasIndex(e => e.CreatedAt, "IX_EmailTemplates_CreatedAt");

            entity.HasIndex(e => e.IsActive, "IX_EmailTemplates_IsActive");

            entity.HasIndex(e => e.Name, "IX_EmailTemplates_Name").IsUnique();

            entity.HasIndex(e => e.Type, "IX_EmailTemplates_Type");

            entity.HasIndex(e => new { e.Type, e.IsActive }, "IX_EmailTemplates_Type_IsActive");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.HtmlTemplate).HasColumnName("html_template");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SubjectTemplate)
                .HasMaxLength(200)
                .HasColumnName("subject_template");
            entity.Property(e => e.Tags)
                .HasMaxLength(500)
                .HasColumnName("tags");
            entity.Property(e => e.TextTemplate).HasColumnName("text_template");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events", "events");

            entity.HasIndex(e => e.SearchVector, "idx_events_search_vector").HasMethod("gin");

            entity.HasIndex(e => e.AddressCity, "ix_events_city").HasFilter("(address_city IS NOT NULL)");

            entity.HasIndex(e => e.Location, "ix_events_location_gist")
                .HasFilter("(location IS NOT NULL)")
                .HasMethod("gist");

            entity.HasIndex(e => e.OrganizerId, "ix_events_organizer_id");

            entity.HasIndex(e => e.StartDate, "ix_events_start_date");

            entity.HasIndex(e => e.Status, "ix_events_status");

            entity.HasIndex(e => new { e.Status, e.AddressCity, e.StartDate }, "ix_events_status_city_startdate").HasFilter("(address_city IS NOT NULL)");

            entity.HasIndex(e => new { e.Status, e.StartDate }, "ix_events_status_start_date");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AddressCity)
                .HasMaxLength(100)
                .HasColumnName("address_city");
            entity.Property(e => e.AddressCountry)
                .HasMaxLength(100)
                .HasColumnName("address_country");
            entity.Property(e => e.AddressState)
                .HasMaxLength(100)
                .HasColumnName("address_state");
            entity.Property(e => e.AddressStreet)
                .HasMaxLength(200)
                .HasColumnName("address_street");
            entity.Property(e => e.AddressZipCode)
                .HasMaxLength(20)
                .HasColumnName("address_zip_code");
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.Category)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Community'::character varying");
            entity.Property(e => e.CoordinatesLatitude)
                .HasPrecision(10, 7)
                .HasColumnName("coordinates_latitude");
            entity.Property(e => e.CoordinatesLongitude)
                .HasPrecision(10, 7)
                .HasColumnName("coordinates_longitude");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("description");
            entity.Property(e => e.HasLocation)
                .HasDefaultValue(true)
                .HasColumnName("has_location");
            entity.Property(e => e.Location)
                .HasComputedColumnSql("\nCASE\n    WHEN ((coordinates_latitude IS NOT NULL) AND (coordinates_longitude IS NOT NULL)) THEN (st_setsrid(st_makepoint((coordinates_longitude)::double precision, (coordinates_latitude)::double precision), 4326))::geography\n    ELSE NULL::geography\nEND", true)
                .HasColumnType("geography(Point,4326)")
                .HasColumnName("location");
            entity.Property(e => e.Pricing)
                .HasColumnType("jsonb")
                .HasColumnName("pricing");
            entity.Property(e => e.SearchVector)
                .HasComputedColumnSql("(setweight(to_tsvector('english'::regconfig, (COALESCE(title, ''::character varying))::text), 'A'::\"char\") || setweight(to_tsvector('english'::regconfig, (COALESCE(description, ''::character varying))::text), 'B'::\"char\"))", true)
                .HasColumnName("search_vector");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Draft'::character varying");
            entity.Property(e => e.TicketPrice)
                .HasColumnType("jsonb")
                .HasColumnName("ticket_price");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("title");
        });

        modelBuilder.Entity<EventAnalytic>(entity =>
        {
            entity.ToTable("event_analytics", "analytics");

            entity.HasIndex(e => e.EventId, "ix_event_analytics_event_id_unique").IsUnique();

            entity.HasIndex(e => e.LastViewedAt, "ix_event_analytics_last_viewed_at");

            entity.HasIndex(e => e.TotalViews, "ix_event_analytics_total_views");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.LastViewedAt).HasColumnName("last_viewed_at");
            entity.Property(e => e.RegistrationCount)
                .HasDefaultValue(0)
                .HasColumnName("registration_count");
            entity.Property(e => e.ShareCount)
                .HasDefaultValue(0)
                .HasColumnName("share_count");
            entity.Property(e => e.TotalViews)
                .HasDefaultValue(0)
                .HasColumnName("total_views");
            entity.Property(e => e.UniqueViewers)
                .HasDefaultValue(0)
                .HasColumnName("unique_viewers");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<EventBadge>(entity =>
        {
            entity.ToTable("event_badges", "badges");

            entity.HasIndex(e => e.BadgeId, "IX_EventBadges_BadgeId");

            entity.HasIndex(e => e.EventId, "IX_EventBadges_EventId");

            entity.HasIndex(e => new { e.EventId, e.BadgeId }, "IX_EventBadges_EventId_BadgeId").IsUnique();

            entity.HasIndex(e => e.ExpiresAt, "IX_EventBadges_ExpiresAt").HasFilter("(\"ExpiresAt\" IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Badge).WithMany(p => p.EventBadges)
                .HasForeignKey(d => d.BadgeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Event).WithMany(p => p.EventBadges).HasForeignKey(d => d.EventId);
        });

        modelBuilder.Entity<EventEmailGroup>(entity =>
        {
            entity.HasKey(e => new { e.EventId, e.EmailGroupId });

            entity.ToTable("event_email_groups");

            entity.HasIndex(e => e.EmailGroupId, "IX_event_email_groups_email_group_id");

            entity.HasIndex(e => e.EventId, "IX_event_email_groups_event_id");

            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.EmailGroupId).HasColumnName("email_group_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("assigned_at");

            entity.HasOne(d => d.EmailGroup).WithMany(p => p.EventEmailGroups).HasForeignKey(d => d.EmailGroupId);

            entity.HasOne(d => d.Event).WithMany(p => p.EventEmailGroups).HasForeignKey(d => d.EventId);
        });

        modelBuilder.Entity<EventImage>(entity =>
        {
            entity.ToTable("EventImages", "events");

            entity.HasIndex(e => e.EventId, "IX_EventImages_EventId");

            entity.HasIndex(e => new { e.EventId, e.DisplayOrder }, "IX_EventImages_EventId_DisplayOrder").IsUnique();

            entity.HasIndex(e => e.EventId, "IX_EventImages_EventId_IsPrimary_True")
                .IsUnique()
                .HasFilter("(\"IsPrimary\" = true)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BlobName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);

            entity.HasOne(d => d.Event).WithOne(p => p.EventImage).HasForeignKey<EventImage>(d => d.EventId);
        });

        modelBuilder.Entity<EventTemplate>(entity =>
        {
            entity.ToTable("event_templates", "events");

            entity.HasIndex(e => new { e.IsActive, e.Category, e.DisplayOrder }, "idx_event_templates_active_category_order");

            entity.HasIndex(e => e.Category, "idx_event_templates_category");

            entity.HasIndex(e => e.DisplayOrder, "idx_event_templates_display_order");

            entity.HasIndex(e => e.IsActive, "idx_event_templates_is_active");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("display_order");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.TemplateDataJson)
                .HasColumnType("jsonb")
                .HasColumnName("template_data_json");
            entity.Property(e => e.ThumbnailSvg).HasColumnName("thumbnail_svg");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<EventVideo>(entity =>
        {
            entity.HasIndex(e => e.EventId, "IX_EventVideos_EventId");

            entity.HasIndex(e => new { e.EventId, e.DisplayOrder }, "IX_EventVideos_EventId_DisplayOrder").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BlobName).HasMaxLength(255);
            entity.Property(e => e.Format).HasMaxLength(50);
            entity.Property(e => e.ThumbnailBlobName).HasMaxLength(255);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.VideoUrl).HasMaxLength(500);

            entity.HasOne(d => d.Event).WithMany(p => p.EventVideos).HasForeignKey(d => d.EventId);
        });

        modelBuilder.Entity<EventVideo1>(entity =>
        {
            entity.ToTable("EventVideos", "events");

            entity.HasIndex(e => e.EventId, "IX_EventVideos_EventId");

            entity.HasIndex(e => new { e.EventId, e.DisplayOrder }, "IX_EventVideos_EventId_DisplayOrder").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BlobName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Format).HasMaxLength(50);
            entity.Property(e => e.ThumbnailBlobName).HasMaxLength(255);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.VideoUrl).HasMaxLength(500);

            entity.HasOne(d => d.Event).WithMany(p => p.EventVideo1s).HasForeignKey(d => d.EventId);
        });

        modelBuilder.Entity<EventViewRecord>(entity =>
        {
            entity.ToTable("event_view_records", "analytics");

            entity.HasIndex(e => new { e.EventId, e.IpAddress, e.ViewedAt }, "ix_event_view_records_dedup_ip");

            entity.HasIndex(e => new { e.EventId, e.UserId, e.ViewedAt }, "ix_event_view_records_dedup_user");

            entity.HasIndex(e => e.EventId, "ix_event_view_records_event_id");

            entity.HasIndex(e => e.IpAddress, "ix_event_view_records_ip_address");

            entity.HasIndex(e => e.UserId, "ix_event_view_records_user_id");

            entity.HasIndex(e => e.ViewedAt, "ix_event_view_records_viewed_at");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ViewedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("viewed_at");
        });

        modelBuilder.Entity<EventWaitingList>(entity =>
        {
            entity.ToTable("event_waiting_list", "events");

            entity.HasIndex(e => new { e.EventId, e.Position }, "ix_event_waiting_list_event_position");

            entity.HasIndex(e => new { e.EventId, e.UserId }, "ix_event_waiting_list_event_user").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Event).WithMany(p => p.EventWaitingLists).HasForeignKey(d => d.EventId);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Id });

            entity.ToTable("external_logins", "identity");

            entity.HasIndex(e => new { e.Provider, e.ExternalProviderId }, "ix_external_logins_provider_external_id").IsUnique();

            entity.HasIndex(e => e.UserId, "ix_external_logins_user_id");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExternalProviderId)
                .HasMaxLength(255)
                .HasColumnName("external_provider_id");
            entity.Property(e => e.LinkedAt).HasColumnName("linked_at");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.ProviderEmail)
                .HasMaxLength(255)
                .HasColumnName("provider_email");

            entity.HasOne(d => d.User).WithMany(p => p.ExternalLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Hash>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("hash_pkey");

            entity.ToTable("hash", "hangfire");

            entity.HasIndex(e => new { e.Key, e.Field }, "hash_key_field_key").IsUnique();

            entity.HasIndex(e => e.Expireat, "ix_hangfire_hash_expireat");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Expireat).HasColumnName("expireat");
            entity.Property(e => e.Field).HasColumnName("field");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("job_pkey");

            entity.ToTable("job", "hangfire");

            entity.HasIndex(e => e.Expireat, "ix_hangfire_job_expireat");

            entity.HasIndex(e => e.Statename, "ix_hangfire_job_statename");

            entity.HasIndex(e => e.Statename, "ix_hangfire_job_statename_is_not_null").HasFilter("(statename IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Arguments)
                .HasColumnType("jsonb")
                .HasColumnName("arguments");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Expireat).HasColumnName("expireat");
            entity.Property(e => e.Invocationdata)
                .HasColumnType("jsonb")
                .HasColumnName("invocationdata");
            entity.Property(e => e.Stateid).HasColumnName("stateid");
            entity.Property(e => e.Statename).HasColumnName("statename");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
        });

        modelBuilder.Entity<Jobparameter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("jobparameter_pkey");

            entity.ToTable("jobparameter", "hangfire");

            entity.HasIndex(e => new { e.Jobid, e.Name }, "ix_hangfire_jobparameter_jobidandname");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Jobid).HasColumnName("jobid");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.Job).WithMany(p => p.Jobparameters)
                .HasForeignKey(d => d.Jobid)
                .HasConstraintName("jobparameter_jobid_fkey");
        });

        modelBuilder.Entity<Jobqueue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("jobqueue_pkey");

            entity.ToTable("jobqueue", "hangfire");

            entity.HasIndex(e => new { e.Fetchedat, e.Queue, e.Jobid }, "ix_hangfire_jobqueue_fetchedat_queue_jobid").HasNullSortOrder(new[] { NullSortOrder.NullsFirst, NullSortOrder.NullsLast, NullSortOrder.NullsLast });

            entity.HasIndex(e => new { e.Jobid, e.Queue }, "ix_hangfire_jobqueue_jobidandqueue");

            entity.HasIndex(e => new { e.Queue, e.Fetchedat }, "ix_hangfire_jobqueue_queueandfetchedat");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Fetchedat).HasColumnName("fetchedat");
            entity.Property(e => e.Jobid).HasColumnName("jobid");
            entity.Property(e => e.Queue).HasColumnName("queue");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
        });

        modelBuilder.Entity<List>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("list_pkey");

            entity.ToTable("list", "hangfire");

            entity.HasIndex(e => e.Expireat, "ix_hangfire_list_expireat");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Expireat).HasColumnName("expireat");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Lock>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("lock", "hangfire");

            entity.HasIndex(e => e.Resource, "lock_resource_key").IsUnique();

            entity.Property(e => e.Acquired).HasColumnName("acquired");
            entity.Property(e => e.Resource).HasColumnName("resource");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
        });

        modelBuilder.Entity<MetroArea>(entity =>
        {
            entity.ToTable("metro_areas", "events");

            entity.HasIndex(e => e.IsActive, "idx_metro_areas_is_active");

            entity.HasIndex(e => e.Name, "idx_metro_areas_name");

            entity.HasIndex(e => e.State, "idx_metro_areas_state");

            entity.HasIndex(e => new { e.State, e.IsStateLevelArea }, "idx_metro_areas_state_level").HasFilter("(is_state_level_area = true)");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CenterLatitude).HasColumnName("center_latitude");
            entity.Property(e => e.CenterLongitude).HasColumnName("center_longitude");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsStateLevelArea)
                .HasDefaultValue(false)
                .HasColumnName("is_state_level_area");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.RadiusMiles).HasColumnName("radius_miles");
            entity.Property(e => e.State)
                .HasMaxLength(2)
                .HasColumnName("state");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<NewsletterSubscriber>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("newsletter_subscribers_pkey");

            entity.ToTable("newsletter_subscribers", "communications");

            entity.HasIndex(e => new { e.IsActive, e.IsConfirmed }, "idx_newsletter_subscribers_active_confirmed");

            entity.HasIndex(e => e.ReceiveAllLocations, "idx_newsletter_subscribers_all_locations").HasFilter("((is_active = true) AND (is_confirmed = true) AND (receive_all_locations = true))");

            entity.HasIndex(e => e.ConfirmationToken, "idx_newsletter_subscribers_confirmation_token");

            entity.HasIndex(e => e.Email, "idx_newsletter_subscribers_email").IsUnique();

            entity.HasIndex(e => e.MetroAreaId, "idx_newsletter_subscribers_metro_area_active").HasFilter("((is_active = true) AND (is_confirmed = true))");

            entity.HasIndex(e => e.MetroAreaId, "idx_newsletter_subscribers_metro_area_id");

            entity.HasIndex(e => e.UnsubscribeToken, "idx_newsletter_subscribers_unsubscribe_token");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ConfirmationSentAt).HasColumnName("confirmation_sent_at");
            entity.Property(e => e.ConfirmationToken)
                .HasMaxLength(100)
                .HasColumnName("confirmation_token");
            entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsConfirmed)
                .HasDefaultValue(false)
                .HasColumnName("is_confirmed");
            entity.Property(e => e.MetroAreaId).HasColumnName("metro_area_id");
            entity.Property(e => e.ReceiveAllLocations)
                .HasDefaultValue(false)
                .HasColumnName("receive_all_locations");
            entity.Property(e => e.UnsubscribeToken)
                .HasMaxLength(100)
                .HasColumnName("unsubscribe_token");
            entity.Property(e => e.UnsubscribedAt).HasColumnName("unsubscribed_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Version)
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .HasColumnName("version");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications", "notifications");

            entity.HasIndex(e => e.CreatedAt, "ix_notifications_created_at");

            entity.HasIndex(e => e.UserId, "ix_notifications_user_id");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "ix_notifications_user_id_is_read");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.RelatedEntityId).HasMaxLength(100);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<ReferenceValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_reference_values");

            entity.ToTable("reference_values", "reference_data");

            entity.HasIndex(e => e.DisplayOrder, "idx_reference_values_display_order");

            entity.HasIndex(e => e.EnumType, "idx_reference_values_enum_type");

            entity.HasIndex(e => e.IsActive, "idx_reference_values_is_active");

            entity.HasIndex(e => e.Metadata, "idx_reference_values_metadata").HasMethod("gin");

            entity.HasIndex(e => new { e.EnumType, e.Code }, "uq_reference_values_type_code").IsUnique();

            entity.HasIndex(e => new { e.EnumType, e.IntValue }, "uq_reference_values_type_int_value").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("display_order");
            entity.Property(e => e.EnumType)
                .HasMaxLength(100)
                .HasColumnName("enum_type");
            entity.Property(e => e.IntValue).HasColumnName("int_value");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.ToTable("registrations", "events");

            entity.HasIndex(e => e.EventId, "ix_registrations_event_id");

            entity.HasIndex(e => e.UserId, "ix_registrations_user_id");

            entity.HasIndex(e => new { e.UserId, e.Status }, "ix_registrations_user_status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AttendeeInfo)
                .HasColumnType("jsonb")
                .HasColumnName("attendee_info");
            entity.Property(e => e.Attendees)
                .HasColumnType("jsonb")
                .HasColumnName("attendees");
            entity.Property(e => e.Contact)
                .HasColumnType("jsonb")
                .HasColumnName("contact");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PaymentStatus).HasDefaultValue(0);
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Confirmed'::character varying");
            entity.Property(e => e.TotalPriceAmount)
                .HasPrecision(18, 2)
                .HasColumnName("total_price_amount");
            entity.Property(e => e.TotalPriceCurrency)
                .HasMaxLength(3)
                .HasColumnName("total_price_currency");

            entity.HasOne(d => d.Event).WithMany(p => p.Registrations).HasForeignKey(d => d.EventId);
        });

        modelBuilder.Entity<Reply>(entity =>
        {
            entity.ToTable("replies", "community");

            entity.HasIndex(e => e.AuthorId, "ix_replies_author_id");

            entity.HasIndex(e => e.HelpfulVotes, "ix_replies_helpful_votes");

            entity.HasIndex(e => e.ParentReplyId, "ix_replies_parent_id");

            entity.HasIndex(e => new { e.IsMarkedAsSolution, e.TopicId }, "ix_replies_solution_topic");

            entity.HasIndex(e => new { e.TopicId, e.CreatedAt }, "ix_replies_topic_created");

            entity.HasIndex(e => e.TopicId, "ix_replies_topic_id");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Content)
                .HasMaxLength(10000)
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.HelpfulVotes).HasDefaultValue(0);
            entity.Property(e => e.IsMarkedAsSolution).HasDefaultValue(false);

            entity.HasOne(d => d.ParentReply).WithMany(p => p.InverseParentReply)
                .HasForeignKey(d => d.ParentReplyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Topic).WithMany(p => p.Replies).HasForeignKey(d => d.TopicId);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews", "business");

            entity.HasIndex(e => e.BusinessId, "ix_reviews_business_id");

            entity.HasIndex(e => new { e.BusinessId, e.ReviewerId }, "ix_reviews_business_reviewer_unique").IsUnique();

            entity.HasIndex(e => new { e.BusinessId, e.Status }, "ix_reviews_business_status");

            entity.HasIndex(e => new { e.BusinessId, e.Status, e.CreatedAt }, "ix_reviews_business_status_created");

            entity.HasIndex(e => e.CreatedAt, "ix_reviews_created_at");

            entity.HasIndex(e => e.ReviewerId, "ix_reviews_reviewer_id");

            entity.HasIndex(e => new { e.ReviewerId, e.Status }, "ix_reviews_reviewer_status");

            entity.HasIndex(e => e.Status, "ix_reviews_status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Cons)
                .HasColumnType("jsonb")
                .HasColumnName("cons");
            entity.Property(e => e.Content)
                .HasMaxLength(2000)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ModerationNotes).HasMaxLength(1000);
            entity.Property(e => e.Pros)
                .HasColumnType("jsonb")
                .HasColumnName("pros");
            entity.Property(e => e.Rating)
                .HasDefaultValue(0)
                .HasColumnName("rating");
            entity.Property(e => e.Status).HasDefaultValueSql("'Pending'::text");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("title");

            entity.HasOne(d => d.Business).WithMany(p => p.Reviews).HasForeignKey(d => d.BusinessId);
        });

        modelBuilder.Entity<Schema>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("schema_pkey");

            entity.ToTable("schema", "hangfire");

            entity.Property(e => e.Version)
                .ValueGeneratedNever()
                .HasColumnName("version");
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("server_pkey");

            entity.ToTable("server", "hangfire");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Data)
                .HasColumnType("jsonb")
                .HasColumnName("data");
            entity.Property(e => e.Lastheartbeat).HasColumnName("lastheartbeat");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("services", "business");

            entity.HasIndex(e => e.BusinessId, "IX_Service_BusinessId");

            entity.HasIndex(e => e.IsActive, "IX_Service_IsActive");

            entity.HasIndex(e => e.Name, "IX_Service_Name");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Duration).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PriceAmount).HasPrecision(10, 2);

            entity.HasOne(d => d.Business).WithMany(p => p.Services).HasForeignKey(d => d.BusinessId);
        });

        modelBuilder.Entity<Set>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("set_pkey");

            entity.ToTable("set", "hangfire");

            entity.HasIndex(e => e.Expireat, "ix_hangfire_set_expireat");

            entity.HasIndex(e => new { e.Key, e.Score }, "ix_hangfire_set_key_score");

            entity.HasIndex(e => new { e.Key, e.Value }, "set_key_value_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Expireat).HasColumnName("expireat");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<SignUpCommitment>(entity =>
        {
            entity.ToTable("sign_up_commitments", "events");

            entity.HasIndex(e => e.SignUpListId, "IX_sign_up_commitments_SignUpListId");

            entity.HasIndex(e => e.SignUpItemId, "ix_sign_up_commitments_sign_up_item_id");

            entity.HasIndex(e => e.UserId, "ix_sign_up_commitments_user_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CommittedAt).HasColumnName("committed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ItemDescription)
                .HasMaxLength(500)
                .HasColumnName("item_description");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SignUpItemId).HasColumnName("sign_up_item_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.SignUpItem).WithMany(p => p.SignUpCommitments)
                .HasForeignKey(d => d.SignUpItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.SignUpList).WithMany(p => p.SignUpCommitments)
                .HasForeignKey(d => d.SignUpListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SignUpItem>(entity =>
        {
            entity.ToTable("sign_up_items", "events");

            entity.HasIndex(e => e.ItemCategory, "ix_sign_up_items_category");

            entity.HasIndex(e => e.SignUpListId, "ix_sign_up_items_list_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.ItemCategory).HasColumnName("item_category");
            entity.Property(e => e.ItemDescription)
                .HasMaxLength(200)
                .HasColumnName("item_description");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasColumnName("notes");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.RemainingQuantity).HasColumnName("remaining_quantity");
            entity.Property(e => e.SignUpListId).HasColumnName("sign_up_list_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.SignUpList).WithMany(p => p.SignUpItems).HasForeignKey(d => d.SignUpListId);
        });

        modelBuilder.Entity<SignUpList>(entity =>
        {
            entity.ToTable("sign_up_lists", "events");

            entity.HasIndex(e => e.Category, "ix_sign_up_lists_category");

            entity.HasIndex(e => e.EventId, "ix_sign_up_lists_event_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.HasMandatoryItems)
                .HasDefaultValue(false)
                .HasColumnName("has_mandatory_items");
            entity.Property(e => e.HasOpenItems)
                .HasDefaultValue(false)
                .HasColumnName("has_open_items");
            entity.Property(e => e.HasPreferredItems)
                .HasDefaultValue(false)
                .HasColumnName("has_preferred_items");
            entity.Property(e => e.HasSuggestedItems)
                .HasDefaultValue(false)
                .HasColumnName("has_suggested_items");
            entity.Property(e => e.PredefinedItems)
                .HasColumnType("jsonb")
                .HasColumnName("predefined_items");
            entity.Property(e => e.SignUpType)
                .HasMaxLength(20)
                .HasColumnName("sign_up_type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Event).WithMany(p => p.SignUpLists).HasForeignKey(d => d.EventId);
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("state_pkey");

            entity.ToTable("state", "hangfire");

            entity.HasIndex(e => e.Jobid, "ix_hangfire_state_jobid");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Data)
                .HasColumnType("jsonb")
                .HasColumnName("data");
            entity.Property(e => e.Jobid).HasColumnName("jobid");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.Updatecount)
                .HasDefaultValue(0)
                .HasColumnName("updatecount");

            entity.HasOne(d => d.Job).WithMany(p => p.States)
                .HasForeignKey(d => d.Jobid)
                .HasConstraintName("state_jobid_fkey");
        });

        modelBuilder.Entity<StripeCustomer>(entity =>
        {
            entity.ToTable("stripe_customers", "payments");

            entity.HasIndex(e => e.StripeCustomerId, "ix_stripe_customers_stripe_customer_id").IsUnique();

            entity.HasIndex(e => e.UserId, "ix_stripe_customers_user_id").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.StripeCreatedAt).HasColumnName("stripe_created_at");
            entity.Property(e => e.StripeCustomerId)
                .HasMaxLength(255)
                .HasColumnName("stripe_customer_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<StripeWebhookEvent>(entity =>
        {
            entity.ToTable("stripe_webhook_events", "payments");

            entity.HasIndex(e => e.EventId, "ix_stripe_webhook_events_event_id").IsUnique();

            entity.HasIndex(e => e.EventType, "ix_stripe_webhook_events_event_type");

            entity.HasIndex(e => e.Processed, "ix_stripe_webhook_events_processed");

            entity.HasIndex(e => new { e.Processed, e.CreatedAt }, "ix_stripe_webhook_events_processed_created_at");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AttemptCount)
                .HasDefaultValue(0)
                .HasColumnName("attempt_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000)
                .HasColumnName("error_message");
            entity.Property(e => e.EventId)
                .HasMaxLength(255)
                .HasColumnName("event_id");
            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .HasColumnName("event_type");
            entity.Property(e => e.Processed)
                .HasDefaultValue(false)
                .HasColumnName("processed");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets", "events");

            entity.HasIndex(e => e.EventId, "IX_tickets_EventId");

            entity.HasIndex(e => e.RegistrationId, "IX_tickets_RegistrationId");

            entity.HasIndex(e => e.TicketCode, "IX_tickets_TicketCode").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_tickets_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.PdfBlobUrl).HasMaxLength(500);
            entity.Property(e => e.TicketCode).HasMaxLength(50);

            entity.HasOne(d => d.Event).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Registration).WithMany(p => p.Tickets).HasForeignKey(d => d.RegistrationId);

            entity.HasOne(d => d.User).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("topics", "community");

            entity.HasIndex(e => e.AuthorId, "ix_topics_author_id");

            entity.HasIndex(e => e.Category, "ix_topics_category");

            entity.HasIndex(e => e.ForumId, "ix_topics_forum_id");

            entity.HasIndex(e => new { e.ForumId, e.Status, e.UpdatedAt }, "ix_topics_forum_status_updated");

            entity.HasIndex(e => new { e.IsPinned, e.UpdatedAt }, "ix_topics_pinned_updated");

            entity.HasIndex(e => e.Status, "ix_topics_status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Content)
                .HasMaxLength(10000)
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsPinned).HasDefaultValue(false);
            entity.Property(e => e.LockReason).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Active'::character varying");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("title");
            entity.Property(e => e.ViewCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "identity");

            entity.HasIndex(e => e.CreatedAt, "ix_users_created_at");

            entity.HasIndex(e => e.Email, "ix_users_email").IsUnique();

            entity.HasIndex(e => e.EmailVerificationToken, "ix_users_email_verification_token");

            entity.HasIndex(e => e.ExternalProviderId, "ix_users_external_provider_id");

            entity.HasIndex(e => e.IdentityProvider, "ix_users_identity_provider");

            entity.HasIndex(e => new { e.IdentityProvider, e.ExternalProviderId }, "ix_users_identity_provider_external_id");

            entity.HasIndex(e => e.IsActive, "ix_users_is_active");

            entity.HasIndex(e => e.IsEmailVerified, "ix_users_is_email_verified");

            entity.HasIndex(e => e.LastLoginAt, "ix_users_last_login_at");

            entity.HasIndex(e => e.PasswordResetToken, "ix_users_password_reset_token");

            entity.HasIndex(e => e.Role, "ix_users_role");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailVerificationToken).HasMaxLength(255);
            entity.Property(e => e.ExternalProviderId).HasMaxLength(255);
            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IdentityProvider).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PasswordResetToken).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.Role).HasDefaultValue(1);
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .HasColumnName("state");
            entity.Property(e => e.ZipCode)
                .HasMaxLength(20)
                .HasColumnName("zip_code");
        });

        modelBuilder.Entity<UserCulturalInterest>(entity =>
        {
            entity.ToTable("user_cultural_interests", "users");

            entity.HasIndex(e => new { e.UserId, e.InterestCode }, "ix_user_cultural_interests_user_code").IsUnique();

            entity.Property(e => e.InterestCode)
                .HasMaxLength(50)
                .HasColumnName("interest_code");

            entity.HasOne(d => d.User).WithMany(p => p.UserCulturalInterests).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserEmailPreference>(entity =>
        {
            entity.ToTable("user_email_preferences", "communications");

            entity.HasIndex(e => e.AllowMarketing, "IX_UserEmailPreferences_AllowMarketing");

            entity.HasIndex(e => e.AllowNotifications, "IX_UserEmailPreferences_AllowNotifications");

            entity.HasIndex(e => e.CreatedAt, "IX_UserEmailPreferences_CreatedAt");

            entity.HasIndex(e => e.PreferredLanguage, "IX_UserEmailPreferences_PreferredLanguage");

            entity.HasIndex(e => e.UserId, "IX_UserEmailPreferences_UserId_Unique").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AllowMarketing)
                .HasDefaultValue(false)
                .HasColumnName("allow_marketing");
            entity.Property(e => e.AllowNewsletters)
                .HasDefaultValue(true)
                .HasColumnName("allow_newsletters");
            entity.Property(e => e.AllowNotifications)
                .HasDefaultValue(true)
                .HasColumnName("allow_notifications");
            entity.Property(e => e.AllowTransactional)
                .HasDefaultValue(true)
                .HasColumnName("allow_transactional");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PreferredLanguage)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en-US'::character varying")
                .HasColumnName("preferred_language");
            entity.Property(e => e.Timezone)
                .HasMaxLength(100)
                .HasColumnName("timezone");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.UserEmailPreference)
                .HasForeignKey<UserEmailPreference>(d => d.UserId)
                .HasConstraintName("FK_UserEmailPreferences_Users_UserId");
        });

        modelBuilder.Entity<UserLanguage>(entity =>
        {
            entity.ToTable("user_languages", "users");

            entity.HasIndex(e => e.UserId, "IX_user_languages_UserId");

            entity.HasIndex(e => e.LanguageCode, "ix_user_languages_code");

            entity.Property(e => e.LanguageCode)
                .HasMaxLength(10)
                .HasColumnName("language_code");
            entity.Property(e => e.ProficiencyLevel).HasColumnName("proficiency_level");

            entity.HasOne(d => d.User).WithMany(p => p.UserLanguages).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserPreferredMetroArea>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.MetroAreaId });

            entity.ToTable("user_preferred_metro_areas", "identity");

            entity.HasIndex(e => e.MetroAreaId, "ix_user_preferred_metro_areas_metro_area_id");

            entity.HasIndex(e => e.UserId, "ix_user_preferred_metro_areas_user_id");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.MetroAreaId).HasColumnName("metro_area_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.MetroArea).WithMany(p => p.UserPreferredMetroAreas)
                .HasForeignKey(d => d.MetroAreaId)
                .HasConstraintName("fk_user_preferred_metro_areas_metro_area_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserPreferredMetroAreas)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_preferred_metro_areas_user_id");
        });

        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.ToTable("user_refresh_tokens", "identity");

            entity.HasIndex(e => e.UserId, "IX_user_refresh_tokens_UserId");

            entity.HasIndex(e => e.ExpiresAt, "ix_user_refresh_tokens_expires_at");

            entity.HasIndex(e => e.Token, "ix_user_refresh_tokens_token").IsUnique();

            entity.Property(e => e.CreatedByIp).HasMaxLength(45);
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.RevokedByIp).HasMaxLength(45);
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
