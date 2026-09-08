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

            using CentralCalendarDbContext database = new();

            LocalAdministrator? administrator =
                await database.LocalAdministrators
                    .FirstOrDefaultAsync(
                        admin => admin.Username == username);

            if (administrator == null)
            {
                PasswordHashResult passwordData =
                    PasswordSecurity.HashPassword(password);

                administrator = new LocalAdministrator
                {
                    Username = username,
                    DisplayName = displayName,
                    PasswordHash = passwordData.Hash,
                    PasswordSalt = passwordData.Salt,
                    PasswordIterations = passwordData.Iterations,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };

                database.LocalAdministrators.Add(administrator);

                await database.SaveChangesAsync();
            }


            Role? administratorRole =
                await database.Roles
                    .FirstOrDefaultAsync(
                        role => role.Code == "administrator");


            if (administratorRole != null)
            {
                bool roleExists =
                    await database.LocalAdministratorRoles
                        .AnyAsync(item =>
                            item.LocalAdministratorId == administrator.Id &&
                            item.RoleId == administratorRole.Id);

                if (!roleExists)
                {
                    database.LocalAdministratorRoles.Add(
                        new LocalAdministratorRole
                        {
                            LocalAdministratorId = administrator.Id,
                            RoleId = administratorRole.Id
                        });

                    await database.SaveChangesAsync();
                }
            }
        }
    }
}