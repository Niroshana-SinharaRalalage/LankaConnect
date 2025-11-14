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
        try
        {
            // Check if admin user already exists (more specific than AnyAsync)
            // This allows seeding admin users even if test/old users exist in database
            System.Console.WriteLine("[UserSeeder] Checking if admin@lankaconnect.com already exists in database...");

            var adminExists = await context.Users
                .AnyAsync(u => u.Email.Value == "admin@lankaconnect.com");

            System.Console.WriteLine($"[UserSeeder] Admin existence check result: {adminExists}");

            // CRITICAL: Temporary override - force seeding every time to diagnose persistence bug
            // if (adminExists)
            // {
            //     System.Console.WriteLine("[UserSeeder] Admin user already exists, skipping seed");
            //     return; // Admin users already seeded
            // }

            System.Console.WriteLine("[UserSeeder] Admin user does not exist, proceeding with seeding");

            // Debug: Count total users in database
            var totalUsersInDb = await context.Users.CountAsync();
            System.Console.WriteLine($"[UserSeeder] Total users currently in database: {totalUsersInDb}");

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
                await context.Users.AddRangeAsync(users);

                System.Console.WriteLine("[UserSeeder] Calling SaveChangesAsync()");
                int savedCount = await context.SaveChangesAsync();
                System.Console.WriteLine($"[UserSeeder] SaveChangesAsync() completed, {savedCount} changes saved to database");
            }
            else
            {
                System.Console.WriteLine("[UserSeeder] No users to seed");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[UserSeeder] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            System.Console.WriteLine($"[UserSeeder] Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Error seeding users: {ex.Message}", ex);
        }
    }
}
