using AuthorizationServer.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Collections.Immutable;
using System.Security.Claims;
using AuthorizationServer.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using X_LabDataBase.Entityes;
using Microsoft.AspNetCore.Http;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace AuthorizationServer.Services
{
    public class RefreshGrantTypeHandler : IGrantTypeHandler
    {
        private readonly UserManager<Person> _userManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly ILogger<RefreshGrantTypeHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefreshGrantTypeHandler(
            UserManager<Person> userManager,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictScopeManager scopeManager,
            ILogger<RefreshGrantTypeHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _scopeManager = scopeManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> HandleAsync(OpenIddictRequest request)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var authenticateResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                if (!authenticateResult.Succeeded)
                {
                    return new ForbidResult(new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Недействительный токен обновления."
                    }));
                }

                var claimsPrincipal = authenticateResult.Principal;

                var user = await _userManager.FindByIdAsync(claimsPrincipal.GetClaim(Claims.Subject));
                if (user == null || await _userManager.IsLockedOutAsync(user))
                {
                    return new ForbidResult(new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Недействительный токен обновления."
                    }));
                }

                var identity = new ClaimsIdentity(claimsPrincipal.Claims,
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                        .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                        .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                        .SetClaims(Claims.Role, new List<string> { "user", "admin" }.ToImmutableArray());

                identity.SetScopes(request.GetScopes());
                identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

                var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                    throw new InvalidOperationException("Не удалось найти данные о вызывающем клиентском приложении.");

                var authorizations = await _authorizationManager.FindAsync(
                    subject: await _userManager.GetUserIdAsync(user),
                    client: await _applicationManager.GetIdAsync(application),
                    status: Statuses.Valid,
                    type: AuthorizationTypes.Permanent,
                    scopes: request.GetScopes()).ToListAsync();

                var authorization = authorizations.LastOrDefault();

                authorization ??= await _authorizationManager.CreateAsync(
                    identity: identity,
                    subject: await _userManager.GetUserIdAsync(user),
                    client: await _applicationManager.GetIdAsync(application),
                    type: AuthorizationTypes.Permanent,
                    scopes: identity.GetScopes());

                identity.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

                identity.SetDestinations(AuthService.GetDestinations);

                return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время обработки предоставления токена обновления.");
                return new BadRequestObjectResult(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.ServerError,
                    ErrorDescription = "Произошла ошибка во время обработки предоставления токена обновления. Пожалуйста, попробуйте позже."
                });
            }
        }
    }
}
