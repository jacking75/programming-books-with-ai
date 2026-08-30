using MMORPG2D.ApiServer.Infrastructure;
using MMORPG2D.ApiServer.Middleware;
using MMORPG2D.ApiServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton(sp =>
    new DbConnectionFactory(builder.Configuration["Database:ConnectionString"]!));
builder.Services.AddSingleton(sp =>
    new RedisProvider(builder.Configuration["Redis:ConnectionString"]!));
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WorldService>();
builder.Services.AddScoped<CharacterService>();

var app = builder.Build();

app.UseTokenAuth();
app.MapControllers();
app.MapGet("/", () => "MMORPG2D.ApiServer is alive");

app.Run();
