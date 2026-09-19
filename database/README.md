# Base de datos SQL-first

Los scripts no se ejecutan automáticamente desde EF Core. Para una base nueva, el orden obligatorio es:

1. `ddl/00_extensions.sql`, `01_create_tables.sql`, `02_indexes.sql`, `03_ticket_sequence.sql`.
2. `functions/03_integrity_functions.sql` y después `ddl/04_integrity_triggers.sql`.
3. `dml/01_seed_catalogs.sql`; `02_seed_test_data.sql` únicamente en pruebas.
4. `views/01_inventory_views.sql`, `02_operational_views.sql`.
5. `functions/01_inventory_functions.sql`, `02_ticket_functions.sql`, `04_dispatch_functions.sql`, `08_audit_functions.sql`, `05_inventory_operations.sql`, `06_closure_functions.sql`, `07_closure_operations.sql`.
6. Las migraciones incrementales de una base ya creada se aplican en orden: `001_ajuste_conteo_fisico.sql`, `002_ajuste_cantidad_recalculada.sql`, `003_inventory_operations_compat.sql` y `004_dispatch_closure_guard.sql`. Todas conservan las firmas existentes y son repetibles.

`ddl/01_create_tables.sql` ya incluye `ajuste_inventario.conteo_fisico` y su restricción no negativa.

Para actualizar una instalación existente, no ejecute nuevamente los archivos de `ddl/`. Consulte `UPGRADE_INVENTORY.md` para respaldo, verificación, orden exacto y recuperación.

## Prueba aislada

Con Docker Desktop activo, desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File .\database\tests\run-isolated.ps1
```

El ejecutor crea `fuel-inventory-test` sobre `postgres:16-alpine`, aplica el orden anterior, ejecuta dos veces la migración para comprobar idempotencia y corre las pruebas SQL con `ON_ERROR_STOP=1`. Conserva el contenedor al terminar para inspección. Para eliminar exclusivamente ese contenedor temporal:

```powershell
docker rm -f fuel-inventory-test
```

Nunca apunte este ejecutor a una base existente.
