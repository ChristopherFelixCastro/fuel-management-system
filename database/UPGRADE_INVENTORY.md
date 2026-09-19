# Actualización incremental del módulo de inventario

Este procedimiento es para una instalación existente. No reinstala el DDL y no debe ejecutarse inicialmente sobre `fuel_management_db` sin haber validado antes una restauración aislada.

## 1. Respaldo y verificación

1. Detener temporalmente las escrituras de la API.
2. Crear un respaldo lógico en formato personalizado con `pg_dump -Fc` y otro de esquema con `pg_dump --schema-only`.
3. No incluir contraseñas en comandos, scripts ni archivos del repositorio. Use el diálogo seguro del cliente o un archivo `pgpass` restringido al usuario local.
4. Verificar el respaldo con `pg_restore --list`. Un listado correcto demuestra que el archivo es legible, pero la validación completa requiere restaurarlo en una base aislada.
5. Restaurar primero en una base temporal y ejecutar allí todas las verificaciones y pruebas SQL.

## 2. Orden exacto

Con `ON_ERROR_STOP` habilitado, aplicar individualmente:

1. `migrations/001_ajuste_conteo_fisico.sql`
2. `migrations/002_ajuste_cantidad_recalculada.sql`
3. `migrations/003_inventory_operations_compat.sql`
4. `migrations/004_dispatch_closure_guard.sql`

Cada archivo controla su propia transacción. Ante cualquier error, detener el proceso y no continuar con el siguiente.

## 3. Verificaciones posteriores

- Confirmar `ajuste_inventario.conteo_fisico` y las restricciones `chk_ajuste_conteo_fisico` y `chk_ajuste_cantidad`.
- Confirmar las firmas de `fn_reportar_ajuste`, `fn_aprobar_ajuste`, `fn_rechazar_ajuste`, `fn_registrar_recepcion`, `fn_registrar_transferencia` y `fn_registrar_despacho`.
- Comparar conteos de tickets, despachos, movimientos, cierres y auditoría con los valores previos.
- Confirmar que ningún tanque tenga stock negativo o superior a su capacidad.
- Ejecutar `tests/01_ticket_reservation_lifecycle.sql` y `tests/02_inventory_operations.sql` solo sobre la restauración aislada.
- Ejecutar pruebas API con JWT real contra esa misma base aislada.

## 4. Recuperación

- Un error dentro de un archivo revierte únicamente la transacción de ese archivo.
- Si todavía no se habilitó la aplicación, se pueden restaurar las definiciones de funciones capturadas antes de la actualización y revertir las restricciones mediante un script revisado.
- Si hubo escrituras de aplicación después de actualizar, no se debe restaurar encima de la base original. Restaurar el respaldo en otra base, verificarlo y realizar un cambio controlado de conexión.
- Nunca borrar movimientos, auditorías o cierres aprobados para intentar corregir la actualización.

## 5. Criterio de habilitación

La base original solo debe actualizarse cuando la restauración aislada supere las pruebas de recepción, transferencia con reservas, capacidad, ajuste pendiente/aprobado/rechazado, diferencia cero, concurrencia, despacho con cierre aprobado y auditoría sin duplicados.
