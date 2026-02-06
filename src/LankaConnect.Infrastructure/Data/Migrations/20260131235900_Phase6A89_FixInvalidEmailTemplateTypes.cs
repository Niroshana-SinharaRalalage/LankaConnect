using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations;

/// <summary>
/// Phase 6A.89: Fix invalid EmailType values in email_templates table
///
/// Root Cause: Migration 20260129100000_Phase6A92_AddRefundEmailTemplates inserted
/// templates with type='Registration', but 'Registration' is not a valid EmailType enum value.
///
/// Fix: Update the invalid 'Registration' values to 'Transactional' which is a valid EmailType.
/// </summary>
public partial class Phase6A89_FixInvalidEmailTemplateTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Fix invalid EmailType values
        // 'Registration' is not a valid EmailType enum value
        // Valid values: Welcome, EmailVerification, PasswordReset, BusinessNotification,
        // EventNotification, Newsletter, Marketing, Transactional, CulturalEventNotification,
        // MemberEmailVerification, SignupCommitmentConfirmation, RegistrationCancellationConfirmation,
        // OrganizerCustomMessage, EventReminder
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET ""type"" = 'Transactional'
            WHERE ""type"" = 'Registration';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert to original (invalid) value - not recommended but included for completeness
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET ""type"" = 'Registration'
            WHERE ""name"" IN ('template-refund-requested', 'template-refund-completed');
        ");
    }
}
