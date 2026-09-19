param([string]$Container = 'fuel-inventory-test', [string]$Password = 'local_test_only_2026')
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
docker run --name $Container -e POSTGRES_PASSWORD=$Password -e POSTGRES_DB=fuel_inventory_test -d postgres:16-alpine | Out-Null
try {
  do { Start-Sleep -Seconds 1; $ready = docker exec $Container pg_isready -U postgres -d fuel_inventory_test } until ($LASTEXITCODE -eq 0)
  $order = @(
    'database/ddl/00_extensions.sql','database/ddl/01_create_tables.sql','database/ddl/02_indexes.sql','database/ddl/03_ticket_sequence.sql',
    'database/functions/03_integrity_functions.sql','database/ddl/04_integrity_triggers.sql',
    'database/dml/01_seed_catalogs.sql','database/dml/02_seed_test_data.sql',
    'database/views/01_inventory_views.sql','database/views/02_operational_views.sql',
    'database/functions/01_inventory_functions.sql','database/functions/02_ticket_functions.sql','database/functions/04_dispatch_functions.sql',
    'database/functions/08_audit_functions.sql','database/functions/05_inventory_operations.sql','database/functions/06_closure_functions.sql','database/functions/07_closure_operations.sql',
    'database/migrations/001_ajuste_conteo_fisico.sql','database/migrations/001_ajuste_conteo_fisico.sql',
    'database/migrations/002_ajuste_cantidad_recalculada.sql','database/migrations/002_ajuste_cantidad_recalculada.sql',
    'database/migrations/003_inventory_operations_compat.sql','database/migrations/003_inventory_operations_compat.sql',
    'database/migrations/004_dispatch_closure_guard.sql','database/migrations/004_dispatch_closure_guard.sql',
    'database/tests/01_ticket_reservation_lifecycle.sql','database/tests/02_inventory_operations.sql'
  )
  foreach ($relative in $order) {
    Write-Host "==> $relative"
    Get-Content -Raw (Join-Path $repo $relative) | docker exec -i $Container psql -v ON_ERROR_STOP=1 -U postgres -d fuel_inventory_test
    if ($LASTEXITCODE -ne 0) { throw "Falló $relative" }
  }
  Write-Host 'Todas las pruebas SQL finalizaron correctamente.'
} finally {
  Write-Host "Contenedor temporal conservado como '$Container' para inspección. Elimínelo con: docker rm -f $Container"
}
