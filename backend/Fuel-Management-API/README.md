# Combustible API — Core Backend, Seguridad y API REST (`feature/core-api`)

Solución `.NET 8 Web API` correspondiente al alcance individual de **Iván**: arquitectura
central, seguridad (JWT + refresh rotation + RBAC), middlewares transversales
(excepciones, auditoría append-only, correlación), OpenAPI/Swagger, mapeo EF Core del
modelo de BD de Christopher, y los endpoints transaccionales críticos (`/auth/*`,
`/api/v1/tickets/validate`, `/api/v1/dispatches`).

## Estructura (Clean Architecture / capas)

```
CombustibleAPI.sln
src/
  CombustibleAPI.Domain          -> Entidades y enums, sin dependencias externas.
  CombustibleAPI.Application     -> DTOs, interfaces de servicios, ApiException, TokenService.
  CombustibleAPI.Infrastructure  -> DbContext + mapeo EF Core, SqlFunctionsRepository
                                     (invoca las funciones SQL nativas de Compañero 1),
                                     AuthService, TicketService, DispatchService, AuditService.
  CombustibleAPI.Api             -> Program.cs, Controllers, Middlewares, Swagger, DI.
```

Regla de dependencia: `Api -> Infrastructure -> Application -> Domain`. `Application` no
conoce EF Core ni Npgsql directamente (salvo `PasswordHasher` de Identity, que es parte
del framework compartido ASP.NET Core).

## ⚠️ Importante: esta API NO define el esquema de base de datos

Por diseño de equipo, **Compañero 1 (`feature/database`)** es dueño del modelo físico,
DDL/DML, índices, vistas y — sobre todo — de las **funciones SQL nativas** que contienen
la lógica transaccional crítica:

```
fn_obtener_stock_disponible, fn_generar_numero_ticket, fn_registrar_despacho,
fn_registrar_recepcion, fn_registrar_transferencia, fn_aprobar_ajuste,
fn_calcular_cierre_diario, fn_registrar_auditoria, ...
```

Esta solución **no reescribe esa lógica en C#**. En su lugar:

1. `AppDbContext` + `Configurations/*.cs` mapean entidades a las tablas ya existentes
   (nombres de tabla/columna en `snake_case`, ver cada `IEntityTypeConfiguration<T>`).
   **No se generan migraciones EF que creen/alteren el esquema** — si necesitas
   `dotnet ef migrations`, coordínalo antes con Compañero 1 para no duplicar la fuente
   de verdad del esquema.
2. `Infrastructure/Persistence/DbFunctions/SqlFunctionsRepository.cs` invoca las
   funciones SQL vía `NpgsqlCommand` (`SELECT * FROM fn_xxx(...)`), compartiendo la
   misma `NpgsqlTransaction` que el resto de la operación.

### ⚠️ Firmas de función asumidas — deben confirmarse

No tenía en contexto el código SQL real de Compañero 1, así que las firmas de
`fn_validar_ticket` y `fn_registrar_despacho` en `SqlFunctionsRepository.cs` están
**documentadas explícitamente como supuestas**, basadas en las reglas descritas en los
SDP (RN-04 a RN-08, sección "Seguridad QR" del SDP de Gabriel). Antes de conectar contra
la BD real:

- Confirma nombre exacto de función, orden/tipo de parámetros y nombres de columnas de
  retorno con Compañero 1.
- Ajusta los `NpgsqlParameter` y los `reader.GetOrdinal("...")` en
  `SqlFunctionsRepository.cs` si difieren.
- Si `fn_validar_ticket` no existe todavía y la validación de firma/token vive en el
  módulo de Gabriel en vez de en una función SQL propia, cambia `TicketService` para
  llamar al servicio de Gabriel en lugar de la función directamente (mismo contrato de
  salida `TicketOficialDto`).

## Cómo ejecutar localmente

Requisitos: SDK de `.NET 8`, PostgreSQL accesible (local o del proveedor Free Tier) con
el esquema de Compañero 1 ya aplicado.

```bash
cd src/CombustibleAPI.Api

# Secretos de desarrollo (NO usar appsettings para esto en producción)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=combustible_dev;Username=combustible_app;Password=********"
dotnet user-secrets set "Jwt:Key" "<genera un secreto aleatorio de al menos 32 caracteres>"

dotnet restore
dotnet build
dotnet run
```

Swagger disponible en `https://localhost:5443/swagger` (o `http://localhost:5080/swagger`)
en ambiente `Development`.

## Roles y RBAC

5 roles fijos definidos en el SDP General (sección 3): `ADMINISTRADOR`, `SUPERVISOR`,
`DESPACHADOR`, `SOLICITANTE`, `AUDITOR`. Un usuario tiene exactamente un rol (claim
`ClaimTypes.Role` en el JWT). Todo `DESPACHADOR` requiere `EstacionId` asignado — se
valida tanto en `AuthService.LoginAsync` como en `DispatchesController`, que además
compara la estación del JWT contra la estación del tanque antes de despachar.

## Flujo transaccional de `/api/v1/dispatches`

Ver `Infrastructure/Services/DispatchService.cs`. Resumen:

1. Revalida el ticket contra el estado oficial en BD (nunca confía en una validación
   previa de `/tickets/validate` ni en el body del request).
2. Verifica que el tanque pertenece a la estación del despachador autenticado (JWT).
3. Verifica disponibilidad (`fn_obtener_stock_disponible`) como *fast-fail*.
4. Ejecuta `fn_registrar_despacho` (descuenta tanque, crea movimiento `DESPACHO`, marca
   ticket `CONSUMIDO`, crea el registro de despacho) — todo en PostgreSQL.
5. Escribe auditoría encadenada (`fn_registrar_auditoria`) **dentro de la misma
   transacción**.
6. Solo tras el `COMMIT` se responde éxito. Cualquier excepción dispara `ROLLBACK` y
   ningún dato queda a medio escribir.

## Middlewares (orden en `Program.cs`)

`CorrelationId -> ExceptionHandling -> Swagger (solo Dev) -> HTTPS -> CORS ->
Authentication -> Audit (401/403) -> Authorization -> Controllers`.

- **ExceptionHandlingMiddleware**: traduce cualquier excepción (`ApiException` de negocio
  o no controlada) al formato `{ "error": {...}, "traceId": "..." }`. Nunca expone stack
  traces ni detalles de PostgreSQL.
- **AuditMiddleware**: registra intentos de acceso rechazados (401/403) a nivel
  transversal. Las mutaciones de negocio (login, despacho) auditan su propio evento con
  más contexto directamente desde el `Service` correspondiente.

## Pendiente antes de producción (según Definition of Done del SDP)

- [ ] Confirmar y ajustar firmas reales de las funciones SQL de Compañero 1.
- [ ] Cargar `Jwt:Key` y `ConnectionStrings:Default` reales vía secret manager del
      proveedor Free Tier elegido — nunca en `appsettings.*.json` versionado.
- [ ] Pruebas de concurrencia sobre `/api/v1/dispatches` (dos solicitudes simultáneas al
      mismo ticket).
- [ ] Pipeline de GitHub Actions (build + tests) antes de habilitar `main` protegida.
- [ ] Revisar con Christopher el contrato exacto de `/tickets/validate` que consume la PWA.
