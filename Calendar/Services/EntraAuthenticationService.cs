using Calendar.Data;
using Calendar.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Calendar.Services
{
    public static class EntraAuthenticationService
    {
        private static IPublicClientApplication? _application;

        private static string? _configuredClientId;
        private static string? _configuredTenantId;


        public static async Task<string> GetGraphAccessTokenAsync()
        {
            using CentralCalendarDbContext database = new();

            var configuration =
                await database.entraConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == 1);

            if (configuration == null ||
                !configuration.IsEnabled)
            {
                throw new InvalidOperationException(
                    LocalizationService.Get("EntraNotConfigured"));
            }

            if (string.IsNullOrWhiteSpace(configuration.ClientId) ||
                string.IsNullOrWhiteSpace(configuration.TenantId))
            {
                throw new InvalidOperationException(
                    LocalizationService.Get("EntraConfigurationIncomplete"));
            }


            EnsureApplication(
                configuration.ClientId,
                configuration.TenantId);


            string[] scopes =
            {
                "User.Read",
                "Group.Read.All"
            };


            IEnumerable<IAccount> accounts =
                await _application!.GetAccountsAsync();

            IAccount? account =
                accounts.FirstOrDefault();


            AuthenticationResult result;

            try
            {
                result =
                    await _application
                        .AcquireTokenSilent(
                            scopes,
                            account)
                        .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result =
                    await _application
                        .AcquireTokenInteractive(scopes)
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
            }


            return result.AccessToken;
        }


        private static void EnsureApplication(
            string clientId,
            string tenantId)
        {
            if (_application != null &&
                _configuredClientId == clientId &&
                _configuredTenantId == tenantId)
            {
                return;
            }


            _application =
                PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(
                        AzureCloudInstance.AzurePublic,
                        tenantId)
                    .WithRedirectUri("http://localhost")
                    .Build();


            _configuredClientId = clientId;
            _configuredTenantId = tenantId;
        }
    }
}