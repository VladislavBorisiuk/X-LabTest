using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
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

                    return JsonSerializer.Deserialize<TokenResponse>(responseContent);
                }
                else
                {
                    MessageBox.Show("StatusCode is unsuccess");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
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
                    return JsonSerializer.Deserialize<TokenResponse>(responseContent);
                }
                else
                {
                    MessageBox.Show("StatusCode is unsuccess");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                return null;
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
