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
    ? builder.HostEnvironment.BaseAddress
    : "https://empirecardgame.com";

// 🧠 Register HttpClient with correct base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

// 🔧 Register your API services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthService>(provider => provider.GetRequiredService<AuthService>());
builder.Services.AddScoped<GameApi>();
builder.Services.AddScoped<GameHubService>();
builder.Services.AddSingleton<GameStateClientService>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<DeckService>();
builder.Services.AddScoped<EmpireGameService>();
builder.Services.AddScoped<CardDataService>();

// 🎮 Manual Game Services (Cockatrice-like)
builder.Services.AddScoped<ManualGameService>();

// 🃏 Enhanced Deck Management
builder.Services.AddScoped<UserDeckService>();

// 🏛️ Lobby Services
builder.Services.AddScoped<LobbyService>();

await builder.Build().RunAsync();
