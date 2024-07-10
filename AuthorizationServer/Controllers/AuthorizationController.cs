using AuthorizationServer.Services.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthorizationServer.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly IGrantTypeHandlerFactory _grantTypeHandlerFactory;
        private readonly ILogger<AuthorizationController> _logger;

        public AuthorizationController(IGrantTypeHandlerFactory grantTypeHandlerFactory, ILogger<AuthorizationController> logger)
        {
            _grantTypeHandlerFactory = grantTypeHandlerFactory;
            _logger = logger;
        }

        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            try
            {
                var request = HttpContext.GetOpenIddictServerRequest() ??
                    throw new InvalidOperationException("Не удалось извлечь запрос OpenID Connect.");

                var handler = _grantTypeHandlerFactory.GetHandler(request.GrantType);
                return await handler.HandleAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время обмена токенами.");
                return BadRequest(new OpenIddictResponse
                {
                    Error = Errors.ServerError,
                    ErrorDescription = "Произошла ошибка во время обмена токенами. Пожалуйста, попробуйте позже."
                });
            }
        }
    }
}
