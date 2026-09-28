using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Application.Services;
using CombustibleAPI.Infrastructure.Notifications;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using CombustibleAPI.Infrastructure.Security;
using CombustibleAPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCombustibleApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- Configuración fuertemente tipada ----
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<QrSecurityOptions>(configuration.GetSection(QrSecurityOptions.SectionName));
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<PublicTicketOptions>(configuration.GetSection(PublicTicketOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));

        // ---- Persistencia ----
        var connectionString = configuration.GetConnectionString("Default");
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
            }));

        services.AddScoped<SqlFunctionsRepository>();

        // ---- Contexto HTTP / usuario actual ----
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ---- HttpClients tipados para Notificaciones Externas ----
        services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<ISmsSender, InfobipSmsSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // ---- Servicios de aplicación / infraestructura ----
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IDispatchService, DispatchService>();
        services.AddScoped<IMastersService, MastersService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryOperationsService, InventoryOperationsService>();
        services.AddScoped<IClosureService, ClosureService>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IPublicTicketService, PublicTicketService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // ---- Background Worker para Notificaciones Outbox ----
        services.AddHostedService<NotificationBackgroundWorker>();

        return services;
    }
}
