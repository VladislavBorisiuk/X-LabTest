using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using X_LabDataBase.Context;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthorizationServer
{
    public class ClientSeeder
    {
        private readonly IServiceProvider _serviceProvider;

        public ClientSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task AddScopes()
        {
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            try
            {
                var apiScope = await manager.FindByNameAsync("api1");

                if (apiScope != null)
                {
                    await manager.DeleteAsync(apiScope);
                }

                await manager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    DisplayName = "API Scope",
                    Name = "api1",
                    Resources =
                    {
                        "resource_server_1"
                    }
                });
            }
            catch (Exception ex)
            {
                // Handle or log exceptions appropriately
                Console.WriteLine($"Error in AddScopes: {ex.Message}");
                throw;
            }
        }

        public async Task AddClients()
        {
            using var scope = _serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

            try
            {
                await context.Database.EnsureCreatedAsync();

                var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

                var client = await manager.FindByClientIdAsync("web-client");

                if (client != null)
                {
                    await manager.DeleteAsync(client);
                }

                await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "web-client",
                    ClientSecret = "901564A5-E7FE-42CB-B18D-61EF6A8F3654",
                    ConsentType = ConsentTypes.Explicit,
                    DisplayName = "Web Client Application",
                    RedirectUris =
                    {
                        new Uri("http://localhost:7169/swagger/oauth2-redirect.html")
                    },
                    PostLogoutRedirectUris =
                    {
                        new Uri("https://localhost:7169/resources")
                    },
                    Permissions =
                    {
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.Logout,
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.GrantTypes.Password,
                        Permissions.ResponseTypes.Code,
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Roles,
                        $"{Permissions.Prefixes.Scope}api1"
                    },
                });
            }
            catch (Exception ex)
            {
                // Handle or log exceptions appropriately
                Console.WriteLine($"Error in AddClients: {ex.Message}");
                throw;
            }
        }
    }
}
