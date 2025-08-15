using Data.Contexts.MongoDB;
using Data.Contexts.SqlServer;
using Data.Repos.Interfaces;
using Data.Repos.MongoDB;
using Data.Repos.SqlServer;
using Microsoft.EntityFrameworkCore;
using Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DatabaseFactory
builder.Services.AddScoped<IDatabaseFactory, DatabaseFactory>();

// --- SQL Server Configuration (with User Secrets) ---
var sqlPassword = builder.Configuration["SqlPassword"];
var sqlConnStrTemplate = builder.Configuration.GetConnectionString("SqlServer_Staging");
if (!string.IsNullOrEmpty(sqlPassword) && sqlConnStrTemplate.Contains("{SqlPassword}"))
{
    var finalSqlConnStr = sqlConnStrTemplate.Replace("{SqlPassword}", sqlPassword);
    builder.Configuration["ConnectionStrings:SqlServer_Staging"] = finalSqlConnStr;
}

builder.Services.AddDbContext<EduBusSqlContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer_Staging"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);

// --- MongoDB Configuration (with User Secrets) ---
var mongoPassword = builder.Configuration["MongoPassword"];
var mongoConnStrTemplate = builder.Configuration.GetConnectionString("MongoDB");
if (!string.IsNullOrEmpty(mongoPassword) && mongoConnStrTemplate.Contains("{MongoPassword}"))
{
    var finalMongoConnStr = mongoConnStrTemplate.Replace("{MongoPassword}", mongoPassword);
    builder.Configuration["ConnectionStrings:MongoDB"] = finalMongoConnStr;
}

builder.Services.AddSingleton<EduBusMongoContext>();

// Repository Registration
builder.Services.AddScoped(typeof(ISqlRepository<>), typeof(SqlRepository<>));
builder.Services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
