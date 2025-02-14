using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Blazored.LocalStorage; // Pastikan library ini sudah di-install

namespace BarangApp.Service
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;

        private string ApiBaseUrl => _configuration["ApiBaseUrl"] ?? "http://192.168.1.162:5069/api";

        public ApiService(HttpClient httpClient, IConfiguration configuration, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _localStorage = localStorage;
        }

        private async Task<HttpRequestMessage> CreateRequest(HttpMethod method, string endpoint)
        {
            var request = new HttpRequestMessage(method, $"{ApiBaseUrl}/{endpoint}");

            var token = await _localStorage.GetItemAsync<string>("accessToken");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return request;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var request = await CreateRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<T>();
            if (result == null)
            {
                throw new Exception($"API response is null for endpoint: {endpoint}");
            }

            return result;
        }


        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
        {
            var request = await CreateRequest(HttpMethod.Post, endpoint);
            request.Content = JsonContent.Create(data);
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
        {
            var request = await CreateRequest(HttpMethod.Put, endpoint);
            request.Content = JsonContent.Create(data);
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            var request = await CreateRequest(HttpMethod.Delete, endpoint);
            return await _httpClient.SendAsync(request);
        }
    }

}
