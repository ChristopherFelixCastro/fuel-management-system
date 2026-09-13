using FuelManagement.Alerts.Detectors;
using FuelManagement.Alerts.Jobs;
using FuelManagement.Alerts.Services;
using FuelManagement.Api.Middleware;
using FuelManagement.Closures.Services;
using FuelManagement.Reports.Services;
using FuelManagement.Shared.Data;
using FuelManagement.Shared.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------
// Controllers & JSON Serializer
// ----------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ----------------------------------------------------------------
// Swagger Documentation
// ----------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FuelManagement API",
        Version = "v1",
        Description = "Módulo Cierres, Alertas y Reportes — Fuel Management System"
    });
});

// ----------------------------------------------------------------
// PostgreSQL DbContext
// ----------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<FuelDbContext>(opt =>
    opt.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention());

// ----------------------------------------------------------------
// Shared Services
// ----------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICurrentUserService, DevCurrentUserService>();
}

// ----------------------------------------------------------------
// Module Services Registration
// ----------------------------------------------------------------

// Module 1: Cierres Diarios
builder.Services.AddScoped<IClosureService, ClosureService>();

// Module 2: Alertas Operativas
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAlertDetector, LowInventoryDetector>();
builder.Services.AddScoped<IAlertDetector, AdjustmentPendingDetector>();
builder.Services.AddHostedService<AlertScannerJob>();

// Module 3: Reportes y Exportaciones
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IExportService, ExportService>();

// ----------------------------------------------------------------
// CORS Policy
// ----------------------------------------------------------------
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ----------------------------------------------------------------
// Web Application Pipeline
// ----------------------------------------------------------------
var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FuelManagement v1"));
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
