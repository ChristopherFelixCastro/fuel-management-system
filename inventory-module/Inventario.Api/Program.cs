using Microsoft.EntityFrameworkCore;
using Inventario.Api.Data;
using Inventario.Api.Services;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<InventoryMockContext>(options =>
    options.UseInMemoryDatabase("InventarioMockDb"));

builder.Services.AddScoped<IStationService, StationService>();

builder.Services.AddScoped<ITankService, TankService>();

builder.Services.AddScoped<ISupplierService, SupplierService>();

builder.Services.AddScoped<IReceptionService, ReceptionService>();

builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddScoped<ITransferService, TransferService>();

builder.Services.AddScoped<IAdjustmentService, AdjustmentService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("Frontend");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryMockContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
