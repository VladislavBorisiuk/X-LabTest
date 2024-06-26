using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace AuthorizationServer.Pages
{
    [Authorize(CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ConsentModel : PageModel
    {
        [BindProperty]
        public string ReturnURL {  get; set; }

        public IActionResult OnGet(string returnURL)
        {
            ReturnURL = returnURL;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string grant)
        {
            if(grant != "Grant")
            {
                return Forbid();
            }

            var consentClaim = User.GetClaim("consent");

            if(string.IsNullOrEmpty(consentClaim)) 
            {
                User.SetClaim("consent", grant);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, User);

                return RedirectToAction(ReturnURL);
            }

            return Page();
        }
    }
}
