using AuthorizationServer.Infrastructure.Extensions;
using AuthorizationServer.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using X_LabDataBase.Entityes;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static System.Net.Mime.MediaTypeNames;

namespace AuthorizationServer.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly UserManager<Person> _userManager;
        private readonly SignInManager<Person> _signInManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;

        private readonly ILogger<AuthorizationController> _logger;

        public AuthorizationController(
            UserManager<Person> userManager,
            SignInManager<Person> signInManager,
            IOpenIddictScopeManager scopeManager,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            ILogger<AuthorizationController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _scopeManager = scopeManager;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _logger = logger;
        }

        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            try
            {
                var request = HttpContext.GetOpenIddictServerRequest() ??
                    throw new InvalidOperationException("Не удалось извлечь запрос OpenID Connect.");

                if (request.IsPasswordGrantType())
                {
                    return await HandlePasswordGrantTypeAsync();
                }
                else if (request.IsRefreshTokenGrantType())
                {
                    return await HandleRefreshTokenAsync();
                }

                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.UnsupportedGrantType,
                    ErrorDescription = "Указанный тип предоставления не поддерживается."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время обмена токенами.");
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.ServerError,
                    ErrorDescription = "Произошла ошибка во время обмена токенами. Пожалуйста, попробуйте позже."
                });
            }
        }

        public async Task<IActionResult> HandlePasswordGrantTypeAsync()
        {
            try
            {
                var request = HttpContext.GetOpenIddictServerRequest() ??
                    throw new InvalidOperationException("Не удалось извлечь запрос OpenID Connect.");

                if (!request.IsPasswordGrantType())
                {
                    return BadRequest(new OpenIddictResponse
                    {
                        Error = OpenIddictConstants.Errors.UnsupportedGrantType,
                        ErrorDescription = "Указанный тип предоставления не поддерживается."
                    });
                }

                var user = await _userManager.FindByNameAsync(request.Username);
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("Не удалось найти данные о вызывающем клиентском приложении.");

                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
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

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время обработки предоставления токена по паролю.");
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.ServerError,
                    ErrorDescription = "Произошла ошибка во время обработки предоставления токена по паролю. Пожалуйста, попробуйте позже."
                });
            }
        }

        private async Task<IActionResult> HandleRefreshTokenAsync()
        {
            try
            {
                var request = HttpContext.GetOpenIddictServerRequest() ??
                    throw new InvalidOperationException("Не удалось извлечь запрос OpenID Connect.");

                if (!request.IsRefreshTokenGrantType())
                {
                    return BadRequest(new OpenIddictResponse
                    {
                        Error = OpenIddictConstants.Errors.UnsupportedGrantType,
                        ErrorDescription = "Указанный тип предоставления не поддерживается."
                    });
                }

                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var claimsPrincipal = result.Principal;

                var user = await _userManager.FindByIdAsync(claimsPrincipal.GetClaim(Claims.Subject));

                if (user == null)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Токен больше не действителен."
                        }));
                }

                if (!await _signInManager.CanSignInAsync(user))
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Пользователю больше не разрешено входить."
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

                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время обработки обновления токена.");
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.ServerError,
                    ErrorDescription = "Произошла ошибка во время обработки обновления токена. Пожалуйста, попробуйте позже."
                });
            }
        }

    }
}
