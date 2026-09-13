using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Application.Interfaces;
using Tickets.Sandbox.Api.Application.Services;
using Tickets.Sandbox.Api.Application.Validators;
using Tickets.Sandbox.Api.Infrastructure.Authentication;
using Tickets.Sandbox.Api.Infrastructure.Configuration;
using Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;
using Tickets.Sandbox.Api.Infrastructure.Persistence;
using Tickets.Sandbox.Api.Infrastructure.Services;
using Tickets.Sandbox.Api.Middlewares;

namespace Tickets.Sandbox.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Configuración de Opciones seguras
        builder.Services.Configure<QrSecurityOptions>(options =>
        {
            builder.Configuration.GetSection(QrSecurityOptions.SectionName).Bind(options);
            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                options.SecretKey = builder.Configuration["QR_SECURITY_SECRET_KEY"] 
                    ?? "DevOnlyFallbackSecretKey_PleaseSetViaUserSecretsOrEnvVar_2026!";
            }
        });

        builder.Services.Configure<SmtpOptions>(
            builder.Configuration.GetSection(SmtpOptions.SectionName));

        // 2. Base de Datos (PostgreSQL Npgsql con fallback a InMemory para Testing)
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseInMemoryDatabase("TicketsSandboxDb");
            }
        });

        // 3. Controladores y Serialización JSON
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // 4. Validadores con FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<CreateRequestValidator>();

        // 5. Registro de Servicios de Dominio y Aplicación
        builder.Services.AddScoped<ITicketSequenceService, TicketSequenceService>();
        builder.Services.AddScoped<IRequestService, RequestService>();
        builder.Services.AddScoped<ITicketService, TicketService>();
        builder.Services.AddSingleton<IIdempotencyService, MemoryIdempotencyService>();

        // 6. Registro de Servicios de Infraestructura
        builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
        builder.Services.AddSingleton<IPdfGeneratorService, QuestPdfTicketGenerator>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.AddScoped<ISmsSender, SimulatedSmsSender>();
        builder.Services.AddScoped<INotificationService, NotificationService>();

        // 7. Registro de Contratos / Stubs de Integración (TODO-INTEGRACIÓN)
        builder.Services.AddSingleton<IMasterDataService, MasterDataStubService>();
        builder.Services.AddSingleton<IInventoryService, InventoryStubService>();
        builder.Services.AddScoped<IAuditService, AuditStubService>();

        // 8. Autenticación y Autorización
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        // 9. Swagger / OpenAPI Documentation
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Módulo de Solicitudes, Aprobación y Tickets Digitales - API",
                Version = "v1",
                Description = "API REST modular para gestión de solicitudes, aprobación, emisión de tickets con código QR, generación de PDF y validación de despacho."
            });
        });

        var app = builder.Build();

        // 10. Pipeline HTTP
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        app.UseMiddleware<IdempotencyMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tickets & Requests API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
