using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Empire.Client;
using System.Net.Http;
using Empire.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 🌍 Dynamic API base URL — switch between dev & prod automatically
var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? "http://134.209.20.47:5000"
    : "https://empirecardgame.com";


// 🧠 Register HttpClient with correct base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

// 🔧 Register your API service
builder.Services.AddScoped<GameApi>();
builder.Services.AddScoped<GameHubService>();
builder.Services.AddSingleton<GameStateClientService>();

await builder.Build().RunAsync();
