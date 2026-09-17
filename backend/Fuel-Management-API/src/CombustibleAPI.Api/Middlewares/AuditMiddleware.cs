using CombustibleAPI.Application.Interfaces;

namespace CombustibleAPI.Api.Middlewares;

/// <summary>
/// Middleware de Auditoría Append-Only (SDP Iván sec. 3/4).
///
/// División de responsabilidades para no duplicar/ensuciar la cadena de hashes:
///  - Las MUTACIONES DE NEGOCIO (login, despacho, aprobar solicitud, etc.) auditan su propio
///    evento con contexto rico (datos anteriores/nuevos) directamente desde el Service
///    correspondiente, dentro de la misma transacción cuando aplica (ver DispatchService,
///    AuthService).
///  - Este middleware complementa registrando, a nivel transversal, los INTENTOS DE ACCESO
///    RECHAZADOS (401/403) sobre rutas protegidas -- información que ningún Service
///    individual vería porque el pipeline de autorización corta la ejecución antes de
///    llegar al Controller.
/// Ambos caminos escriben en audit_log a través de IAuditService -> fn_registrar_auditoria,
/// preservando un único hash encadenado del lado de PostgreSQL.
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService, ICurrentUserService currentUser)
    {
        await _next(context);

        if (context.Response.StatusCode is 401 or 403)
        {
            try
            {
                await auditService.RegistrarAsync(
                    usuarioId: currentUser.UsuarioId,
                    accion: context.Response.StatusCode == 401 ? "ACCESO_NO_AUTENTICADO" : "ACCESO_DENEGADO",
                    entidad: "HttpRequest",
                    entidadId: context.Request.Path,
                    ipAddress: context.Connection.RemoteIpAddress?.ToString(),
                    datosAnteriores: null,
                    datosNuevos: new { method = context.Request.Method, path = context.Request.Path.Value, status = context.Response.StatusCode },
                    ct: context.RequestAborted);
            }
            catch (Exception ex)
            {
                // La auditoría de un intento rechazado nunca debe tumbar la respuesta ya emitida.
                _logger.LogError(ex, "No se pudo registrar auditoría de acceso denegado para {Path}", context.Request.Path);
            }
        }
    }
}
