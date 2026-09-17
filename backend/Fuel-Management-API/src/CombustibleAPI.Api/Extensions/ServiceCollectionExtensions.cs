using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Application.Services;
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

        // ---- Servicios de aplicación / infraestructura ----
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IDispatchService, DispatchService>();
        services.AddScoped<IMastersService, MastersService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IClosureService, ClosureService>();
        services.AddScoped<IRequestService, RequestService>();

        return services;
    }
}
