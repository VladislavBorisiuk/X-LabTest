using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using X_LabDataBase.Entityes;
using X_LabTest.Models;

[ApiController]
[Route("resources")]

public class ResourceController : ControllerBase
{
    private readonly UserManager<Person> _userManager;

    public ResourceController(UserManager<Person> userManager)
    {
        _userManager = userManager;
    }
    [Authorize]
    [HttpGet]
    public IActionResult Get()
    {
        var users = _userManager.Users.ToList();
        string userList = "";
        foreach (var user in users)
        {
            userList += $"Имя пользователя: {user.UserName} Пароль пользователя {user.PasswordHash} \n";
        }
        return Ok($"users: {userList}");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] PersonDTO model)
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
                return BadRequest(result.Errors);
            }
        }
        return BadRequest("Invalid model");
    }
}
