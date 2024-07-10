using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace AuthorizationServer.Services.Interfaces
{
    public interface IGrantTypeHandler
    {
        Task<IActionResult> HandleAsync(OpenIddictRequest request);
    }
}
