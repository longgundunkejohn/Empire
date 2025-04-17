using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Empire.Client;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 🌍 Dynamic API base URL — dev vs prod
var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? "http://localhost:5000" // ✅ Your local dev backend
    : "https://empirecardgame.com"; // ✅ Your prod domain

// 🧠 Register HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

// 🔧 Inject your API wrapper service
builder.Services.AddScoped<GameApi>();

await builder.Build().RunAsync();
