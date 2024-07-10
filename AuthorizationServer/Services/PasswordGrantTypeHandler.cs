using AuthorizationServer.Infrastructure.Extensions;
using AuthorizationServer.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using X_LabDataBase.Entityes;
using static OpenIddict.Abstractions.OpenIddictConstants;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace AuthorizationServer.Services
{
    public class PasswordGrantTypeHandler : IGrantTypeHandler
    {
        private readonly UserManager<Person> _userManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly ILogger<PasswordGrantTypeHandler> _logger;

        public PasswordGrantTypeHandler(
            UserManager<Person> userManager,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictScopeManager scopeManager,
            ILogger<PasswordGrantTypeHandler> logger)
        {
            _userManager = userManager;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _scopeManager = scopeManager;
            _logger = logger;
        }

        public async Task<IActionResult> HandleAsync(OpenIddictRequest request)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(request.Username);
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                    throw new InvalidOperationException("Не удалось найти данные о вызывающем клиентском приложении.");

                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    return new ForbidResult(new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Неверная комбинация имени пользователя и пароля."
                    }));
                }

                var authorizations = await _authorizationManager.FindAsync(
                    subject: await _userManager.GetUserIdAsync(user),
                    client: await _applicationManager.GetIdAsync(application),
                    status: Statuses.Valid,
                    type: AuthorizationTypes.Permanent,
                    scopes: request.GetScopes()).ToListAsync();

                var identity = new ClaimsIdentity(
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                        .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                        .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                        .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user))
                        .SetClaims(Claims.Role, new List<string> { "user", "admin" }.ToImmutableArray());

                identity.SetScopes(request.GetScopes());
                identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

                var authorization = authorizations.LastOrDefault();
                authorization ??= await _authorizationManager.CreateAsync(
                    identity: identity,
                    subject: await _userManager.GetUserIdAsync(user),
                    client: await _applicationManager.GetIdAsync(application),
                    type: AuthorizationTypes.Permanent,
                    scopes: identity.GetScopes());

                identity.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
                identity.SetDestinations(AuthService.GetDestinations);

                var principal = new ClaimsPrincipal(identity);
                principal.SetScopes(request.GetScopes());
                principal.SetResources(await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

                return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время обработки предоставления токена по паролю.");
                return new BadRequestObjectResult(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.ServerError,
                    ErrorDescription = "Произошла ошибка во время обработки предоставления токена по паролю. Пожалуйста, попробуйте позже."
                });
            }
        }
    }
}
