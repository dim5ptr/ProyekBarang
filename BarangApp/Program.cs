using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BarangApp;
using BarangApp.Services; 
using BarangApp.Service;// Make sure this is correct
using BarangApp.BarangServices;
using BarangApp.AuthService;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://192.168.1.162:5069/api";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// ✅ Register Services
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddBlazoredLocalStorage(); 
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<BarangService>();
builder.Services.AddScoped<ApiService>(); 
builder.Services.AddScoped<BarangApp.BarangServices.BarangService>();
builder.Services.AddScoped<AuthService>();




await builder.Build().RunAsync();
