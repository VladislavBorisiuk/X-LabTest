using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using X_LabDataBase.Entityes;
using AuthorizationServer.Infrastructure.Extensions;
using AuthorizationServer.Services;
using AuthorizationServer.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Immutable;
using static System.Net.Mime.MediaTypeNames;
using System.Net;

namespace AuthorizationServer.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly UserManager<Person> _userManager;
        private readonly SignInManager<Person> _signInManager;
        private AuthService _authService;

        public AuthorizationController(
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictScopeManager scopeManager,
            UserManager<Person> userManager,
            AuthService authService,
            SignInManager<Person> signInManager)
        {
            _authorizationManager = authorizationManager;
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
            _userManager = userManager;
            _authService = authService;
            _signInManager = signInManager;
        }

        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]

        public async Task<IActionResult> Authorize()
{
    var request = HttpContext.GetOpenIddictServerRequest() ??
        throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    var IsAuthentificated = _authService.IsAuthenticated(result, request);

    var parameters = _authService.ParceOAuthParameters(HttpContext);

    if (!IsAuthentificated)
    {
        return Challenge(
            authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = _authService.BuildRedirectUri(HttpContext, parameters),
            });
    }

    var user = await _userManager.GetUserAsync(result.Principal) ??
        throw new InvalidOperationException("The user details cannot be retrieved.");

    var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
        throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

    var authorizations = await _authorizationManager.FindAsync(
        subject: await _userManager.GetUserIdAsync(user),
        client: await _applicationManager.GetIdAsync(application),
        status: Statuses.Valid,
        type: AuthorizationTypes.Permanent,
        scopes: request.GetScopes()).ToListAsync();

    switch (await _applicationManager.GetConsentTypeAsync(application))
    {
        case ConsentTypes.External when authorizations.Count is 0:
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The logged in user is not allowed to access this client application."
                }));

        case ConsentTypes.Implicit:
        case ConsentTypes.External when authorizations.Count is not 0:
        case ConsentTypes.Explicit when authorizations.Count is not 0 && !request.HasPrompt(Prompts.Consent):
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                    .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                    .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                    .SetClaims(Claims.Role, new List<string> { "user","admin"}.ToImmutableArray());

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

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        case ConsentTypes.Explicit when request.HasPrompt(Prompts.None):
        case ConsentTypes.Systematic when request.HasPrompt(Prompts.None):
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Interactive user consent is required."
                }));
        default:
            return View(new AuthorizeViewModel
            {
                ApplicationName = await _applicationManager.GetLocalizedDisplayNameAsync(application),
                Scope = request.Scope
            });
    }
}



        [Authorize(CookieAuthenticationDefaults.AuthenticationScheme), FormValueRequired("submit.Accept")]
        [HttpPost("~/connect/authorize")]
        public async Task<IActionResult> Accept(string returnUrl)
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the profile of the logged in user.
            var user = await _userManager.GetUserAsync(User) ??
                throw new InvalidOperationException("The user details cannot be retrieved.");

            // Retrieve the application details from the database.
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

            // Retrieve the permanent authorizations associated with the user and the calling client application.
            var authorizations = await _authorizationManager.FindAsync(
                subject: await _userManager.GetUserIdAsync(user),
                client: await _applicationManager.GetIdAsync(application),
                status: Statuses.Valid,
                type: AuthorizationTypes.Permanent,
                scopes: request.GetScopes()).ToListAsync();

            // Note: the same check is already made in the other action but is repeated
            // here to ensure a malicious user can't abuse this POST-only endpoint and
            // force it to return a valid response without the external authorization.
            if (authorizations.Count is 0 && await _applicationManager.HasConsentTypeAsync(application, ConsentTypes.External))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application."
                    }));
            }

            // Create the claims-based identity that will be used by OpenIddict to generate tokens.
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            // Add the claims that will be persisted in the tokens.
            identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                    .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                    .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                    .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user))
                    .SetClaims(Claims.Role, [.. (await _userManager.GetRolesAsync(user))]);

            // Note: in this sample, the granted scopes match the requested scope
            // but you may want to allow the user to uncheck specific scopes.
            // For that, simply restrict the list of scopes before calling SetScopes.
            identity.SetScopes(request.GetScopes());
            identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

            // Automatically create a permanent authorization to avoid requiring explicit consent
            // for future authorization or token requests containing the same scopes.
            var authorization = authorizations.LastOrDefault();
            authorization ??= await _authorizationManager.CreateAsync(
                identity: identity,
                subject: await _userManager.GetUserIdAsync(user),
                client: await _applicationManager.GetIdAsync(application),
                type: AuthorizationTypes.Permanent,
                scopes: identity.GetScopes());

            identity.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
            identity.SetDestinations(AuthService.GetDestinations);

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            ClaimsPrincipal claimsPrincipal;

            if (request.IsAuthorizationCodeGrantType())
            {
                claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }
            else if (request.IsRefreshTokenGrantType())
            {
                claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }
            else
            {
                throw new InvalidOperationException("The specified grant type is not supported.");
            }

            var user = await _userManager.FindByIdAsync(claimsPrincipal.GetClaim(Claims.Subject));

            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("Details concerning the calling client application cannot be found.");
            if (user is null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                    }));
            }

            if (!await _signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
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


        [HttpPost("~/connect/logout"), ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            await _signInManager.SignOutAsync();

            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = "/"
                });
        }
    }
    }

