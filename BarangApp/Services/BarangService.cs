using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Blazored.LocalStorage;
using System;

namespace BarangApp.BarangServices
{
    public class BarangService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;

        public BarangService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        private async Task<HttpRequestMessage> CreateRequest(HttpMethod method, string endpoint, object? body = null)
        {
            var request = new HttpRequestMessage(method, $"api/{endpoint}");

            // Ambil token dari LocalStorage
            var token = await _localStorage.GetItemAsync<string>("accessToken");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"✅ Token ditambahkan ke Header: {token}");
            }
            else
            {
                Console.WriteLine("⚠️ Tidak ada accessToken ditemukan di LocalStorage!");
            }

            // Tambahkan body jika ada
            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            // Logging header sebelum request dikirim
            Console.WriteLine("📝 Headers: ");
            foreach (var header in request.Headers)
            {
                Console.WriteLine($"   🔹 {header.Key}: {string.Join(", ", header.Value)}");
            }

            return request;
        }

        public async Task<List<Models.Barang>> GetBarangAsync()
        {
            var request = await CreateRequest(HttpMethod.Get, "barang"); 

            // ✅ Logging sebelum mengirim request
            Console.WriteLine("📡 Mengirim GET Barang...");
            Console.WriteLine($"📝 Headers dalam GET:");
            foreach (var header in request.Headers)
            {
                Console.WriteLine($"   🔹 {header.Key}: {string.Join(", ", header.Value)}");
            }

            var response = await _http.SendAsync(request);

            Console.WriteLine($"📡 GET Barang - Status Code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Models.Barang>>() ?? new List<Models.Barang>();
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Gagal Fetch Data: {response.StatusCode}, {errorMessage}");
                return new List<Models.Barang>();
            }
        }


        public async Task<bool> CreateBarangAsync(Models.Barang barang)
        {
            var token = await _localStorage.GetItemAsync<string>("accessToken");

            var requestBody = new
            {
                Kode = barang.Kode,
                Nama = barang.Nama,
                Stok = barang.Stok,
                Harga = barang.Harga,
                accessToken = token
            };

            var request = await CreateRequest(HttpMethod.Post, "barang", requestBody);
            var response = await _http.SendAsync(request);

            Console.WriteLine($"📡 POST Barang - Status Code: {response.StatusCode}");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateBarangAsync(Models.Barang barang)
        {
            var request = await CreateRequest(HttpMethod.Put, "barang/update", barang);
            var response = await _http.SendAsync(request);

            Console.WriteLine($"📡 PUT Barang - Status Code: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Gagal Update Barang: {response.StatusCode}, {errorMessage}");
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteBarangAsync(int id)
        {
            var request = await CreateRequest(HttpMethod.Delete, $"barang/delete?id={id}");
            var response = await _http.SendAsync(request);

            Console.WriteLine($"📡 DELETE Barang - Status Code: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Gagal Hapus Barang: {response.StatusCode}, {errorMessage}");
            }

            return response.IsSuccessStatusCode;
        }
    }
}
