

BEGIN;

 
-- 1. VISTA: vw_tickets_operativos
 
-- Presenta información consolidada del ticket junto con
-- solicitud, empleado, vehículo, estación y combustible.
--
-- Además calcula un "estado efectivo":
-- - ANULADO
-- - CONSUMIDO
-- - VENCIDO
-- - PROXIMO_A_VENCER
-- - CREADO / ENVIADO
--
-- VENCIDO y PROXIMO_A_VENCER son estados derivados,
-- no almacenados físicamente en la tabla ticket.
 

CREATE OR REPLACE VIEW vw_tickets_operativos AS
SELECT
    tk.id AS ticket_id,
    tk.numero_ticket,
    tk.solicitud_id,

    s.empleado_id,
    emp.codigo_empleado,
    emp.nombre AS empleado_nombre,
    emp.apellido AS empleado_apellido,

    s.vehiculo_id,
    v.placa,
    v.ficha,
    v.marca,
    v.modelo,

    s.departamento_id,
    d.codigo AS departamento_codigo,
    d.nombre AS departamento_nombre,

    tk.estacion_id,
    est.codigo AS estacion_codigo,
    est.nombre AS estacion_nombre,

    tk.tipo_combustible_id,
    tc.codigo AS combustible_codigo,
    tc.nombre AS combustible_nombre,

    tk.cantidad_autorizada,

    tk.estado AS estado_almacenado,

    CASE
        WHEN tk.estado = 'ANULADO'
            THEN 'ANULADO'

        WHEN tk.estado = 'CONSUMIDO'
            THEN 'CONSUMIDO'

        WHEN tk.fecha_expiracion <= NOW()
            THEN 'VENCIDO'

        WHEN tk.fecha_expiracion <= NOW() + INTERVAL '2 days'
            THEN 'PROXIMO_A_VENCER'

        ELSE tk.estado
    END AS estado_efectivo,

    tk.fecha_emision,
    tk.fecha_envio,
    tk.fecha_expiracion,
    tk.fecha_anulacion,

    tk.anulado_por_usuario_id,
    tk.motivo_anulacion

FROM ticket tk

INNER JOIN solicitud s
    ON s.id = tk.solicitud_id

INNER JOIN empleado emp
    ON emp.id = s.empleado_id

INNER JOIN vehiculo v
    ON v.id = s.vehiculo_id

INNER JOIN departamento d
    ON d.id = s.departamento_id

INNER JOIN estacion est
    ON est.id = tk.estacion_id

INNER JOIN tipo_combustible tc
    ON tc.id = tk.tipo_combustible_id;


 
-- 2. VISTA: vw_movimientos_tanque
 
-- Presenta el historial consolidado de movimientos
-- de inventario por tanque.
--
-- Permite consultar:
-- estación
-- combustible
-- tanque
-- tipo de movimiento
-- cantidad
-- saldo anterior
-- saldo posterior
-- usuario
-- fecha
-- origen transaccional
 

CREATE OR REPLACE VIEW vw_movimientos_tanque AS
SELECT
    mi.id AS movimiento_id,

    mi.tanque_id,
    t.codigo AS tanque_codigo,
    t.nombre AS tanque_nombre,

    t.estacion_id,
    est.codigo AS estacion_codigo,
    est.nombre AS estacion_nombre,

    t.tipo_combustible_id,
    tc.codigo AS combustible_codigo,
    tc.nombre AS combustible_nombre,

    mi.tipo_movimiento,
    mi.cantidad,
    mi.saldo_anterior,
    mi.saldo_posterior,

    mi.registrado_por_usuario_id,
    u.nombre_usuario AS registrado_por,

    mi.recepcion_id,
    mi.despacho_id,
    mi.transferencia_id,
    mi.ajuste_id,

    mi.fecha_movimiento,
    mi.observaciones

FROM movimiento_inventario mi

INNER JOIN tanque t
    ON t.id = mi.tanque_id

INNER JOIN estacion est
    ON est.id = t.estacion_id

INNER JOIN tipo_combustible tc
    ON tc.id = t.tipo_combustible_id

INNER JOIN usuario u
    ON u.id = mi.registrado_por_usuario_id;


 
-- 3. VISTA: vw_cierres_resumen
 
-- Consolida la información de cierres diarios
-- junto con tanque, estación, combustible
-- y usuarios responsables.
 

CREATE OR REPLACE VIEW vw_cierres_resumen AS
SELECT
    c.id AS cierre_id,
    c.fecha_cierre,
    c.estado,

    c.tanque_id,
    t.codigo AS tanque_codigo,
    t.nombre AS tanque_nombre,

    t.estacion_id,
    est.codigo AS estacion_codigo,
    est.nombre AS estacion_nombre,

    t.tipo_combustible_id,
    tc.codigo AS combustible_codigo,
    tc.nombre AS combustible_nombre,

    c.stock_inicial,
    c.total_recepciones,
    c.total_transferencias_entrada,
    c.total_transferencias_salida,
    c.total_despachos,
    c.total_ajustes_positivos,
    c.total_ajustes_negativos,
    c.stock_teorico_final,
    c.stock_fisico_final,
    c.diferencia,

    c.creado_por_usuario_id,
    uc.nombre_usuario AS creado_por,

    c.revisado_por_usuario_id,
    ur.nombre_usuario AS revisado_por,

    c.fecha_creacion,
    c.fecha_revision,

    c.motivo_diferencia,
    c.motivo_rechazo,
    c.observaciones

FROM cierre_diario c

INNER JOIN tanque t
    ON t.id = c.tanque_id

INNER JOIN estacion est
    ON est.id = t.estacion_id

INNER JOIN tipo_combustible tc
    ON tc.id = t.tipo_combustible_id

INNER JOIN usuario uc
    ON uc.id = c.creado_por_usuario_id

LEFT JOIN usuario ur
    ON ur.id = c.revisado_por_usuario_id;

COMMIT;