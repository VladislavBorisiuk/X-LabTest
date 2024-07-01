using Microsoft.AspNetCore.Identity;
using Moq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using X_LabDataBase.Entityes;
using X_LabTest.Models;
using Newtonsoft.Json;
using XLabApp.Models;
using XLabApp.Services;
using Moq.Protected;
using Xunit;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace LabXUnitTest
{
    public class DataServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpMessageHandler> _authHttpMessageHandlerMock;
        private readonly Mock<HttpMessageHandler> _resourceHttpMessageHandlerMock;
        private readonly DataService _dataService;
        private readonly Mock<UserManager<Person>> _userManagerMock;


        public DataServiceTests()
        {
            var store = new Mock<IUserStore<Person>>();
            _userManagerMock = new Mock<UserManager<Person>>(
                store.Object, null, null, null, null, null, null, null, null);

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            _authHttpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var authHttpClient = new HttpClient(_authHttpMessageHandlerMock.Object)
            {
                BaseAddress = new System.Uri("https://localhost:7168/")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("AuthServer")).Returns(authHttpClient);

            _resourceHttpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var resourceHttpClient = new HttpClient(_resourceHttpMessageHandlerMock.Object)
            {
                BaseAddress = new System.Uri("http://localhost:7169/")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("ResourceServer")).Returns(resourceHttpClient);

            _dataService = new DataService(_httpClientFactoryMock.Object);
        }

        [Fact]
        public async Task RegisterUserAsync_ValidModel_ReturnsSuccessMessage()
        {
            var model = new PersonDTO { Login = "testuser", Password = "Test@123" };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ѕользователь успешно зарегистрирован")
            };

            _resourceHttpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var result = await _dataService.RegisterUserAsync(model);

            Assert.Equal("ѕользователь успешно зарегистрирован", result);
        }

        [Fact]
        public async Task AuthorizeUserAsync_ValidModel_ReturnsTokenResponse()
        {
            var model = new PersonDTO { Login = "testuser", Password = "Test@123" };
            var tokenResponse = new TokenResponse { access_token = "access_token", refresh_token = "refresh_token" };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(tokenResponse))
            };

            _authHttpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var result = await _dataService.AuthorizeUserAsync(model);

            Assert.NotNull(result);
            Assert.Equal("access_token", result.access_token);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_ValidRefreshToken_ReturnsNewTokenResponse()
        {
            var refreshToken = "refresh_token";
            var tokenResponse = new TokenResponse { access_token = "new_access_token", refresh_token = "new_refresh_token" };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(tokenResponse))
            };

            _authHttpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var result = await _dataService.RefreshAccessTokenAsync(refreshToken);

            Assert.NotNull(result);
            Assert.Equal("new_access_token", result.access_token);
        }



        [Fact]
        public async Task Register_ValidModel_ReturnsOk()
        {
            var model = new PersonServerDTO { Login = "testuser", Password = "Test@123" };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Person>(), It.IsAny<string>()))
                            .ReturnsAsync(IdentityResult.Success);

            var controller = new ResourceController(_userManagerMock.Object);

            var result = await controller.Register(model);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            var model = new PersonServerDTO { Login = "testuser", Password = "Test@123" };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Person>(), It.IsAny<string>()))
                            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            var controller = new ResourceController(_userManagerMock.Object);

            var result = await controller.Register(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }
        [Fact]
        public async Task GetUsersAsync_ValidToken_ReturnsUsers()
        {
            var token = "valid_token";
            var usersResponse = new List<PersonDTO>
            {
                new PersonDTO { Login = "user1", Password = "pass1" },
                new PersonDTO { Login = "user2", Password = "pass2" }
            };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(usersResponse))
            };

            _resourceHttpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var result = await _dataService.GetUsersAsync(token);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Contains(result, u => u.Login == "user1");
            Assert.Contains(result, u => u.Login == "user2");
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            return true;
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("Short1", false)]
        [InlineData("nouppercase1", false)]
        [InlineData("NOLOWERCASE1", false)]
        [InlineData("NoDigits", false)]
        [InlineData("ValidPass1", true)]
        public void IsValidPassword_ShouldValidateCorrectly(string password, bool expected)
        {
            var result = IsValidPassword(password);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("validLogin123", true)]
        [InlineData("Valid_Login", true)]
        [InlineData("invalid login!", false)]
        [InlineData("invalidLogin#", false)]
        [InlineData("", false)]
        public void IsValidLogin_ShouldValidateCorrectly(string login, bool expected)
        {
            var result = IsValidLogin(login);
            Assert.Equal(expected, result);
        }

        private bool IsValidLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return false;
            var regex = new Regex("^[a-zA-Z0-9_@.-]+$");
            return regex.IsMatch(login);
        }
    }
}
