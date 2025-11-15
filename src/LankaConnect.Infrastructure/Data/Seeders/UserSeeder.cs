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
            var existingAdminEmails = allUsers
                .Select(u => u.Email.Value)
                .Where(email => requiredAdminEmails.Contains(email))
                .ToList();

            // If all 4 admin users already exist, skip seeding
            if (existingAdminEmails.Count == requiredAdminEmails.Length)
            {
                return; // All admin users already seeded
            }

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
                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error seeding users: {ex.Message}", ex);
        }
    }
}
