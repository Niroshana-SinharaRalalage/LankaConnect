using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.133: Add FK constraint and partial index on linked_user_id
    /// for multi-organizer co-organizer lookup support.
    /// </summary>
    public partial class AddLinkedUserIdForeignKeyAndIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add FK constraint: linked_user_id → identity.users.Id
            // ON DELETE SET NULL: if a user is deleted, their co-organizer link is cleared
            // but the contact info remains
            migrationBuilder.Sql(@"
                ALTER TABLE events.event_organizer_contacts
                ADD CONSTRAINT ""FK_event_organizer_contacts_linked_user_id""
                FOREIGN KEY (linked_user_id) REFERENCES identity.users(""Id"")
                ON DELETE SET NULL;
            ");

            // Partial index for efficient co-organizer lookups:
            // "find all events where user X is a co-organizer"
            // Only indexes rows where linked_user_id IS NOT NULL (most are null)
            migrationBuilder.Sql(@"
                CREATE INDEX ix_event_organizer_contacts_linked_user_id
                ON events.event_organizer_contacts (linked_user_id)
                WHERE linked_user_id IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS events.ix_event_organizer_contacts_linked_user_id;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE events.event_organizer_contacts
                DROP CONSTRAINT IF EXISTS ""FK_event_organizer_contacts_linked_user_id"";
            ");
        }
    }
}
