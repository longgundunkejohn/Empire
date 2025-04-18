using Empire.Server.Hubs;
using Empire.Server.Interfaces;
using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ────────────── Services ──────────────

// ✅ Mongo
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IConfiguration>()["MongoDB:ConnectionString"]));

builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IConfiguration>()["MongoDB:DatabaseName"]));

// ✅ JSON
builder.Services.AddControllers().AddJsonOptions(opts =>
    opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// ✅ Game services
builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<ICardDatabaseService, CardGameDatabaseService>();
builder.Services.AddSingleton<ICardService, CardService>();
builder.Services.AddSingleton<GameSessionService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<DeckService>();
builder.Services.AddSingleton<DeckLoaderService>();
builder.Services.AddSingleton<CardFactory>();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen();

// ✅ SignalR
builder.Services.AddSignalR().AddJsonProtocol(opt =>
    opt.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// ✅ CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowEmpireClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://empirecardgame.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ────────────── Pipeline ──────────────
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

// ✅ Docker/Nginx reverse proxy headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ✅ Force domain (prod)
app.Use((context, next) =>
{
    context.Request.Host = new HostString("empirecardgame.com");
    return next();
});

app.UseCors("AllowEmpireClient");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapHub<GameHub>("/gamehub");
app.MapFallbackToFile("index.html");

app.Run();
