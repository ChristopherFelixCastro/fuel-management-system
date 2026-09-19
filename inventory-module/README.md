# Módulo de Inventario (José Enrique) - versión desacoplada

## Cómo correrlo
- API: `cd Inventario.Api` y `dotnet run` (http://localhost:5076, docs en /scalar/v1)
- Web: `cd inventario-web`, `npm install`, `npm run dev` (http://localhost:5174)

## Qué reemplazar al conectar a la BD real
1. `Data/InventoryMockContext.cs`: usar el DbContext real. Los DbSet son Estaciones, Tanques, Proveedores, Recepciones, Movimientos, Transferencias y Ajustes.
2. `Models/`: mismas columnas que la rama `database`; reemplazar por las entidades reales.
3. `Program.cs`: quitar `UseInMemoryDatabase`, `EnsureCreated()`, la política CORS "Frontend" y `MapScalarApiReference()`.
4. `Services/MockRoleHelper.cs`: reemplazar por la autorización real. Los controllers usan `MockRoleHelper.HasRole(...)`.
5. Los controllers usan `usuarioIdSimulado = Guid.NewGuid()`: reemplazar por el id del usuario autenticado (llave foránea a `usuario`).
6. `inventario-web/src/api/inventoryApi.js`: quitar la cabecera `X-Mock-Role`, usar el token real y cambiar `VITE_API_URL` en `.env`.

## Otras cosas
1. Target: net10.0
2. Crear inventario-web/.env con: VITE_API_URL=http://localhost:5076/api/v1