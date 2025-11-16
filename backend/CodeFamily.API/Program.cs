using CodeFamily.Data;
using CodeFamily.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CodeFamily.API.Workers;
using CodeFamily.Data.Services;
using CodeFamily.Core.Services;   // <-- Add this

var builder = WebApplication.CreateBuilder(args);

// === 1. Add services to the container ===
// Register our background worker

// Get the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register the DbContext with the application's services
builder.Services.AddDbContext<CodeFamilyDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.MigrationsAssembly("CodeFamily.Data"))
);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHostedService<RepositoryIngestionWorker>();
// Register our custom application services
builder.Services.AddScoped<IRepositoryIngestionService, RepositoryIngestionService>();


// Map C# enums to PostgreSQL enums
NpgsqlConnection.GlobalTypeMapper.MapEnum<RepoStatus>();
NpgsqlConnection.GlobalTypeMapper.MapEnum<DependencyType>();
NpgsqlConnection.GlobalTypeMapper.MapEnum<ReviewState>();
NpgsqlConnection.GlobalTypeMapper.MapEnum<WebhookStatus>();


// Add default API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === 2. Build the application ===
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();


app.UseAuthorization();
app.MapControllers();

// === 3. Run the application ===
app.Run();