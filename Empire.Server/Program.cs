using Empire.Server.Interfaces;
using Empire.Server.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ───── Services ─────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>()
        .GetDatabase(builder.Configuration["MongoDB:DatabaseName"]));

builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
builder.Services.AddSingleton<DeckLoaderService>();
builder.Services.AddScoped<ICardDatabaseService, CardDatabaseService>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<CardFactory>();
builder.Services.AddScoped<GameSessionService>();

// ───── CORS (allow Blazor Client) ─────

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:5049") // Your Blazor Client HTTPS
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ───── HTTP Pipeline ─────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
