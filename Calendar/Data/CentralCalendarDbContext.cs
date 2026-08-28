using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Data
{
    public class CentralCalendarDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<CalendarEntry> CalendarEntries =>
            Set<CalendarEntry>();

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
        }
    }
}
