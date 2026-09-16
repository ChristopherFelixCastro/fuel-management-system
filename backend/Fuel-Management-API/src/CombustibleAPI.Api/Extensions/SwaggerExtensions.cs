using Microsoft.OpenApi.Models;

namespace CombustibleAPI.Api.Extensions;

public static class SwaggerExtensions
{
    /// <summary>
    /// OpenAPI/Swagger como contrato oficial del equipo (SDP General sec. 8, SDP Iván sec. 3/6).
    /// </summary>
    public static IServiceCollection AddCombustibleSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FuelManagement.Api",
                Version = "v1",
                Description = "Contrato oficial de la Plataforma Web y PWA de Gestión de Tickets Digitales e Inventario de Combustible."
            });

            c.TagActionsBy(api => new[] { "FuelManagement.Api" });
            c.DocInclusionPredicate((name, api) => true);

            var jwtScheme = new OpenApiSecurityScheme
            {
                Scheme = "bearer",
                BearerFormat = "JWT",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Description = "Ingrese únicamente el token JWT (sin el prefijo 'Bearer ')."
            };

            c.AddSecurityDefinition("Bearer", jwtScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
