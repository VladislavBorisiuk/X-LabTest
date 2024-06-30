using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using XLabApp.Models;
using XLabApp.Services.Interfaces;

namespace XLabApp.Services
{
    public class DataService : IDataService
    {
        private readonly HttpClient _authHttpClient;
        private readonly HttpClient _resourceHttpClient;

        public DataService(IHttpClientFactory httpClientFactory)
        {
            _authHttpClient = httpClientFactory.CreateClient("AuthServer");
            _resourceHttpClient = httpClientFactory.CreateClient("ResourceServer");
        }

        public async Task<string> RegisterUserAsync(PersonDTO model)
        {
            try
            {
                var requestBody = new Dictionary<string, string>
                {
                    { "username", model.Login },
                    { "password", model.Password },
                };

                var requestContent = new FormUrlEncodedContent(requestBody);

                var response = await _resourceHttpClient.PostAsync("resources/register", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> AuthorizeUserAsync(PersonDTO model)
        {
            try
            {
                var tokenEndpoint = "https://localhost:7168/connect/token";

                var requestBody = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "username", model.Login },
                    { "password", model.Password },
                    { "client_id", "web-client" },
                    { "client_secret", "901564A5-E7FE-42CB-B18D-61EF6A8F3654" },
                    { "scope", "api1" }
                };

                var requestContent = new FormUrlEncodedContent(requestBody);

                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                {
                    Content = requestContent
                };

                var response = await _authHttpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Парсим JSON-ответ
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                    // Извлекаем токен доступа (access_token)
                    string accessToken = tokenResponse.access_token;

                    return accessToken;
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GetUsersAsync(string token)
        {
            try
            {
                _resourceHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await _resourceHttpClient.GetAsync("resources");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
