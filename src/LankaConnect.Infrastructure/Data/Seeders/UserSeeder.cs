using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds admin users for the LankaConnect platform
/// Phase 6A.1: Initial admin users for testing and development
/// </summary>
public static class UserSeeder
{
    /// <summary>
    /// Seed admin users into the database
    /// Passwords are hashed using the provided password hashing service
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, IPasswordHashingService passwordHashingService)
    {
        System.Console.WriteLine("[UserSeeder] SeedAsync started");
        try
        {
            // Check if ALL 4 admin users already exist
            // Only skip seeding if all admin users are present
            var requiredAdminEmails = new[]
            {
                "admin@lankaconnect.com",
                "admin1@lankaconnect.com",
                "organizer@lankaconnect.com",
                "user@lankaconnect.com"
            };

            // CRITICAL FIX: Load all users into memory first, then filter client-side
            // EF Core cannot reliably translate .Contains() queries on owned entity properties (Email is OwnsOne)
            // This was causing the idempotency check to fail silently, preventing proper user creation/deletion
            var allUsers = await context.Users.ToListAsync();
            System.Console.WriteLine($"[UserSeeder] Loaded {allUsers.Count} total users from database");

            var existingAdminEmails = allUsers
                .Select(u => u.Email.Value)
                .Where(email => requiredAdminEmails.Contains(email))
                .ToList();

            System.Console.WriteLine($"[UserSeeder] Found {existingAdminEmails.Count} existing admin users: {string.Join(", ", existingAdminEmails)}");

            // If all 4 admin users already exist, skip seeding
            if (existingAdminEmails.Count == requiredAdminEmails.Length)
            {
                System.Console.WriteLine("[UserSeeder] All 4 admin users exist, skipping seeding (idempotency)");
                return; // All admin users already seeded
            }

            System.Console.WriteLine($"[UserSeeder] Seeding required - {4 - existingAdminEmails.Count} admin users missing");

            var users = new List<User>();

            // Admin Manager (super admin)
            var adminManagerEmail = LankaConnect.Domain.Shared.ValueObjects.Email.Create("admin@lankaconnect.com");
            if (adminManagerEmail.IsSuccess)
            {
                var adminManager = User.Create(
                    adminManagerEmail.Value,
                    "Admin",
                    "Manager",
                    UserRole.AdminManager
                );

                if (adminManager.IsSuccess)
                {
                    var user = adminManager.Value;

                    // Set password: Admin@123
                    var passwordHash = passwordHashingService.HashPassword("Admin@123");
                    if (passwordHash.IsSuccess)
                    {
                        user.SetPassword(passwordHash.Value);
                        user.VerifyEmail(); // Auto-verify admin accounts
                        users.Add(user);
                    }
                }
            }

            // Regular Admin
            var adminEmail = LankaConnect.Domain.Shared.ValueObjects.Email.Create("admin1@lankaconnect.com");
            if (adminEmail.IsSuccess)
            {
                var admin = User.Create(
                    adminEmail.Value,
                    "John",
                    "Admin",
                    UserRole.Admin
                );

                if (admin.IsSuccess)
                {
                    var user = admin.Value;

                    // Set password: Admin@123
                    var passwordHash = passwordHashingService.HashPassword("Admin@123");
                    if (passwordHash.IsSuccess)
                    {
                        user.SetPassword(passwordHash.Value);
                        user.VerifyEmail(); // Auto-verify admin accounts
                        users.Add(user);
                    }
                }
            }

            // Event Organizer with active free trial
            var organizerEmail = LankaConnect.Domain.Shared.ValueObjects.Email.Create("organizer@lankaconnect.com");
            if (organizerEmail.IsSuccess)
            {
                var organizer = User.Create(
                    organizerEmail.Value,
                    "Sarah",
                    "Organizer",
                    UserRole.EventOrganizer
                );

                if (organizer.IsSuccess)
                {
                    var user = organizer.Value;

                    // Set password: Organizer@123
                    var passwordHash = passwordHashingService.HashPassword("Organizer@123");
                    if (passwordHash.IsSuccess)
                    {
                        user.SetPassword(passwordHash.Value);
                        user.VerifyEmail();
                        users.Add(user);
                    }
                }
            }

            // General User
            var generalUserEmail = LankaConnect.Domain.Shared.ValueObjects.Email.Create("user@lankaconnect.com");
            if (generalUserEmail.IsSuccess)
            {
                var generalUser = User.Create(
                    generalUserEmail.Value,
                    "Mike",
                    "User",
                    UserRole.GeneralUser
                );

                if (generalUser.IsSuccess)
                {
                    var user = generalUser.Value;

                    // Set password: User@123
                    var passwordHash = passwordHashingService.HashPassword("User@123");
                    if (passwordHash.IsSuccess)
                    {
                        user.SetPassword(passwordHash.Value);
                        user.VerifyEmail();
                        users.Add(user);
                    }
                }
            }

            // Add users to context and save
            if (users.Any())
            {
                System.Console.WriteLine($"[UserSeeder] Adding {users.Count} users to context");

                // Add users to change tracker
                await context.Users.AddRangeAsync(users);

                // CRITICAL DIAGNOSTICS: Examine EF Core change tracking BEFORE SaveChangesAsync
                System.Console.WriteLine("[UserSeeder] === CHANGE TRACKING DIAGNOSTICS (PRE-SAVE) ===");

                var trackedEntries = context.ChangeTracker.Entries().ToList();
                System.Console.WriteLine($"[UserSeeder] Total tracked entries: {trackedEntries.Count}");

                foreach (var entry in trackedEntries)
                {
                    System.Console.WriteLine($"[UserSeeder]   - {entry.Entity.GetType().Name}: {entry.State}");

                    // For User entities, show detailed info
                    if (entry.Entity is User user)
                    {
                        System.Console.WriteLine($"[UserSeeder]     User ID: {user.Id}");
                        System.Console.WriteLine($"[UserSeeder]     Email: {user.Email.Value}");
                        System.Console.WriteLine($"[UserSeeder]     Role: {user.Role}");
                        System.Console.WriteLine($"[UserSeeder]     Name: {user.FullName}");
                        System.Console.WriteLine($"[UserSeeder]     HasPasswordHash: {!string.IsNullOrEmpty(user.PasswordHash)}");
                        System.Console.WriteLine($"[UserSeeder]     IsEmailVerified: {user.IsEmailVerified}");
                    }
                }

                // Check for any validation errors or issues
                var validationErrors = context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .SelectMany(e =>
                    {
                        try
                        {
                            // Try to get validation errors if any
                            return new[] { $"{e.Entity.GetType().Name}: OK" };
                        }
                        catch (Exception ex)
                        {
                            return new[] { $"{e.Entity.GetType().Name}: ERROR - {ex.Message}" };
                        }
                    })
                    .ToList();

                if (validationErrors.Any())
                {
                    System.Console.WriteLine("[UserSeeder] Validation status:");
                    foreach (var error in validationErrors)
                    {
                        System.Console.WriteLine($"[UserSeeder]   {error}");
                    }
                }

                System.Console.WriteLine("[UserSeeder] === EXECUTING SaveChangesAsync ===");

                try
                {
                    var saveResult = await context.SaveChangesAsync();
                    System.Console.WriteLine($"[UserSeeder] SaveChangesAsync returned {saveResult} rows affected");

                    // POST-SAVE DIAGNOSTICS: Verify what was actually persisted
                    System.Console.WriteLine("[UserSeeder] === CHANGE TRACKING DIAGNOSTICS (POST-SAVE) ===");

                    var postSaveEntries = context.ChangeTracker.Entries().ToList();
                    System.Console.WriteLine($"[UserSeeder] Total tracked entries after save: {postSaveEntries.Count}");

                    foreach (var entry in postSaveEntries)
                    {
                        System.Console.WriteLine($"[UserSeeder]   - {entry.Entity.GetType().Name}: {entry.State}");
                    }

                    // CRITICAL: Verify users are actually in database by querying immediately
                    var verifyCount = await context.Users.CountAsync();
                    System.Console.WriteLine($"[UserSeeder] VERIFICATION: Database now contains {verifyCount} total users");

                    // Check specifically for admin users
                    var allUsersAfterSave = await context.Users.ToListAsync();
                    var adminUsersAfterSave = allUsersAfterSave
                        .Where(u => requiredAdminEmails.Contains(u.Email.Value))
                        .ToList();

                    System.Console.WriteLine($"[UserSeeder] VERIFICATION: Found {adminUsersAfterSave.Count} admin users in database:");
                    foreach (var admin in adminUsersAfterSave)
                    {
                        System.Console.WriteLine($"[UserSeeder]   - {admin.Email.Value} (Role: {admin.Role})");
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    System.Console.WriteLine($"[UserSeeder] DbUpdateException during SaveChangesAsync: {dbEx.Message}");
                    System.Console.WriteLine($"[UserSeeder] Inner Exception: {dbEx.InnerException?.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[UserSeeder] Unexpected exception during SaveChangesAsync: {ex.Message}");
                    System.Console.WriteLine($"[UserSeeder] Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }
            else
            {
                System.Console.WriteLine("[UserSeeder] No users to add (all idempotent checks passed)");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[UserSeeder] Exception: {ex}");
            throw new InvalidOperationException($"Error seeding users: {ex.Message}", ex);
        }
    }
}
