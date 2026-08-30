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

            modelBuilder.Entity<ApplicationBranding>()
                .HasData(
                    new ApplicationBranding
                    {
                        Id = 1,
                        CompanyName = "Central calendar"
                    }
                );

            modelBuilder.Entity<SystemInformation>()
                .HasData(
                    new SystemInformation
                     {
                        Id = 1,
                        LatestClientVersion = "2.0.0",
                        MinimumClientVersion = "2.0.0"
                     }
                );
        }
    }
}
