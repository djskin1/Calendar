using Calendar.Data;
using Calendar.Models;
using Calendar.Services.GraphModels;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Calendar.Services
{
    public static class EntraGroupSyncService
    {
        public static async Task<int> SyncAsync()
        {
            string accessToken =
                await EntraAuthenticationService
                    .GetGraphAccessTokenAsync();


            List<GraphGroup> entraGroups =
                await GetAllGroupsAsync(accessToken);


            DateTime syncTime =
                DateTime.UtcNow;


            using CentralCalendarDbContext database = new();


            List<CalendarGroup> existingGroups =
                await database.calendarGroups
                    .ToListAsync();


            Dictionary<string, CalendarGroup> existingByEntraId =
                existingGroups.ToDictionary(
                    group => group.EntraObjectId,
                    StringComparer.OrdinalIgnoreCase);


            HashSet<string> receivedIds =
                new(StringComparer.OrdinalIgnoreCase);


            foreach (GraphGroup entraGroup in entraGroups)
            {
                if (string.IsNullOrWhiteSpace(entraGroup.Id))
                {
                    continue;
                }


                receivedIds.Add(entraGroup.Id);


                bool isMicrosoft365Group =
                    entraGroup.GroupTypes.Any(
                        type =>
                            string.Equals(
                                type,
                                "Unified",
                                StringComparison.OrdinalIgnoreCase));


                if (existingByEntraId.TryGetValue(
                    entraGroup.Id,
                    out CalendarGroup? existing))
                {
                    existing.DisplayName =
                        entraGroup.DisplayName;

                    existing.Description =
                        entraGroup.Description;

                    existing.IsSecurityGroup =
                        entraGroup.SecurityEnabled ?? false;

                    existing.IsMicrosoft365Group =
                        isMicrosoft365Group;

                    existing.IsActive = true;

                    existing.LastSyncedAt =
                        syncTime;

                    existing.ModifiedAt =
                        syncTime;
                }
                else
                {
                    database.calendarGroups.Add(
                        new CalendarGroup
                        {
                            EntraObjectId =
                                entraGroup.Id,

                            DisplayName =
                                entraGroup.DisplayName,

                            Description =
                                entraGroup.Description,

                            IsSecurityGroup =
                                entraGroup.SecurityEnabled ?? false,

                            IsMicrosoft365Group =
                                isMicrosoft365Group,

                            // Nieuwe groepen verschijnen niet
                            // automatisch in de kalender.
                            IsVisibleInCalendar =
                                false,

                            IsActive =
                                true,

                            LastSyncedAt =
                                syncTime,

                            CreatedAt =
                                syncTime
                        });
                }
            }


            // Niet verwijderen:
            // zo behouden we historie wanneer een Entra-groep
            // verwijderd wordt.
            foreach (CalendarGroup existing in existingGroups)
            {
                if (!receivedIds.Contains(
                    existing.EntraObjectId))
                {
                    existing.IsActive = false;
                    existing.ModifiedAt = syncTime;
                }
            }


            EntraConfiguration? configuration =
                await database.entraConfigurations
                    .FirstOrDefaultAsync(
                        item => item.Id == 1);


            if (configuration != null)
            {
                configuration.LastGroupSyncAt =
                    syncTime;

                configuration.ModifiedAt =
                    syncTime;
            }


            await database.SaveChangesAsync();

            return entraGroups.Count;
        }


        private static async Task<List<GraphGroup>>
            GetAllGroupsAsync(
                string accessToken)
        {
            List<GraphGroup> result = new();


            using HttpClient client = new();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);


            string? url =
                "https://graph.microsoft.com/v1.0/groups" +
                "?$select=id,displayName,description," +
                "securityEnabled,groupTypes" +
                "&$top=999";


            while (!string.IsNullOrWhiteSpace(url))
            {
                using HttpResponseMessage response =
                    await client.GetAsync(url);


                string json =
                    await response.Content
                        .ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Microsoft Graph returned " +
                        $"{(int)response.StatusCode}: {json}");
                }


                GraphGroupResponse? page =
                    JsonSerializer.Deserialize<GraphGroupResponse>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });


                if (page == null)
                {
                    break;
                }


                result.AddRange(page.Value);

                url = page.NextLink;
            }


            return result;
        }
    }
}