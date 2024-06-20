using AuthorizationServer.ViewModels;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace AuthorizationServer.Pages
{
    public class AuthorizeModel : PageModel
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public AuthorizeModel(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        [BindProperty]
        public AuthorizeViewModel ViewModel { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

            ViewModel = new AuthorizeViewModel
            {
                ApplicationName = await _applicationManager.GetLocalizedDisplayNameAsync(application),
                Scope = request.Scope
            };

            return Page();
        }
    }
}
