using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using XLabApp.Models;
using XLabApp.Services.Interfaces;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace XLabApp.Services
{
    public class DataService : IDataService
    {
        private readonly HttpClient _authHttpClient;
        private readonly HttpClient _resourceHttpClient;
        private TokenResponse _currentToken;
        public TokenResponse CurrentToken { get => _currentToken; set => _currentToken = value; }

        public DataService(IHttpClientFactory httpClientFactory)
        {
            _authHttpClient = httpClientFactory.CreateClient("AuthServer");
            _resourceHttpClient = httpClientFactory.CreateClient("ResourceServer");
        }

        public async Task<string> RegisterUserAsync(PersonDTO model)
        {
            try
            {
                var json = JsonConvert.SerializeObject(model);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _resourceHttpClient.PostAsync("/resources/register", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Ошибка: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }

        public async Task<TokenResponse> AuthorizeUserAsync(PersonDTO model)
        {
            try
            {
                var requestBody = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "username", model.Login },
                    { "password", model.Password },
                    { "client_id", "web-client" },
                    { "scope", "api1 offline_access" }
                };

                var requestContent = new FormUrlEncodedContent(requestBody);

                var response = await _authHttpClient.PostAsync("/connect/token", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    CurrentToken = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                    CurrentToken.ExpiryTime = DateTime.UtcNow.AddSeconds(CurrentToken.expires_in);
                    return CurrentToken;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка: {response.StatusCode} - {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return null;
            }
        }

        public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var requestBody = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken },
                    { "client_id", "web-client" },
                    { "scope", "api1 offline_access" }
                };

                var requestContent = new FormUrlEncodedContent(requestBody);
                var response = await _authHttpClient.PostAsync("/connect/token", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    CurrentToken = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                    CurrentToken.ExpiryTime = DateTime.UtcNow.AddSeconds(_currentToken.expires_in);
                    return CurrentToken;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка: {response.StatusCode} - {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PersonDTO>> GetUsersAsync(string? OdataCode)
        {
            await EnsureTokenAsync();
            try
            {
                _resourceHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentToken.access_token);
                HttpResponseMessage response = new();
                if (!string.IsNullOrEmpty(OdataCode))
                {
                    response = await _resourceHttpClient.GetAsync($"resources?{OdataCode}");
                }
                else
                {
                    response = await _resourceHttpClient.GetAsync("resources");
                }
                

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonConvert.DeserializeObject<List<PersonDTO>>(json);
                    return users;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Ошибка: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении пользователя. Токен не корректен или время жизни истекло");
                throw new Exception($"Ошибка при получении пользователей: {ex.Message}");
            }
        }

        private async Task EnsureTokenAsync()
        {
            if (CurrentToken == null || CurrentToken.ExpiryTime <= DateTime.UtcNow)
            {
                CurrentToken = await RefreshAccessTokenAsync(CurrentToken.refresh_token);
            }
        }
    }
}
