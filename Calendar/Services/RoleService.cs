using Calendar.Data;
using Calendar.Localization;
using Calendar.Models;
using Calendar.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Services
{
    public static class RoleService
    {
        public static async Task<List<Role>> GetRolesAsync()
        {
            using CentralCalendarDbContext database = new();

            return await database.Roles
                .AsNoTracking()
                .Where(role => role.IsActive)
                .OrderBy(role => role.Name)
                .ToListAsync();
        }


        public static async Task<List<Permission>> GetPermissionsAsync()
        {
            using CentralCalendarDbContext database = new();

            return await database.Permissions
                .AsNoTracking()
                .OrderBy(permission => permission.Id)
                .ToListAsync();
        }


        public static async Task<HashSet<int>>
            GetRolePermissionIdsAsync(int roleId)
        {
            using CentralCalendarDbContext database = new();

            List<int> permissionIds =
                await database.RolePermissions
                    .Where(item => item.RoleId == roleId)
                    .Select(item => item.PermissionId)
                    .ToListAsync();

            return permissionIds.ToHashSet();
        }


        public static async Task<Role> CreateRoleAsync(
            string name,
            string? description)
        {
            using CentralCalendarDbContext database = new();

            name = name.Trim();

            bool exists =
                await database.Roles
                    .AnyAsync(role => role.Name == name);

            if (exists)
            {
                throw new InvalidOperationException(
                    LocalizationService.Get("RoleAlreadyExists"));
            }


            Role role = new()
            {
                Code = $"custom.{Guid.NewGuid():N}",
                Name = name,
                Description = description?.Trim(),
                IsSystemRole = false,
                IsActive = true
            };


            database.Roles.Add(role);

            await database.SaveChangesAsync();

            return role;
        }


        public static async Task SaveRolePermissionsAsync(
            int roleId,
            IEnumerable<int> permissionIds)
        {
            using CentralCalendarDbContext database = new();

            Role? role =
                await database.Roles
                    .FirstOrDefaultAsync(
                        item => item.Id == roleId);

            if (role == null)
            {
                return;
            }

            // System roles houden wij beschermd.
            if (role.IsSystemRole)
            {
                throw new InvalidOperationException(
                    LocalizationService.Get(
                        "SystemRoleCannotBeModified"));
            }


            List<RolePermission> existing =
                await database.RolePermissions
                    .Where(item => item.RoleId == roleId)
                    .ToListAsync();


            database.RolePermissions.RemoveRange(existing);


            foreach (int permissionId
                     in permissionIds.Distinct())
            {
                database.RolePermissions.Add(
                    new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });
            }


            await database.SaveChangesAsync();
        }


        public static async Task<List<AdminUserRow>>
            GetUsersAsync()
        {
            using CentralCalendarDbContext database = new();

            var users =
                await database.Users
                    .AsNoTracking()
                    .Include(user => user.UserRoles)
                    .ThenInclude(item => item.Role)
                    .OrderBy(user => user.DisplayName)
                    .ToListAsync();


            return users
                .Select(user => new AdminUserRow
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    IsActive = user.IsActive,

                    Roles = user.UserRoles.Any()
                        ? string.Join(
                            ", ",
                            user.UserRoles
                                .Where(item =>
                                    item.Role != null)
                                .Select(item =>
                                    item.Role!.Name)
                                .OrderBy(name => name))
                        : "-"
                })
                .ToList();
        }


        public static async Task<List<AdminLocalAdministratorRow>>
            GetLocalAdministratorsAsync()
        {
            using CentralCalendarDbContext database = new();

            var administrators =
                await database.LocalAdministrators
                    .AsNoTracking()
                    .Include(admin =>
                        admin.LocalAdministratorRoles)
                    .ThenInclude(item =>
                        item.Role)
                    .OrderBy(admin =>
                        admin.DisplayName)
                    .ToListAsync();


            return administrators
                .Select(admin =>
                    new AdminLocalAdministratorRow
                    {
                        Id = admin.Id,
                        Username = admin.Username,
                        DisplayName = admin.DisplayName,
                        IsEnabled = admin.IsEnabled,
                        LastLoginAt = admin.LastLoginAt,

                        Roles =
                            admin.LocalAdministratorRoles.Any()
                                ? string.Join(
                                    ", ",
                                    admin.LocalAdministratorRoles
                                        .Where(item =>
                                            item.Role != null)
                                        .Select(item =>
                                            item.Role!.Name)
                                        .OrderBy(name => name))
                                : "-"
                    })
                .ToList();
        }


        public static async Task AssignRoleToUserAsync(
            int userId,
            int roleId)
        {
            using CentralCalendarDbContext database = new();

            bool exists =
                await database.UserRoles
                    .AnyAsync(item =>
                        item.UserId == userId &&
                        item.RoleId == roleId);

            if (exists)
            {
                return;
            }


            database.UserRoles.Add(
                new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                });


            await database.SaveChangesAsync();
        }


        public static async Task AssignRoleToAdministratorAsync(
            int administratorId,
            int roleId)
        {
            using CentralCalendarDbContext database = new();

            bool exists =
                await database.LocalAdministratorRoles
                    .AnyAsync(item =>
                        item.LocalAdministratorId ==
                            administratorId &&
                        item.RoleId == roleId);

            if (exists)
            {
                return;
            }


            database.LocalAdministratorRoles.Add(
                new LocalAdministratorRole
                {
                    LocalAdministratorId =
                        administratorId,

                    RoleId = roleId
                });


            await database.SaveChangesAsync();
        }
    }
}