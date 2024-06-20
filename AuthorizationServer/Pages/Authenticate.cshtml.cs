using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using X_LabDataBase.Entityes;

namespace AuthorizationServer.Pages
{
    public class AuthenticateModel : PageModel
    {
        public string Login { get; set; }

        public string Password { get; set; }

        [BindProperty]
        public string ReturnUrl { get; set; }

        public string AuthStatus { get; set; } = "";

        private readonly UserManager<Person> _userManager;

        public AuthenticateModel(UserManager<Person> userManager)
        {
            _userManager = userManager;
            _userManager.Users.ToList();
        }
        public IActionResult OnGet(string returnUrl)
        {
            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                AuthStatus = "Login or password cannot be empty";
                return Page();
            }

            var user = await _userManager.FindByNameAsync(login);
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                AuthStatus = "Invalid login or password";
                return Page();
            }
            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id),
};

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            AuthStatus = "Authenticated";
            return Page();
        }

    }
}