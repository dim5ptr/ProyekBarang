using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarangApp.AuthService
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public string Token { get; private set; } = string.Empty;

        public AuthService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        public async Task<bool> Login(User user)
        {
            var response = await _http.PostAsJsonAsync("auth/login", user);
            
            if (!response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"❌ Login failed: {response.StatusCode}");
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
            if (result == null) 
            {
                Console.WriteLine("❌ Login response was empty");
                return false;
            }

            Token = result.AccessToken;
            Console.WriteLine($"✅ Token received: {Token}");

            // Simpan data profil
            await SaveProfileData(result);
            await Task.Delay(500); // Tunggu penyimpanan selesai

            // Verifikasi penyimpanan
            var isDataSaved = await VerifyProfileData();
            if (!isDataSaved)
            {
                Console.WriteLine("⚠️ Retrying profile data storage...");
                await SaveProfileData(result);
            }

            return isDataSaved;
        }

        private async Task SaveProfileData(AuthResponse result)
        {
            Console.WriteLine("📝 Menyimpan data profil...");

            await _localStorage.SetItemAsync("accessToken", result.AccessToken);
            await _localStorage.SetItemAsync("userId", result.Id.ToString());
            await _localStorage.SetItemAsync("username", result.Username ?? "");
            await _localStorage.SetItemAsync("email", result.Email ?? "");
            await _localStorage.SetItemAsync("fullName", result.FullName ?? "");
            await _localStorage.SetItemAsync("phoneNumber", result.PhoneNumber ?? "");
            await _localStorage.SetItemAsync("address", result.Address ?? "");

            await Task.Delay(500); // Tambahkan delay agar penyimpanan selesai

            Console.WriteLine("✅ Data profil berhasil disimpan!");
        }


        private async Task<bool> VerifyProfileData()
        {
            var token = await _localStorage.GetItemAsync<string>("accessToken");
            var username = await _localStorage.GetItemAsync<string>("username");
            var email = await _localStorage.GetItemAsync<string>("email");

            var isValid = !string.IsNullOrEmpty(token) && 
                          !string.IsNullOrEmpty(username) && 
                          !string.IsNullOrEmpty(email);

            Console.WriteLine($"🔍 Profile data verification: {(isValid ? "Success" : "Failed")}");
            return isValid;
        }

        public async Task<string?> GetProfileData(string key)
        {
            var data = await _localStorage.GetItemAsync<string>(key);
            Console.WriteLine(!string.IsNullOrEmpty(data) 
                ? $"✅ {key}: {data}" 
                : $"❌ {key} not found");
            return data;
        }
        public async Task<string?> GetAccessToken() => await _localStorage.GetItemAsync<string>("accessToken");

        public async Task<bool> UpdateProfile(UserProfile profile)
        {
            await AddAuthHeader();
            var token = await GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("⚠️ No access token found. Cannot update profile.");
                return false;
            }

            // Set header dengan accessToken
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _http.DefaultRequestHeaders.Remove("accessToken"); // Pastikan tidak ada duplikasi
            _http.DefaultRequestHeaders.Add("accessToken", token);

            // Log untuk memeriksa header sebelum request
            Console.WriteLine($"🔍 Sending profile update request...");
            Console.WriteLine($"📝 Headers: ");
            foreach (var header in _http.DefaultRequestHeaders)
            {
                Console.WriteLine($"   ➡️ {header.Key}: {string.Join(", ", header.Value)}");
            }

            // Kirim request dengan body JSON
            var response = await _http.PutAsJsonAsync("api/auth/update", profile);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ API Response: {response.StatusCode} - {responseContent}");
                return false;
            }

            Console.WriteLine("✅ Profile updated successfully!");

            // Simpan ulang data profil setelah update
            var authResponse = new AuthResponse
            {
                AccessToken = token,
                Id = int.TryParse(await _localStorage.GetItemAsync<string>("userId"), out var id) ? id : 0,
                Username = profile.Username,
                Email = profile.Email,
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Address = profile.Address
            };

            await SaveProfileData(authResponse);
            return true;
        }

        public async Task Logout()
        {
            await _localStorage.ClearAsync();
            Token = string.Empty;
            Console.WriteLine("✅ Logout successful - storage cleared");
        }
        public async Task<bool> LogoutWithAPI()
        {
            await AddAuthHeader();
            var token = await GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("⚠️ No access token found, logging out locally...");
                await Logout();
                return false;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Logout API failed: {response.StatusCode}");
                return false;
            }

            Console.WriteLine("✅ Logout successful from API");
            await Logout(); // Hapus token lokal setelah logout berhasil dari API
            return true;
        }

        private class AuthResponse
        {
            [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
            [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;
            [JsonPropertyName("id")] public int Id { get; set; }
            [JsonPropertyName("username")] public string? Username { get; set; }
            [JsonPropertyName("email")] public string? Email { get; set; }
            [JsonPropertyName("fullName")] public string? FullName { get; set; }
            [JsonPropertyName("phoneNumber")] public string? PhoneNumber { get; set; }
            [JsonPropertyName("address")] public string? Address { get; set; }
        }
        public async Task AddAuthHeader()
        {
            var token = await GetAccessToken();
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

    }
}
