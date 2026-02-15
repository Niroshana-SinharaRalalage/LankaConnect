#!/usr/bin/env python3
"""
Phase 6A.112/113: Create EF Core Migration for Email Template Updates

Generates a C# migration file that updates email templates in the database.
"""

import os
from pathlib import Path
from datetime import datetime

def escape_sql(text):
    """Escape single quotes for SQL string literals."""
    return text.replace("'", "''")

def generate_migration_cs():
    """Generate the C# migration file content."""
    # Get list of modified templates
    template_dir = Path("../Template_Correction/staging-phase6a113")

    if not template_dir.exists():
        print(f"[ERROR] Directory not found: {template_dir}")
        print("Run: python modify_templates_phase6a113.py first")
        return None

    modified_files = list(template_dir.glob("*-modified.html"))

    if not modified_files:
        print(f"[ERROR] No modified templates found in {template_dir}")
        return None

    print(f"Found {len(modified_files)} modified templates")
    print()

    # Extract template names
    template_updates = []
    for file_path in sorted(modified_files):
        template_name = file_path.stem.replace("-modified", "")
        print(f"  - {template_name}")
        template_updates.append((template_name, file_path.name))

    print()

    # Generate migration timestamp
    timestamp = datetime.now().strftime("%Y%m%d%H%M%S")

    migration_content = f'''using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{{
    /// <summary>
    /// Phase 6A.112/113: Update {len(template_updates)} email templates
    ///
    /// Changes:
    /// 1. Add "View Signup Forms" button to 14 event email templates
    /// 2. Remove "feel free" text from 8 templates
    ///
    /// Templates Updated:'''

    # Add list of templates
    for template_name, _ in template_updates:
        migration_content += f'\n    /// - {template_name}'

    migration_content += f'''
    /// </summary>
    public partial class Phase6A113_UpdateEmailTemplatesWithSignupFormsButton : Migration
    {{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {{
            // Get path to modified templates
            var projectRoot = FindProjectRoot();
            var templatePath = Path.Combine(projectRoot, "Template_Correction", "staging-phase6a113");

'''

    # Generate UPDATE statements for each template
    for template_name, file_name in template_updates:
        migration_content += f'''            // Update: {template_name}
            {{
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "{file_name}"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{{EscapeSql(htmlContent)}}',
                        updated_at = NOW()
                    WHERE name = '{template_name}';
                ");
            }}

'''

    migration_content += '''        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration not implemented - would need to restore from Phase 6A.96 templates
            migrationBuilder.Sql(@"
                -- Phase 6A.113 Down: Email template changes cannot be automatically reverted.
                -- To rollback, restore from database backup or re-run previous migration.
                SELECT 'Phase 6A.113 rollback: Email template changes cannot be automatically reverted.' as warning;
            ");
        }

        /// <summary>
        /// Finds the project root directory by searching for the .sln file.
        /// </summary>
        private string FindProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);

            // Search up the directory tree for LankaConnect.sln
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "LankaConnect.sln")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new InvalidOperationException(
                    "Could not find project root (LankaConnect.sln). " +
                    $"Current directory: {currentDir}");
            }

            return dir.FullName;
        }

        /// <summary>
        /// Escapes single quotes for SQL string literals.
        /// </summary>
        private string EscapeSql(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
'''

    return migration_content, timestamp

def main():
    print("=" * 80)
    print("Phase 6A.112/113: Migration Generator")
    print("=" * 80)
    print()

    migration_content, timestamp = generate_migration_cs()

    if not migration_content:
        return

    # Write migration file
    output_file = Path(f"../src/LankaConnect.Infrastructure/Data/Migrations/{timestamp}_Phase6A113_UpdateEmailTemplatesWithSignupFormsButton.cs")
    output_file.parent.mkdir(parents=True, exist_ok=True)

    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(migration_content)

    print(f"[OK] Migration created: {output_file.name}")
    print(f"[DIR] Location: {output_file.parent}")
    print()
    print("Next steps:")
    print("1. Review migration file")
    print("2. Run: dotnet ef database update")
    print("3. Test locally")
    print("4. Commit and push to staging")

if __name__ == "__main__":
    main()
