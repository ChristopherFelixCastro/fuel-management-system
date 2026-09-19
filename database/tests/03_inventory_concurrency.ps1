param(
    [string]$Database = 'fuel_management_test',
    [string]$Psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
)

$ErrorActionPreference = 'Stop'
if ($Database -eq 'fuel_management_db') {
    throw 'Esta prueba nunca puede ejecutarse sobre fuel_management_db.'
}

function Invoke-Sql([string]$Sql, [switch]$TuplesOnly) {
    $args = @('-h','localhost','-p','5432','-d',$Database,'-U','postgres','-w','-X','-v','ON_ERROR_STOP=1')
    if ($TuplesOnly) { $args += '-At' }
    $args += @('-c',$Sql)
    $output = & $Psql @args 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
    return $output
}

$ids = (Invoke-Sql -TuplesOnly @'
INSERT INTO estacion(codigo,nombre)
VALUES ('EST-CONC-TEST','Estación concurrencia')
RETURNING id;
'@ | Select-Object -First 1).Trim()

try {
    $context = (Invoke-Sql -TuplesOnly @"
WITH destination AS (
    INSERT INTO tanque(estacion_id,tipo_combustible_id,codigo,nombre,capacidad_maxima,stock_actual,nivel_critico)
    SELECT '$ids'::uuid,tipo_combustible_id,'TNQ-CONC-TEST','Tanque concurrencia',1000,0,0
    FROM tanque WHERE codigo='TNQ-DIESEL-001'
    RETURNING id
)
SELECT o.id || '|' || d.id || '|' || o.estacion_id || '|' || o.tipo_combustible_id || '|' ||
       u.id || '|' || tk.id || '|' || tk.cantidad_autorizada
FROM tanque o CROSS JOIN destination d
CROSS JOIN LATERAL (SELECT id FROM usuario WHERE nombre_usuario='supervisor.test') u
CROSS JOIN LATERAL (
    SELECT id,cantidad_autorizada FROM ticket
    WHERE estacion_id=o.estacion_id AND tipo_combustible_id=o.tipo_combustible_id
      AND estado IN ('CREADO','ENVIADO') AND fecha_expiracion>NOW()
    LIMIT 1
) tk
WHERE o.codigo='TNQ-DIESEL-001';
"@ | Select-Object -First 1).Trim().Split('|')

    $origin,$destination,$station,$fuel,$user,$ticket,$originalAmount = $context
    Invoke-Sql "UPDATE tanque SET stock_actual=400 WHERE id='$origin';"

    $lockerSql = @"
BEGIN;
SELECT pg_advisory_xact_lock(hashtextextended('${station}:${fuel}',0));
UPDATE ticket SET cantidad_autorizada=650 WHERE id='$ticket';
SELECT pg_sleep(2);
COMMIT;
"@
    $transferSql = "SELECT fn_registrar_transferencia('$origin','$destination','$user',100,NULL);"

    $locker = Start-Job -ScriptBlock { param($exe,$db,$sql) & $exe -h localhost -p 5432 -d $db -U postgres -w -X -v ON_ERROR_STOP=1 -c $sql 2>&1 } -ArgumentList $Psql,$Database,$lockerSql
    Start-Sleep -Milliseconds 300
    $transfer = Start-Job -ScriptBlock { param($exe,$db,$sql) & $exe -h localhost -p 5432 -d $db -U postgres -w -X -v ON_ERROR_STOP=1 -c $sql 2>&1; $LASTEXITCODE } -ArgumentList $Psql,$Database,$transferSql
    Wait-Job $locker,$transfer | Out-Null
    $transferOutput = (Receive-Job $transfer) -join "`n"
    if ($transferOutput -notmatch 'STOCK_DISPONIBLE_INSUFICIENTE_POR_RESERVAS') {
        throw "La transferencia no respetó la reserva concurrente: $transferOutput"
    }

    Invoke-Sql "UPDATE ticket SET cantidad_autorizada=$originalAmount WHERE id='$ticket';"

    $adjustment = (Invoke-Sql -TuplesOnly @"
INSERT INTO ajuste_inventario(tanque_id,reportado_por_usuario_id,tipo_ajuste,cantidad,conteo_fisico,motivo)
VALUES ('$origin','$user','NEGATIVO',100,300,'Prueba de concurrencia')
RETURNING id;
"@ | Select-Object -First 1).Trim()

    $locker2 = Start-Job -ScriptBlock { param($exe,$db,$sql) & $exe -h localhost -p 5432 -d $db -U postgres -w -X -v ON_ERROR_STOP=1 -c $sql 2>&1 } -ArgumentList $Psql,$Database,$lockerSql
    Start-Sleep -Milliseconds 300
    $approveSql = "SELECT fn_aprobar_ajuste('$adjustment','$user');"
    $approval = Start-Job -ScriptBlock { param($exe,$db,$sql) & $exe -h localhost -p 5432 -d $db -U postgres -w -X -v ON_ERROR_STOP=1 -c $sql 2>&1; $LASTEXITCODE } -ArgumentList $Psql,$Database,$approveSql
    Wait-Job $locker2,$approval | Out-Null
    $approvalOutput = (Receive-Job $approval) -join "`n"
    if ($approvalOutput -notmatch 'STOCK_DISPONIBLE_INSUFICIENTE_POR_RESERVAS') {
        throw "El ajuste no respetó la reserva concurrente: $approvalOutput"
    }

    Invoke-Sql "UPDATE ticket SET cantidad_autorizada=$originalAmount WHERE id='$ticket'; DELETE FROM ajuste_inventario WHERE id='$adjustment';"
    Write-Host 'Pruebas de concurrencia superadas.'
}
finally {
    Invoke-Sql "DELETE FROM tanque WHERE estacion_id='$ids'; DELETE FROM estacion WHERE id='$ids';" | Out-Null
}
