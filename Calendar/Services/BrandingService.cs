using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Services
{
    public static class BrandingService
    {
        public static async Task<ApplicationBranding> GetAsync()
        {
            using CentralCalendarDbContext database = new();

            ApplicationBranding? branding =
                await database.ApplicationBranding
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == 1);

            if (branding != null)
            {
                return branding;
            }

            branding = new ApplicationBranding
            {
                Id = 1,
                CompanyName = "Central calendar",
                PrimaryColor = "#0B856D",
                AccentColor = "#0097A7",
                ModifiedAt = DateTime.UtcNow
            };

            database.ApplicationBranding.Add(branding);
            await database.SaveChangesAsync();

            return branding;
        }


        public static async Task SaveAsync(
            ApplicationBranding branding)
        {
            using CentralCalendarDbContext database = new();

            ApplicationBranding? existing =
                await database.ApplicationBranding
                    .FirstOrDefaultAsync(item => item.Id == 1);

            if (existing == null)
            {
                existing = new ApplicationBranding
                {
                    Id = 1
                };

                database.ApplicationBranding.Add(existing);
            }

            existing.CompanyName = branding.CompanyName;

            existing.LogoData = branding.LogoData;
            existing.LogoFileName = branding.LogoFileName;
            existing.LogoContentType = branding.LogoContentType;

            existing.PrimaryColor = branding.PrimaryColor;
            existing.AccentColor = branding.AccentColor;

            existing.ModifiedAt = DateTime.UtcNow;

            await database.SaveChangesAsync();
        }
    }
}