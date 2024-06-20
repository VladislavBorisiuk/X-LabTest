using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace X_LabTest.Controllers
{
    [ApiController]
    [Authorize]
    [Route("resources")]
    public class ResourceController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var user = HttpContext.User.Identity.Name;

            return Ok($"user: {user}");
        }
    }
}
