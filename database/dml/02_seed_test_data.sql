

BEGIN;

 
-- 1. DEPARTAMENTO DE PRUEBA
 

INSERT INTO departamento (
    codigo,
    nombre,
    descripcion
)
VALUES (
    'DEP-001',
    'Transportación',
    'Departamento de prueba para gestión de combustible.'
);


 
-- 2. ESTACION DE PRUEBA
 

INSERT INTO estacion (
    codigo,
    nombre,
    ubicacion,
    descripcion
)
VALUES (
    'EST-001',
    'Estación Principal',
    'Campus principal',
    'Estación utilizada para pruebas del sistema.'
);


 
-- 3. TANQUES DE PRUEBA
 

INSERT INTO tanque (
    estacion_id,
    tipo_combustible_id,
    codigo,
    nombre,
    capacidad_maxima,
    stock_actual,
    nivel_critico
)
SELECT
    e.id,
    tc.id,
    'TNQ-DIESEL-001',
    'Tanque Diésel 1',
    1000.00,
    500.00,
    100.00
FROM estacion e
JOIN tipo_combustible tc
    ON tc.codigo = 'DIESEL'
WHERE e.codigo = 'EST-001';


INSERT INTO tanque (
    estacion_id,
    tipo_combustible_id,
    codigo,
    nombre,
    capacidad_maxima,
    stock_actual,
    nivel_critico
)
SELECT
    e.id,
    tc.id,
    'TNQ-DIESEL-002',
    'Tanque Diésel 2',
    800.00,
    300.00,
    80.00
FROM estacion e
JOIN tipo_combustible tc
    ON tc.codigo = 'DIESEL'
WHERE e.codigo = 'EST-001';


INSERT INTO tanque (
    estacion_id,
    tipo_combustible_id,
    codigo,
    nombre,
    capacidad_maxima,
    stock_actual,
    nivel_critico
)
SELECT
    e.id,
    tc.id,
    'TNQ-GAS-001',
    'Tanque Gasolina 1',
    900.00,
    400.00,
    90.00
FROM estacion e
JOIN tipo_combustible tc
    ON tc.codigo = 'GASOLINA'
WHERE e.codigo = 'EST-001';


 
-- 4. EMPLEADO DE PRUEBA
 

INSERT INTO empleado (
    departamento_id,
    codigo_empleado,
    nombre,
    apellido,
    cedula,
    cargo,
    email,
    telefono
)
SELECT
    d.id,
    'EMP-001',
    'Carlos',
    'Pérez',
    '001-0000001-1',
    'Chofer',
    'carlos.perez@test.local',
    '809-555-0101'
FROM departamento d
WHERE d.codigo = 'DEP-001';


 
-- 5. VEHICULO DE PRUEBA
 

INSERT INTO vehiculo (
    departamento_id,
    tipo_combustible_id,
    placa,
    ficha,
    marca,
    modelo,
    anio,
    tipo_vehiculo,
    capacidad_tanque,
    odometro_actual
)
SELECT
    d.id,
    tc.id,
    'TEST001',
    'FICHA-001',
    'Toyota',
    'Hilux',
    2024,
    'Camioneta',
    80.00,
    12500.00
FROM departamento d
JOIN tipo_combustible tc
    ON tc.codigo = 'DIESEL'
WHERE d.codigo = 'DEP-001';


 
-- 6. USUARIO SUPERVISOR DE PRUEBA
 
-- El password_hash es ficticio porque todavía no estamos
-- probando autenticación real.
 

INSERT INTO usuario (
    rol_id,
    empleado_id,
    nombre_usuario,
    email,
    password_hash
)
SELECT
    r.id,
    e.id,
    'supervisor.test',
    'supervisor@test.local',
    'HASH_PRUEBA_NO_VALIDO'
FROM rol r
JOIN empleado e
    ON e.codigo_empleado = 'EMP-001'
WHERE r.nombre = 'SUPERVISOR';


 
-- 7. SOLICITUD APROBADA DE PRUEBA
 

INSERT INTO solicitud (
    empleado_id,
    vehiculo_id,
    departamento_id,
    creada_por_usuario_id,
    revisada_por_usuario_id,
    tipo_solicitud,
    cantidad_solicitada,
    cantidad_autorizada,
    estado,
    fecha_solicitud,
    fecha_expiracion,
    fecha_revision,
    observaciones
)
SELECT
    emp.id,
    v.id,
    d.id,
    u.id,
    u.id,
    'MANUAL',
    50.00,
    50.00,
    'APROBADA',
    NOW(),
    NOW() + INTERVAL '2 days',
    NOW(),
    'Solicitud de prueba para validar reserva de inventario.'
FROM empleado emp
JOIN departamento d
    ON d.id = emp.departamento_id
JOIN vehiculo v
    ON v.departamento_id = d.id
JOIN usuario u
    ON u.empleado_id = emp.id
WHERE emp.codigo_empleado = 'EMP-001'
  AND v.ficha = 'FICHA-001';


 
-- 8. TICKET ACTIVO DE PRUEBA
 

INSERT INTO ticket (
    solicitud_id,
    estacion_id,
    tipo_combustible_id,
    numero_ticket,
    cantidad_autorizada,
    estado,
    token_qr_hash,
    firma_qr,
    fecha_emision,
    fecha_expiracion
)
SELECT
    s.id,
    est.id,
    tc.id,
    'COM-2026-000001',
    s.cantidad_autorizada,
    'CREADO',
    'HASH_QR_PRUEBA_000001',
    'FIRMA_QR_PRUEBA_000001',
    NOW(),
    s.fecha_expiracion
FROM solicitud s
JOIN estacion est
    ON est.codigo = 'EST-001'
JOIN tipo_combustible tc
    ON tc.codigo = 'DIESEL'
WHERE s.estado = 'APROBADA'
  AND s.observaciones = 'Solicitud de prueba para validar reserva de inventario.';



INSERT INTO usuario (
    rol_id,
    estacion_id,
    nombre_usuario,
    email,
    password_hash
)
SELECT
    r.id,
    e.id,
    'despachador.test',
    'despachador@test.local',
    'HASH_PRUEBA_NO_VALIDO'
FROM rol r
CROSS JOIN estacion e
WHERE r.nombre = 'DESPACHADOR'
  AND e.codigo = 'EST-001';

-- =========================================================
-- SINCRONIZAR SECUENCIA DE TICKETS DE PRUEBA
-- =========================================================

INSERT INTO secuencia_ticket_anual (
    anio,
    ultimo_numero,
    fecha_actualizacion
)
VALUES (
    EXTRACT(YEAR FROM CURRENT_DATE)::SMALLINT,
    1,
    NOW()
)
ON CONFLICT (anio)
DO UPDATE SET
    ultimo_numero = GREATEST(
        secuencia_ticket_anual.ultimo_numero,
        EXCLUDED.ultimo_numero
    ),
    fecha_actualizacion = NOW();
COMMIT;