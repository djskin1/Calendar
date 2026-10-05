using Calendar.Data;
using Calendar.Localization;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Interop;

namespace Calendar.Services
{
    public sealed class EntraGraphService
    {
        private static readonly HttpClient HttpClient =
            new();


        private static readonly JsonSerializerOptions
            JsonOptions =
                new()
                {
                    PropertyNameCaseInsensitive =
                        true
                };


        private readonly EntraConfiguration _configuration;
        private readonly IntPtr _parentWindowHandle;


        public EntraGraphService(
            EntraConfiguration configuration,
            IntPtr parentWindowHandle)
        {
            _configuration =
                configuration;

            _parentWindowHandle =
                parentWindowHandle;
        }


        // =================================================
        // CONNECTION TEST
        // =================================================

        public async Task<EntraUserProfile>
            TestConnectionAsync()
        {
            string accessToken =
                await GetAuthenticationAsync();


            const string url =
                "https://graph.microsoft.com/v1.0/me" +
                "?$select=id,displayName,userPrincipalName";


            return await GetAsync<EntraUserProfile>(
                url,
                accessToken);
        }


        // =================================================
        // GROUP SYNC
        // =================================================

        public async Task<int>
            SyncGroupsAsync()
        {
           string accessToken =
                await GetAuthenticationAsync();


            List<EntraGraphGroup> entraGroups =
                await LoadAllGroupsAsync(
                    accessToken);


            DateTime syncTime =
                DateTime.UtcNow;


            using CentralCalendarDbContext database =
                new();


            List<CalendarGroup> databaseGroups =
                await database.calendarGroups
                    .ToListAsync();


            HashSet<string> receivedObjectIds =
                new(
                    StringComparer.OrdinalIgnoreCase);


            foreach (EntraGraphGroup entraGroup
                     in entraGroups)
            {
                if (string.IsNullOrWhiteSpace(
                    entraGroup.Id))
                {
                    continue;
                }


                receivedObjectIds.Add(
                    entraGroup.Id);


                CalendarGroup? databaseGroup =
                    databaseGroups
                        .FirstOrDefault(group =>
                            group.EntraObjectId.Equals(
                                entraGroup.Id,
                                StringComparison.OrdinalIgnoreCase));


                bool isMicrosoft365Group =
                    entraGroup.GroupTypes.Any(
                        type =>
                            type.Equals(
                                "Unified",
                                StringComparison.OrdinalIgnoreCase));


                bool isSecurityGroup =
                    entraGroup.SecurityEnabled ==
                    true;


                if (databaseGroup == null)
                {
                    database.calendarGroups.Add(
                        new CalendarGroup
                        {
                            EntraObjectId =
                                entraGroup.Id,

                            DisplayName =
                                entraGroup.DisplayName ??
                                "",

                            Description =
                                entraGroup.Description,

                            IsSecurityGroup =
                                isSecurityGroup,

                            IsMicrosoft365Group =
                                isMicrosoft365Group,

                            // Nieuwe groep niet automatisch
                            // in kalender tonen.
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
                else
                {
                    databaseGroup.DisplayName =
                        entraGroup.DisplayName ??
                        databaseGroup.DisplayName;

                    databaseGroup.Description =
                        entraGroup.Description;

                    databaseGroup.IsSecurityGroup =
                        isSecurityGroup;

                    databaseGroup.IsMicrosoft365Group =
                        isMicrosoft365Group;

                    databaseGroup.IsActive =
                        true;

                    databaseGroup.LastSyncedAt =
                        syncTime;

                    databaseGroup.ModifiedAt =
                        syncTime;

                    // IsVisibleInCalendar NIET wijzigen.
                    // Dat is een lokale Central Calendar instelling.
                }
            }


            // Groepen die niet meer door Entra worden
            // teruggegeven niet verwijderen.
            // Alleen inactive zetten.
            foreach (CalendarGroup databaseGroup
                     in databaseGroups)
            {
                if (!receivedObjectIds.Contains(
                    databaseGroup.EntraObjectId))
                {
                    databaseGroup.IsActive =
                        false;

                    databaseGroup.ModifiedAt =
                        syncTime;
                }
            }


            EntraConfiguration? configuration =
                await database.entraConfigurations
                    .FirstOrDefaultAsync();


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


        // =================================================
        // GRAPH GROUP PAGINATION
        // =================================================

        private async Task<List<EntraGraphGroup>>
            LoadAllGroupsAsync(
                string accessToken)
        {
            List<EntraGraphGroup> result =
                new();


            string? nextUrl =
                "https://graph.microsoft.com/v1.0/groups" +
                "?$select=id,displayName,description," +
                "securityEnabled,groupTypes" +
                "&$top=999";


            while (!string.IsNullOrWhiteSpace(
                nextUrl))
            {
                EntraGraphCollection<EntraGraphGroup>
                    page =
                        await GetAsync<
                            EntraGraphCollection<
                                EntraGraphGroup>>(
                            nextUrl,
                            accessToken);


                if (page.Value != null)
                {
                    result.AddRange(
                        page.Value);
                }


                nextUrl =
                    page.NextLink;
            }


            return result;
        }


        // =================================================
        // AUTHENTICATION
        // =================================================

        private async Task<string>
            GetAuthenticationAsync()
        {
            return await EntraAuthenticationService.GetGraphAccessTokenAsync(
                    _parentWindowHandle);
        }


        // =================================================
        // GRAPH GET
        // =================================================

        private static async Task<T>
            GetAsync<T>(
                string url,
                string accessToken)
        {
            using HttpRequestMessage request =
                new(
                    HttpMethod.Get,
                    url);


            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);


            using HttpResponseMessage response =
                await HttpClient.SendAsync(
                    request);


            string json =
                await response.Content
                    .ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    json);
            }


            T? result =
                JsonSerializer.Deserialize<T>(
                    json,
                    JsonOptions);


            if (result == null)
            {
                throw new InvalidOperationException(
                    LocalizationService.Get(
                        "EntraInvalidGraphResponse"));
            }


            return result;
        }
    }


    // =====================================================
    // GRAPH MODELS
    // =====================================================

    public sealed class EntraUserProfile
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";


        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";


        [JsonPropertyName("userPrincipalName")]
        public string UserPrincipalName { get; set; } = "";
    }


    public sealed class EntraGraphGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";


        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }


        [JsonPropertyName("description")]
        public string? Description { get; set; }


        [JsonPropertyName("securityEnabled")]
        public bool? SecurityEnabled { get; set; }


        [JsonPropertyName("groupTypes")]
        public List<string> GroupTypes { get; set; } =
            new();
    }


    public sealed class EntraGraphCollection<T>
    {
        [JsonPropertyName("value")]
        public List<T> Value { get; set; } =
            new();


        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }
}