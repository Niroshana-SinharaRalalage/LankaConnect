using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;

namespace ExportEmailTemplates;

class Program
{
    // Phase 6A.112/113: Export email templates from Azure PostgreSQL staging database
    private const string StagingConnectionString = "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require";

    // 14 templates needing "View Signup Forms" button
    private static readonly string[] SignupFormsTemplates = new[]
    {
        "template-free-event-registration-confirmation",
        "template-paid-event-registration-confirmation-with-ticket",
        "template-event-reminder",
        "template-event-registration-cancellation",
        "template-attendees-added-confirmation",
        "template-event-cancellation-notifications",
        "template-new-event-publication",
        "template-newsletter-notification",
        "template-signup-list-commitment-confirmation",
        "template-signup-list-commitment-update",
        "template-signup-list-commitment-cancellation",
        "template-refund-requested",
        "template-refund-completed",
        "template-event-details-publication"
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("Phase 6A.112/113: Email Template Exporter\n");
        Console.WriteLine("==================================================\n");

        try
        {
            // Export templates for signup forms button
            await ExportSignupFormsTemplates();

            // Find templates with "feel free" text
            await FindFeelFreeTemplates();

            Console.WriteLine("\n✅ Export complete!");
            Console.WriteLine($"\nNext steps:");
            Console.WriteLine("1. Review exported JSON files");
            Console.WriteLine("2. Modify templates (add button, remove 'feel free')");
            Console.WriteLine("3. Create migration with updated templates");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    static async Task ExportSignupFormsTemplates()
    {
        Console.WriteLine("📋 Exporting 14 templates for 'View Signup Forms' button...\n");

        var templates = new List<object>();

        await using var conn = new NpgsqlConnection(StagingConnectionString);
        await conn.OpenAsync();

        foreach (var templateName in SignupFormsTemplates)
        {
            var sql = @"
                SELECT name, subject_template, html_template, text_template, description
                FROM communications.email_templates
                WHERE name = @name;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("name", templateName);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                templates.Add(new
                {
                    name = reader.GetString(0),
                    subject = reader.GetString(1),
                    html_body = reader.GetString(2),
                    text_body = reader.GetString(3),
                    description = reader.IsDBNull(4) ? "" : reader.GetString(4)
                });

                Console.WriteLine($"  ✅ {templateName}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠️  {templateName} - NOT FOUND");
                Console.ResetColor();
            }
        }

        // Save to JSON
        var outputPath = Path.Combine("..", "phase6a112_signup_forms_button_templates.json");
        var json = JsonConvert.SerializeObject(templates, Formatting.Indented);
        await File.WriteAllTextAsync(outputPath, json);

        Console.WriteLine($"\n✅ Exported {templates.Count} templates to: {Path.GetFullPath(outputPath)}");
    }

    static async Task FindFeelFreeTemplates()
    {
        Console.WriteLine("\n📋 Finding templates with 'feel free' text...\n");

        var templates = new List<object>();

        await using var conn = new NpgsqlConnection(StagingConnectionString);
        await conn.OpenAsync();

        var sql = @"
            SELECT name,
                   subject_template,
                   html_template,
                   text_template,
                   description,
                   CASE
                       WHEN html_template ILIKE '%feel free%' THEN 'html_template'
                       WHEN text_template ILIKE '%feel free%' THEN 'text_template'
                       ELSE 'none'
                   END as found_in
            FROM communications.email_templates
            WHERE html_template ILIKE '%feel free%' OR text_template ILIKE '%feel free%'
            ORDER BY name;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var templateName = reader.GetString(0);
            var foundIn = reader.GetString(5);

            templates.Add(new
            {
                name = templateName,
                subject = reader.GetString(1),
                html_body = reader.GetString(2),
                text_body = reader.GetString(3),
                description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                found_in = foundIn
            });

            Console.WriteLine($"  ✅ {templateName} (found in: {foundIn})");
        }

        if (templates.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✅ No templates found with 'feel free' text");
            Console.ResetColor();
        }
        else
        {
            // Save to JSON
            var outputPath = Path.Combine("..", "phase6a112_feel_free_templates.json");
            var json = JsonConvert.SerializeObject(templates, Formatting.Indented);
            await File.WriteAllTextAsync(outputPath, json);

            Console.WriteLine($"\n✅ Exported {templates.Count} templates to: {Path.GetFullPath(outputPath)}");
        }
    }
}
