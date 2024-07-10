using AuthorizationServer.Services.Interfaces;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthorizationServer.Services
{

    public class GrantTypeHandlerFactory : IGrantTypeHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GrantTypeHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IGrantTypeHandler GetHandler(string grantType)
        {
            return grantType switch
            {
                GrantTypes.Password => _serviceProvider.GetRequiredService<PasswordGrantTypeHandler>(),
                GrantTypes.RefreshToken => _serviceProvider.GetRequiredService<RefreshGrantTypeHandler>(),
                _ => throw new InvalidOperationException("Не поддерживаемый тип гранта.")
            };
        }
    }

}
