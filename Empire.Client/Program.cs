using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Empire.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 🔁 Switch between local and production API endpoints
var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? "http://134.209.20.47:5000/api/" // 🔁 Always point to remote API for now
    : "/api/"; // ✅ When deployed, this will be relative to the server hosting both client + API

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

await builder.Build().RunAsync();
