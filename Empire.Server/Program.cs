using Empire.Server.Interfaces;
using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ────────────── Services ──────────────

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new MongoClient(config["MongoDB:ConnectionString"]);
});
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var config = sp.GetRequiredService<IConfiguration>();
    return client.GetDatabase(config["MongoDB:DatabaseName"]);
});

builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<ICardDatabaseService, CardGameDatabaseService>();
builder.Services.AddScoped<ICardService, CardService>();

builder.Services.AddSingleton<GameSessionService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<DeckService>();
builder.Services.AddTransient<DeckLoaderService>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEmpireClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://empirecardgame.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ────────────── App Pipeline ──────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Use((context, next) =>
{
    context.Request.Host = new HostString("empirecardgame.com");
    return next();
});

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowEmpireClient");
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
