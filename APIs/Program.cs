using Data.Contexts.MongoDB;
using Data.Contexts.SqlServer;
using Data.Repos.Interfaces;
using Data.Repos.MongoDB;
using Data.Repos.SqlServer;
using Microsoft.EntityFrameworkCore;
using Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//DatabaseFactory
builder.Services.AddScoped<IDatabaseFactory, DatabaseFactory>();

// SQL Server Configuration
builder.Services.AddDbContext<EduBusSqlContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer"),
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

// MongoDB Configuration
// Read connection string from appsettings, replace {MongoPassword} with User Secrets
var mongoPassword = builder.Configuration["MongoPassword"];
var connStrTemplate = builder.Configuration.GetConnectionString("MongoDB");
var finalMongoConnStr = connStrTemplate.Replace("{MongoPassword}", mongoPassword);

// Override into IConfiguration to EduBusMongoContext can read
builder.Configuration["ConnectionStrings:MongoDB"] = finalMongoConnStr;

builder.Services.AddSingleton<EduBusMongoContext>();


// Repository Registration
builder.Services.AddScoped(typeof(ISqlRepository<>), typeof(SqlRepository<>));
builder.Services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

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

app.Run();
