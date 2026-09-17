using Calendar.Data;
using Calendar.Localization;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Calendar.Services
{
    public static class PublicHolidaySyncService
    {
        private static readonly HttpClient HttpClient = new();

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =====================================================
        // COUNTRY LIST
        // =====================================================

        public static async Task EnsureCountriesAsync()
        {
            Dictionary<string, string> countries =
                new(StringComparer.OrdinalIgnoreCase);


            foreach (CultureInfo culture in
                     CultureInfo.GetCultures(
                         CultureTypes.SpecificCultures))
            {
                try
                {
                    RegionInfo region =
                        new(culture.Name);

                    string code =
                        region.TwoLetterISORegionName
                            .ToUpperInvariant();

                    if (code.Length != 2)
                    {
                        continue;
                    }

                    if (!countries.ContainsKey(code))
                    {
                        countries.Add(
                            code,
                            region.EnglishName);
                    }
                }
                catch
                {
                    // Sommige systeemcultures hebben geen bruikbare RegionInfo.
                }
            }


            using CentralCalendarDbContext database = new();

            List<PublicHolidayCountry> existing =
                await database.PublicHolidayCountries
                    .ToListAsync();


            foreach (KeyValuePair<string, string> country
                     in countries.OrderBy(item => item.Value))
            {
                PublicHolidayCountry? databaseCountry =
                    existing.FirstOrDefault(item =>
                        item.CountryCode.Equals(
                            country.Key,
                            StringComparison.OrdinalIgnoreCase));


                if (databaseCountry == null)
                {
                    database.PublicHolidayCountries.Add(
                        new PublicHolidayCountry
                        {
                            CountryCode = country.Key,
                            CountryName = country.Value,
                            IsSelected = false,
                            IsBlockingCountry = false
                        });
                }
                else
                {
                    databaseCountry.CountryName =
                        country.Value;
                }
            }


            await database.SaveChangesAsync();
        }


        // =====================================================
        // SYNC ONE COUNTRY
        // =====================================================

        public static async Task<int> SyncCountryAsync(
            string countryCode,
            int year)
        {
            countryCode =
                countryCode.Trim().ToUpperInvariant();


            string url =
                $"https://nagerholidays.com/api/v4/Holidays/" +
                $"{countryCode}/{year}";


            using HttpResponseMessage response =
                await HttpClient.GetAsync(url);


            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"{LocalizationService.Get("HolidayCountryNotSupported")} " +
                    countryCode);
            }


            response.EnsureSuccessStatusCode();


            string json =
                await response.Content.ReadAsStringAsync();


            List<HolidayApiItem>? apiItems =
                JsonSerializer.Deserialize<List<HolidayApiItem>>(
                    json,
                    JsonOptions);


            if (apiItems == null)
            {
                return 0;
            }


            List<HolidayApiItem> holidays =
                apiItems
                    .Where(item =>
                        item.NationalHoliday &&
                        item.HolidayTypes.Any(type =>
                            type.Equals(
                                "Public",
                                StringComparison.OrdinalIgnoreCase)))
                    .ToList();


            using CentralCalendarDbContext database = new();


            DateTime yearStart =
                new(year, 1, 1);

            DateTime nextYear =
                yearStart.AddYears(1);


            // Oude API-data voor dit land/jaar verwijderen.
            List<PublicHoliday> oldApiItems =
                await database.PublicHolidays
                    .Where(item =>
                        item.CountryCode == countryCode &&
                        item.Source == "Api" &&
                        item.Date >= yearStart &&
                        item.Date < nextYear)
                    .ToListAsync();


            if (oldApiItems.Count > 0)
            {
                database.PublicHolidays.RemoveRange(
                    oldApiItems);

                await database.SaveChangesAsync();
            }


            DateTime syncTime =
                DateTime.UtcNow;


            foreach (HolidayApiItem holiday in holidays)
            {
                database.PublicHolidays.Add(
                    new PublicHoliday
                    {
                        Name = holiday.Name,
                        Date = holiday.Date.Date,
                        CountryCode = countryCode,

                        IsNationalHoliday =
                            holiday.NationalHoliday,

                        IsOfficialPublicHoliday =
                            true,

                        Source = "Api",
                        IsActive = true,
                        SyncedAt = syncTime
                    });
            }


            PublicHolidayCountry? country =
                await database.PublicHolidayCountries
                    .FirstOrDefaultAsync(item =>
                        item.CountryCode == countryCode);


            if (country != null)
            {
                country.LastSyncedYear = year;
                country.LastSyncedAt = syncTime;
            }


            await database.SaveChangesAsync();

            return holidays.Count;
        }


        // =====================================================
        // MANUAL SYNC
        // =====================================================

        public static async Task<int> SyncSelectedCountriesAsync(
            int year)
        {
            using CentralCalendarDbContext database = new();

            List<string> countries =
                await database.PublicHolidayCountries
                    .AsNoTracking()
                    .Where(country => country.IsSelected)
                    .Select(country => country.CountryCode)
                    .ToListAsync();


            int total = 0;


            foreach (string countryCode in countries)
            {
                total +=
                    await SyncCountryAsync(
                        countryCode,
                        year);
            }


            return total;
        }


        // =====================================================
        // AUTOMATIC: ONCE PER YEAR
        // =====================================================

        public static async Task EnsureCurrentYearSyncedAsync()
        {
            int year =
                DateTime.Today.Year;


            using CentralCalendarDbContext database = new();

            List<PublicHolidayCountry> countries =
                await database.PublicHolidayCountries
                    .AsNoTracking()
                    .Where(country => country.IsSelected)
                    .ToListAsync();


            foreach (PublicHolidayCountry country in countries)
            {
                if (country.LastSyncedYear == year)
                {
                    continue;
                }


                await SyncCountryAsync(
                    country.CountryCode,
                    year);
            }
        }
    }


    internal class HolidayApiItem
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; } = "";

        [JsonPropertyName("nationalHoliday")]
        public bool NationalHoliday { get; set; }

        [JsonPropertyName("holidayTypes")]
        public List<string> HolidayTypes { get; set; } = new();
    }
}