using Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// SQL Server EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DevConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()) // retry policy
);

// MongoDB
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    // Get Passowrd from configuration (User Secrets will override appsettings)
    var mongoPassword = builder.Configuration["MongoPassword"];

    // Get string connection từ appsettings
    var connStrTemplate = builder.Configuration.GetConnectionString("MongoConnection");

    // Replace placeholder with real password
    var connStr = connStrTemplate.Replace("{MongoPassword}", mongoPassword);

    return new MongoClient(connStr);
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
