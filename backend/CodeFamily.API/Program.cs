using CodeFamily.Data;
using CodeFamily.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// === 1. Add services to the container ===

// Get the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register the DbContext with the application's services
builder.Services.AddDbContext<CodeFamilyDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.MigrationsAssembly("CodeFamily.Data"))
);

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
app.UseAuthorization();
app.MapControllers();

// === 3. Run the application ===
app.Run();