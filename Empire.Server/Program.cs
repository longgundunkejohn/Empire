using Empire.Server.Interfaces;
using Empire.Server.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ───── Add Services ──────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register controllers
builder.Services.AddControllers(); // ✅ ← Add this line

// ───── MongoDB Configuration ─────────────────────

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>()
      .GetDatabase(builder.Configuration["MongoDB:DatabaseName"]));

builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
builder.Services.AddSingleton<DeckLoaderService>();

// ───── Game Services ─────────────────────────────

builder.Services.AddScoped<ICardDatabaseService, CardDatabaseService>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<CardFactory>();
builder.Services.AddScoped<GameSessionService>();

// ───── CORS for Blazor client ────────────────────

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ───── HTTP Pipeline ─────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapControllers(); // required to actually serve your endpoints

app.Run();
