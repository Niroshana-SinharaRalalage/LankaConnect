using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add tsvector column for full-text search
            migrationBuilder.Sql(@"
                ALTER TABLE events.events
                ADD COLUMN search_vector tsvector
                GENERATED ALWAYS AS (
                    setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
                    setweight(to_tsvector('english', coalesce(description, '')), 'B')
                ) STORED;
            ");

            // Create GIN index on search_vector for fast full-text search
            migrationBuilder.Sql(@"
                CREATE INDEX idx_events_search_vector
                ON events.events
                USING GIN(search_vector);
            ");

            // Update existing rows to populate search_vector (for GENERATED ALWAYS AS, this is automatic)
            // But we'll analyze the table to update statistics for the query planner
            migrationBuilder.Sql("ANALYZE events.events;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the GIN index
            migrationBuilder.Sql("DROP INDEX IF EXISTS events.idx_events_search_vector;");

            // Drop the search_vector column
            migrationBuilder.Sql("ALTER TABLE events.events DROP COLUMN IF EXISTS search_vector;");
        }
    }
}
