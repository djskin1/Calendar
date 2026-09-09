using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Data
{
    public class CentralCalendarDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<CalendarEntry> CalendarEntries =>
            Set<CalendarEntry>();

        public DbSet<LocalAdministrator> LocalAdministrators =>
            Set<LocalAdministrator>();

        public DbSet<PublicHoliday> PublicHolidays =>
            Set<PublicHoliday>();

        public DbSet<CompanyEvent> CompanyEvents =>
            Set<CompanyEvent>();

        public DbSet<ApplicationBranding> ApplicationBranding =>
            Set<ApplicationBranding>();

        public DbSet<SystemInformation> SystemInformation =>
            Set<SystemInformation>();

        public DbSet<Role> Roles => Set<Role>();

        public DbSet<Permission> Permissions => Set<Permission>();

        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        public DbSet<UserRole> UserRoles => Set<UserRole>();

        public DbSet<LocalAdministratorRole> LocalAdministratorRoles => Set<LocalAdministratorRole>();

        public DbSet<CalendarGroup> calendarGroups => Set<CalendarGroup>();

        public DbSet<EntraConfiguration> entraConfigurations => Set<EntraConfiguration>();

        public DbSet<CalendarStatus> CalendarStatuses => Set<CalendarStatus>();

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Server=.\SQLEXPRESS;
                  Database=CentralCalendar;
                  Trusted_Connection=True;
                  TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            // A Microsoft Entra Object ID may only occur once.
            modelBuilder.Entity<User>()
                .HasIndex(user => user.EntraObjectId)
                .IsUnique()
                .HasFilter("[EntraObjectId] IS NOT NULL");

            // Connect calendar entries to users.
            modelBuilder.Entity<CalendarEntry>()
                .HasOne(entry => entry.User)
                .WithMany()
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LocalAdministrator>()
                .HasIndex(admin => admin.Username)
                .IsUnique();

            DateTime seedDate =
                new DateTime(
                    2026,1,1, 
                    0, 0, 0,
                    DateTimeKind.Utc);

            modelBuilder.Entity<ApplicationBranding>()
                .HasData(
                    new ApplicationBranding
                    {
                        Id = 1,
                        CompanyName = "Central calendar",
                        PrimaryColor = "#0B856D",
                        AccentColor = "#0097A7",
                        ModifiedAt = new DateTime (2026,1,1)
                    }
                );

            modelBuilder.Entity<SystemInformation>()
                .HasData(
                    new SystemInformation
                     {
                        Id = 1,
                        LatestClientVersion = "2.0.0",
                        MinimumClientVersion = "2.0.0",
                        ModifiedAt = seedDate
                    }
                );

            modelBuilder.Entity<Role>()
                .HasIndex(role => role.Code)
                .IsUnique();

            modelBuilder.Entity<Permission>()
                .HasIndex(permission => permission.Code)
                .IsUnique();


            modelBuilder.Entity<RolePermission>()
                .HasKey(item => new
                {
                    item.RoleId,
                    item.PermissionId
                });

            modelBuilder.Entity<RolePermission>()
                .HasOne(item => item.Role)
                .WithMany(role => role.RolePermissions)
                .HasForeignKey(item => item.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(item => item.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(item => item.PermissionId);


            modelBuilder.Entity<UserRole>()
                .HasKey(item => new
                {
                    item.UserId,
                    item.RoleId
                });

            modelBuilder.Entity<UserRole>()
                .HasOne(item => item.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(item => item.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(item => item.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(item => item.RoleId);


            modelBuilder.Entity<LocalAdministratorRole>()
                .HasKey(item => new
                {
                    item.LocalAdministratorId,
                    item.RoleId
                });

            modelBuilder.Entity<LocalAdministratorRole>()
                .HasOne(item => item.LocalAdministrator)
                .WithMany(admin => admin.LocalAdministratorRoles)
                .HasForeignKey(item => item.LocalAdministratorId);

            modelBuilder.Entity<LocalAdministratorRole>()
                .HasOne(item => item.Role)
                .WithMany(role => role.LocalAdministratorRoles)
                .HasForeignKey(item => item.RoleId);

            modelBuilder.Entity<Role>().HasData(
                new Role
            {
                Id = 1,
                Code = "administrator",
                Name = "Administrator",
                Description = "Full access to Central Calendar.",
                IsSystemRole = true,
                 IsActive = true
            },
                new Role
            {
                Id = 2,
                Code = "user",
                Name = "User",
                Description = "Standard Central Calendar user.",
                IsSystemRole = true,
                IsActive = true
            }
            );

            modelBuilder.Entity<Permission>().HasData(

                new Permission
            {
                Id = 1,
                Code = "calendar.view",
                NameResourceKey = "PermissionViewCalendar"
            },

                new Permission
            {
                Id = 2,
                Code = "calendar.edit.own",
                NameResourceKey = "PermissionEditOwnCalendar"
            },

                new Permission
            {
                Id = 3,
                Code = "calendar.edit.all",
                NameResourceKey = "PermissionEditAllCalendar"
            },

                new Permission
            {
                Id = 4,
                Code = "admin.branding",
                NameResourceKey = "PermissionManageBranding"
            },

                new Permission
            {
                Id = 5,
                Code = "admin.users",
                NameResourceKey = "PermissionManageUsers"
            },

                new Permission
            {
                Id = 6,
                Code = "admin.administrators",
                NameResourceKey = "PermissionManageAdministrators"
            },

                new Permission
            {
                Id = 7,
                Code = "admin.roles",
                NameResourceKey = "PermissionManageRoles"
            },

                new Permission
            {
                Id = 8,
                Code = "admin.groups",
                NameResourceKey = "PermissionManageGroups"
            },

                new Permission
            {
                Id = 9,
                Code = "admin.statuses",
                NameResourceKey = "PermissionManageStatuses"
            },

                new Permission
            {
                Id = 10,
                Code = "admin.holidays",
                NameResourceKey = "PermissionManageHolidays"
            },

                new Permission
            {
                Id = 11,
                Code = "admin.events",
                NameResourceKey = "PermissionManageCompanyEvents"
            },

                new Permission
            {
                Id = 12,
                Code = "admin.entra",
                NameResourceKey = "PermissionManageEntra"
            },

                new Permission
            {
                Id = 13,
                Code = "admin.updates",
                NameResourceKey = "PermissionManageUpdates"
            }
            );

            modelBuilder.Entity<RolePermission>().HasData(
                new RolePermission { RoleId = 1, PermissionId = 1 },
                new RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 1, PermissionId = 5 },
                new RolePermission { RoleId = 1, PermissionId = 6 },
                new RolePermission { RoleId = 1, PermissionId = 7 },
                new RolePermission { RoleId = 1, PermissionId = 8 },
                new RolePermission { RoleId = 1, PermissionId = 9 },
                new RolePermission { RoleId = 1, PermissionId = 10 },
                new RolePermission { RoleId = 1, PermissionId = 11 },
                new RolePermission { RoleId = 1, PermissionId = 12 },
                new RolePermission { RoleId = 1, PermissionId = 13 },

                // Normal user
                new RolePermission { RoleId = 2, PermissionId = 1 },
                new RolePermission { RoleId = 2, PermissionId = 2 }
            );

            modelBuilder.Entity<CalendarGroup>()
                .HasIndex(group => group.EntraObjectId)
                .IsUnique();

            modelBuilder.Entity<EntraConfiguration>()
                .HasData(
                    new EntraConfiguration
                    {
                        Id = 1,
                        IsEnabled = false,
                        TenantId = "",
                        ClientId = "",
                        ModifiedAt = new DateTime(2026, 1, 1)
                    }
                );

            modelBuilder.Entity<CalendarStatus>()
                .HasIndex(status => status.Code)
                .IsUnique();

            DateTime statusSeedDate =
                new DateTime(2026, 1, 1);

            modelBuilder.Entity<CalendarStatus>().HasData(

                new CalendarStatus
                {
                    Id = 1,
                    Code = "OFFI",
                    DisplayName = "Office",
                    Description = "Working at the office.",
                    BackgroundColor = "#D1FAE5",
                    ForegroundColor = "#065F46",
                    IsActive = true,
                    IsSelectable = true,
                    IsSystemStatus = false,
                    SortOrder = 10,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 2,
                    Code = "HOME",
                    DisplayName = "Home",
                    Description = "Working from home.",
                    BackgroundColor = "#DBEAFE",
                    ForegroundColor = "#1E40AF",
                    IsActive = true,
                    IsSelectable = true,
                    IsSystemStatus = false,
                    SortOrder = 20,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 3,
                    Code = "HO/AB",
                    DisplayName = "Home / Absent",
                    BackgroundColor = "#EDE9FE",
                    ForegroundColor = "#5B21B6",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 30,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 4,
                    Code = "OF/AB",
                    DisplayName = "Office / Absent",
                    BackgroundColor = "#FEE2E2",
                    ForegroundColor = "#991B1B",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 40,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 5,
                    Code = "HO/OF",
                    DisplayName = "Home / Office",
                    BackgroundColor = "#E0F2FE",
                    ForegroundColor = "#075985",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 50,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 6,
                    Code = "CONG",
                    DisplayName = "Congress",
                    BackgroundColor = "#FEF3C7",
                    ForegroundColor = "#92400E",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 60,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 7,
                    Code = "MEET",
                    DisplayName = "Meeting",
                    BackgroundColor = "#CFFAFE",
                    ForegroundColor = "#155E75",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 70,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 8,
                    Code = "MV",
                    DisplayName = "Leave",
                    BackgroundColor = "#FCE7F3",
                    ForegroundColor = "#9D174D",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 80,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 9,
                    Code = "OPT",
                    DisplayName = "Optional",
                    BackgroundColor = "#F3F4F6",
                    ForegroundColor = "#374151",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 90,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 10,
                    Code = "ABS",
                    DisplayName = "Absent",
                    BackgroundColor = "#FECACA",
                    ForegroundColor = "#7F1D1D",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 100,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 11,
                    Code = "Weekend",
                    DisplayName = "Weekend",
                    BackgroundColor = "#D1D5DB",
                    ForegroundColor = "#4B5563",
                    IsActive = true,
                    IsSelectable = false,
                    IsSystemStatus = true,
                    SortOrder = 110,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 12,
                    Code = "PUB",
                    DisplayName = "Public holiday",
                    BackgroundColor = "#FDE68A",
                    ForegroundColor = "#78350F",
                    IsActive = true,
                    IsSelectable = false,
                    IsSystemStatus = true,
                    SortOrder = 120,
                    CreatedAt = statusSeedDate
                },

                new CalendarStatus
                {
                    Id = 13,
                    Code = "MF",
                    DisplayName = "Medical / Family",
                    BackgroundColor = "#DDD6FE",
                    ForegroundColor = "#4C1D95",
                    IsActive = true,
                    IsSelectable = true,
                    SortOrder = 130,
                    CreatedAt = statusSeedDate
                }
            );

        }
    }
}
