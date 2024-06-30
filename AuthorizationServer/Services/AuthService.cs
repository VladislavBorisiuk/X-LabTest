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
