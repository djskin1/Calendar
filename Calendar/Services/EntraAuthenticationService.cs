using Calendar.Data;
using Calendar.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace Calendar.Services
{
    public static class EntraAuthenticationService
    {
        private static IPublicClientApplication? _application;

        private static string? _configuredClientId;
        private static string? _configuredTenantId;

        private static IntPtr _parentWindowHandle;


        private static readonly string[] GraphScopes =
        {
            "User.Read",
            "Group.Read.All"
        };


        // =====================================================
        // GRAPH ACCESS TOKEN
        // =====================================================

        public static async Task<string> GetGraphAccessTokenAsync(
            IntPtr parentWindowHandle)
        {
            using CentralCalendarDbContext database =
                new();


            var configuration =
                await database.entraConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.Id == 1);


            if (configuration == null ||
                !configuration.IsEnabled)
            {
                throw new InvalidOperationException(
                    LocalizationService.Get(
                        "EntraNotConfigured"));
            }


            if (string.IsNullOrWhiteSpace(
                    configuration.ClientId) ||
                string.IsNullOrWhiteSpace(
                    configuration.TenantId))
            {
                throw new InvalidOperationException(
                    LocalizationService.Get(
                        "EntraConfigurationIncomplete"));
            }


            _parentWindowHandle =
                parentWindowHandle;


            EnsureApplication(
                configuration.ClientId.Trim(),
                configuration.TenantId.Trim());


            IEnumerable<IAccount> accounts =
                await _application!
                    .GetAccountsAsync();


            IAccount? account =
                accounts.FirstOrDefault();


            AuthenticationResult result;


            try
            {
                // =============================================
                // 1. Eerst bestaand MSAL account proberen
                // =============================================

                if (account != null)
                {
                    result =
                        await _application
                            .AcquireTokenSilent(
                                GraphScopes,
                                account)
                            .ExecuteAsync();
                }
                else
                {
                    // =========================================
                    // 2. Geen cached account:
                    //    probeer huidige Windows account
                    //    via WAM / Windows SSO.
                    // =========================================

                    result =
                        await _application
                            .AcquireTokenSilent(
                                GraphScopes,
                                PublicClientApplication
                                    .OperatingSystemAccount)
                            .ExecuteAsync();
                }
            }
            catch (MsalUiRequiredException)
            {
                // =============================================
                // 3. Silent login lukt niet.
                //    Dan pas interactieve Microsoft login.
                // =============================================

                result =
                    await _application
                        .AcquireTokenInteractive(
                            GraphScopes)

                        .WithParentActivityOrWindow(
                            parentWindowHandle)

                        .WithPrompt(
                            Prompt.SelectAccount)

                        .ExecuteAsync();
            }


            return result.AccessToken;
        }


        // =====================================================
        // APPLICATION CONFIGURATION
        // =====================================================

        private static void EnsureApplication(
            string clientId,
            string tenantId)
        {
            if (_application != null &&
                string.Equals(
                    _configuredClientId,
                    clientId,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    _configuredTenantId,
                    tenantId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            BrokerOptions brokerOptions =
                new(
                    BrokerOptions
                        .OperatingSystems
                        .Windows)
                {
                    Title =
                        "Central Calendar"
                };


            _application =
                PublicClientApplicationBuilder
                    .Create(
                        clientId)

                    .WithAuthority(
                        AzureCloudInstance.AzurePublic,
                        tenantId)

                    .WithDefaultRedirectUri()

                    .WithParentActivityOrWindow(
                        () => _parentWindowHandle)

                    .WithBroker(
                        brokerOptions)

                    .Build();


            _configuredClientId =
                clientId;

            _configuredTenantId =
                tenantId;
        }
    }
}