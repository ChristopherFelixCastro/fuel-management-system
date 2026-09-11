
BEGIN;

 
-- 1. ROLES DEL SISTEMA
 

INSERT INTO rol (
    nombre,
    descripcion
)
VALUES
    (
        'ADMINISTRADOR',
        'Administración general del sistema, usuarios y configuración.'
    ),
    (
        'SUPERVISOR',
        'Supervisión de combustible, aprobación de solicitudes, ajustes y cierres.'
    ),
    (
        'DESPACHADOR',
        'Validación de tickets y registro de despachos de combustible.'
    ),
    (
        'SOLICITANTE',
        'Creación y seguimiento de solicitudes de combustible.'
    ),
    (
        'AUDITOR',
        'Consulta de trazabilidad, auditoría y reportes del sistema.'
    );


 
-- 2. TIPOS DE COMBUSTIBLE
 

INSERT INTO tipo_combustible (
    codigo,
    nombre,
    descripcion
)
VALUES
    (
        'GASOLINA',
        'Gasolina',
        'Combustible tipo gasolina.'
    ),
    (
        'DIESEL',
        'Diésel',
        'Combustible tipo diésel.'
    );


 
-- 3. CONFIGURACIONES INICIALES DEL SISTEMA
 

INSERT INTO configuracion_sistema (
    clave,
    valor,
    tipo_dato,
    descripcion,
    editable
)
VALUES
    (
        'PREFIJO_TICKET',
        'COM',
        'STRING',
        'Prefijo utilizado para generar el número secuencial de los tickets.',
        TRUE
    ),
    (
        'DIAS_ALERTA_VENCIMIENTO',
        '2',
        'INTEGER',
        'Cantidad de días previos al vencimiento para generar una alerta.',
        TRUE
    ),
    (
        'REINICIO_ANUAL_NUMERACION',
        'true',
        'BOOLEAN',
        'Indica si la numeración de tickets se reinicia al iniciar un nuevo año.',
        TRUE
    );

COMMIT;