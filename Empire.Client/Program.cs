using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Empire.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 🔁 Switch between local and remote API base URLs
var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? "https://localhost:7080" // Local API (when debugging)
    : "http://134.209.20.47/api/"; // Deployed API

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

await builder.Build().RunAsync();
