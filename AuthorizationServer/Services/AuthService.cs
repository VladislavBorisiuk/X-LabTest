using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthorizationServer.Services
{
    public class AuthService
    {
        public string BuildRedirectUri(HttpContext httpContext, IDictionary<string, StringValues> parameters)
        {
            var uri = httpContext.Request.PathBase + httpContext.Request.Path + QueryString.Create(parameters);

            return uri;
        }

        public bool IsAuthenticated(AuthenticateResult result, OpenIddictRequest request)
        {
            if(!result.Succeeded)
            {
                return false;
            }

            if(request.MaxAge.HasValue && result.Properties != null)
            {
                var maxSeconds = TimeSpan.FromSeconds(request.MaxAge.Value);    

                var expire = !result.Properties.IssuedUtc.HasValue ||
                    DateTimeOffset.UtcNow - result.Properties.IssuedUtc > maxSeconds;

                if(expire)
                {
                    return false;
                }
            }
            /*if (result == null || !result.Succeeded || request.HasPrompt(Prompts.Login) ||
                (request.MaxAge != null && result.Properties?.IssuedUtc != null ||
                 DateTimeOffset.UtcNow - result.Properties.IssuedUtc > TimeSpan.FromSeconds(request.MaxAge.Value)))
            {

                return false;
            }
            else
            {
                return true;
            }*/

            return true;
        }

        public IDictionary<string, StringValues> ParceOAuthParameters(HttpContext httpContext)
        {
            var parameters = httpContext.Request.HasFormContentType ?
                httpContext.Request.Form
                    .Where(parameter => parameter.Key != Parameters.Prompt).
                    ToDictionary(kvp => kvp.Key, kvp => kvp.Value) :
                httpContext.Request.Query
                    .Where(parameter => parameter.Key != Parameters.Prompt)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return parameters;
        }

        public static List<string> GetDestinations(Claim claim)
        {
            var destinations = new List<string>();

            if(claim.Type is Claims.Name or Claims.Email)
            {
                destinations.Add(Destinations.AccessToken);
            }
            return destinations;
        }
    }
}
