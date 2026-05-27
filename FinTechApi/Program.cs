using FinTechApi.Data;
using FinTechApi.Models;
using FinTechApi.Repositories;
using FinTechApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// DbContext: use a connection string if present, otherwise sqlite file for local development.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=fintech.db";
builder.Services.AddDbContext<FinTechContext>(options =>
    options.UseSqlServer(connectionString));






// Register specialized repository implementations (overrides generic for the same service).
builder.Services.AddScoped<IAsyncRepository<Customer, Guid>, CustomerRepository>();
builder.Services.AddScoped<IAsyncRepository<Order, Guid>, EfRepository<Order, Guid>>();
builder.Services.AddScoped<IAsyncRepository<Trade, Guid>, EfRepository<Trade, Guid>>();
// Domain services
builder.Services.AddScoped<IOrderService, OrderService>();

// Simple market price provider used for market orders (replace with real implementation in prod)
builder.Services.AddSingleton<IMarketPriceProvider, SimpleMarketPriceProvider>();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
