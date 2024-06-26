using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using X_LabTest.Models;

namespace X_LabTest.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(PersonDTO model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var apiUrl = "http://your-api-url/resources/register"; // Замените на фактический URL вашего API

                    var client = _httpClientFactory.CreateClient();

                    var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        return Ok(responseContent);
                    }
                    else
                    {
                        return BadRequest($"Registration failed with status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"Registration failed: {ex.Message}");
                }
            }
            else
            {
                return BadRequest("Invalid model");
            }
        }
    }
}
