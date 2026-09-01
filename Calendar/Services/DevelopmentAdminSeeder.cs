using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Services
{
    public static class DevelopmentAdminSeeder
    {
        public static async Task EnsureTestAdminAsync()
        {
            const string username = "localadmin";
            const string displayName = "Local Administrator";
            const string password = "Test-Calendar-2026!";

            using CentralCalendarDbContext database =
                new CentralCalendarDbContext();

            bool adminAlreadyExists =
                await database.LocalAdministrators
                    .AnyAsync(admin =>
                        admin.Username == username);

            if (adminAlreadyExists)
            {
                return;
            }

            PasswordHashResult passwordData =
                PasswordSecurity.HashPassword(password);

            LocalAdministrator administrator =
                new LocalAdministrator
                {
                    Username = username,
                    DisplayName = displayName,

                    PasswordHash =
                        passwordData.Hash,

                    PasswordSalt =
                        passwordData.Salt,

                    PasswordIterations =
                        passwordData.Iterations,

                    IsEnabled = true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            database.LocalAdministrators.Add(
                administrator);

            await database.SaveChangesAsync();
        }
    }
}