using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using X_LabDataBase.Entityes;
using X_LabTest.Models;
using System.Threading.Tasks;
using System.Linq;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query;

[ApiController]
[Route("resources")]
public class ResourceController : ODataController
{
    private readonly UserManager<Person> _userManager;

    public ResourceController(
        UserManager<Person> userManager)
    {
        _userManager = userManager;
    }
    [EnableQuery]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet]
    public IActionResult Get()
    {
        var users = _userManager.Users.Select(user => new
        {
            Login = user.UserName,
            Password = user.PasswordHash
        }).ToList();

        return Ok(users);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] PersonServerDTO model)
    {
        if (ModelState.IsValid)
        {
            var user = new Person
            {
                UserName = model.Login,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok("Пользователь успешно зарегистрирован");
            }
            else
            {
                return BadRequest("Пользователь с таким именем уже существует");
            }
        }
        return BadRequest("Переданные на сервер данные не валидны");
    }
}
