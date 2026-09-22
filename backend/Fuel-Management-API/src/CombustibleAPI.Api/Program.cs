using System.Text;
using CombustibleAPI.Api.Extensions;
using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CombustibleAPI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1) CONFIGURACIÓN Y SECRETOS
//    - Desarrollo: dotnet user-secrets (no toca el repositorio).
//    - Producción: variables de entorno / secret manager del proveedor Free Tier
//      elegido (SDP General sec. 7). Nunca se commitean credenciales reales.
// ============================================================================
builder.Configuration.AddEnvironmentVariables();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Key) && !builder.Environment.IsEnvironment("Testing"))
{
    // Falla rápido y explícito en vez de arrancar con una API insegura por accidente.
    throw new InvalidOperationException(
        "Falta configurar Jwt:Key. Use `dotnet user-secrets set Jwt:Key \"<valor>\"` en desarrollo " +
        "o la variable de entorno Jwt__Key en producción.");
}

// ============================================================================
// 2) SERVICIOS
// ============================================================================
builder.Services.AddControllers();
builder.Services.AddCombustibleApiServices(builder.Configuration);
builder.Services.AddCombustibleSwagger();

// ---- CORS: solo los orígenes explícitos del portal/PWA (SDP Iván sec. 4 "Protección") ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientesAutorizados", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ---- Autenticación JWT de corta duración ----
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = false; // no se persiste el token crudo en el contexto más de lo necesario
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key ?? "")),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30) // tolerancia mínima, acceso token es de vida corta
    };

    // Respuesta 401 uniforme (el middleware de auditoría capturará el 401 resultante).
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(
                "{\"error\":{\"code\":\"UNAUTHORIZED\",\"message\":\"Token ausente o inválido.\",\"details\":[]}}");
        }
    };
});

// ---- RBAC: políticas explícitas por rol (los 5 roles del SDP General sec. 3) ----
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloAdministrador", p => p.RequireRole("ADMINISTRADOR"));
    options.AddPolicy("AdministracionOperativa", p => p.RequireRole("ADMINISTRADOR", "SUPERVISOR"));
    options.AddPolicy("Despacho", p => p.RequireRole("DESPACHADOR"));
    options.AddPolicy("SoloLectura", p => p.RequireRole("ADMINISTRADOR", "SUPERVISOR", "AUDITOR"));
    options.AddPolicy("UsuarioAutenticado", p => p.RequireAuthenticatedUser());
});

var defaultConn = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(defaultConn))
{
    builder.Services.AddHealthChecks()
        .AddNpgSql(defaultConn, name: "postgresql");
}
else
{
    builder.Services.AddHealthChecks();
}

var app = builder.Build();

// ============================================================================
// 3) PIPELINE HTTP
//    Orden crítico: correlación -> excepciones -> HTTPS -> CORS -> auth -> auditoría
//    de accesos denegados -> autorización -> endpoints.
// ============================================================================
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FuelManagement.Api v1");
        c.RoutePrefix = "swagger";
    });
}

// En Render, HTTPS termina en el proxy del proveedor.
// El contenedor recibe HTTP interno; evitamos redirecciones duplicadas.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseCors("ClientesAutorizados");

app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>(); // audita 401/403 luego de que Authentication resuelve al usuario
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/ready");

app.Run();

// Expuesto para pruebas de integración (WebApplicationFactory<Program>).
public partial class Program { }
