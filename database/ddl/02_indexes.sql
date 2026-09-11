

BEGIN;

 
-- 1. EMPLEADO
 

CREATE INDEX idx_empleado_departamento
    ON empleado (departamento_id);


 
-- 2. VEHICULO
 

CREATE INDEX idx_vehiculo_departamento
    ON vehiculo (departamento_id);

CREATE INDEX idx_vehiculo_tipo_combustible
    ON vehiculo (tipo_combustible_id);


 
-- 3. TANQUE
 

CREATE INDEX idx_tanque_estacion
    ON tanque (estacion_id);

CREATE INDEX idx_tanque_tipo_combustible
    ON tanque (tipo_combustible_id);

CREATE INDEX idx_tanque_estacion_combustible
    ON tanque (estacion_id, tipo_combustible_id)
    WHERE activo = TRUE;


 
-- 4. USUARIO
 

CREATE INDEX idx_usuario_rol
    ON usuario (rol_id);

CREATE INDEX idx_usuario_estacion
    ON usuario (estacion_id);

-- empleado_id ya tiene UNIQUE, por lo que ya posee índice.


 
-- 5. REFRESH TOKEN
 

CREATE INDEX idx_refresh_token_usuario
    ON refresh_token (usuario_id);

CREATE INDEX idx_refresh_token_expiracion
    ON refresh_token (fecha_expiracion);


 
-- 6. SOLICITUD
 

CREATE INDEX idx_solicitud_empleado
    ON solicitud (empleado_id);

CREATE INDEX idx_solicitud_vehiculo
    ON solicitud (vehiculo_id);

CREATE INDEX idx_solicitud_departamento
    ON solicitud (departamento_id);

CREATE INDEX idx_solicitud_creada_por
    ON solicitud (creada_por_usuario_id);

CREATE INDEX idx_solicitud_revisada_por
    ON solicitud (revisada_por_usuario_id);

CREATE INDEX idx_solicitud_estado
    ON solicitud (estado);

CREATE INDEX idx_solicitud_fecha
    ON solicitud (fecha_solicitud);

CREATE INDEX idx_solicitud_pendiente
    ON solicitud (fecha_solicitud)
    WHERE estado = 'PENDIENTE';


 
-- 7. TICKET
 

CREATE INDEX idx_ticket_estacion
    ON ticket (estacion_id);

CREATE INDEX idx_ticket_tipo_combustible
    ON ticket (tipo_combustible_id);

CREATE INDEX idx_ticket_anulado_por
    ON ticket (anulado_por_usuario_id);

CREATE INDEX idx_ticket_estado
    ON ticket (estado);

CREATE INDEX idx_ticket_fecha_expiracion
    ON ticket (fecha_expiracion);

CREATE INDEX idx_ticket_estacion_combustible
    ON ticket (
        estacion_id,
        tipo_combustible_id
    );

-- Muy importante para el cálculo de combustible reservado.
CREATE INDEX idx_ticket_reserva_activa
    ON ticket (
        estacion_id,
        tipo_combustible_id,
        fecha_expiracion
    )
    WHERE estado IN ('CREADO', 'ENVIADO');


 
-- 8. DESPACHO
 

CREATE INDEX idx_despacho_usuario
    ON despacho (despachador_usuario_id);

CREATE INDEX idx_despacho_tanque
    ON despacho (tanque_id);

CREATE INDEX idx_despacho_fecha
    ON despacho (fecha_despacho);

-- ticket_id ya tiene UNIQUE.


 
-- 9. RECEPCION COMBUSTIBLE
 

CREATE INDEX idx_recepcion_proveedor
    ON recepcion_combustible (proveedor_id);

CREATE INDEX idx_recepcion_tanque
    ON recepcion_combustible (tanque_id);

CREATE INDEX idx_recepcion_usuario
    ON recepcion_combustible (registrada_por_usuario_id);

CREATE INDEX idx_recepcion_fecha
    ON recepcion_combustible (fecha_recepcion);


 
-- 10. TRANSFERENCIA INVENTARIO
 

CREATE INDEX idx_transferencia_origen
    ON transferencia_inventario (tanque_origen_id);

CREATE INDEX idx_transferencia_destino
    ON transferencia_inventario (tanque_destino_id);

CREATE INDEX idx_transferencia_usuario
    ON transferencia_inventario (registrada_por_usuario_id);

CREATE INDEX idx_transferencia_fecha
    ON transferencia_inventario (fecha_transferencia);


 
-- 11. AJUSTE INVENTARIO
 

CREATE INDEX idx_ajuste_tanque
    ON ajuste_inventario (tanque_id);

CREATE INDEX idx_ajuste_reportado_por
    ON ajuste_inventario (reportado_por_usuario_id);

CREATE INDEX idx_ajuste_revisado_por
    ON ajuste_inventario (revisado_por_usuario_id);

CREATE INDEX idx_ajuste_estado
    ON ajuste_inventario (estado);

CREATE INDEX idx_ajuste_fecha
    ON ajuste_inventario (fecha_reporte);

CREATE INDEX idx_ajuste_pendiente
    ON ajuste_inventario (fecha_reporte)
    WHERE estado = 'PENDIENTE';


 
-- 12. MOVIMIENTO INVENTARIO
 

CREATE INDEX idx_movimiento_tanque
    ON movimiento_inventario (tanque_id);

CREATE INDEX idx_movimiento_usuario
    ON movimiento_inventario (registrado_por_usuario_id);

CREATE INDEX idx_movimiento_fecha
    ON movimiento_inventario (fecha_movimiento);

CREATE INDEX idx_movimiento_tipo
    ON movimiento_inventario (tipo_movimiento);

-- Muy utilizado para historial y cierres.
CREATE INDEX idx_movimiento_tanque_fecha
    ON movimiento_inventario (
        tanque_id,
        fecha_movimiento
    );

-- recepcion_id, despacho_id y ajuste_id ya tienen UNIQUE.


CREATE INDEX idx_movimiento_transferencia
    ON movimiento_inventario (transferencia_id);


 
-- 13. CIERRE DIARIO
 

CREATE INDEX idx_cierre_tanque
    ON cierre_diario (tanque_id);

CREATE INDEX idx_cierre_creado_por
    ON cierre_diario (creado_por_usuario_id);

CREATE INDEX idx_cierre_revisado_por
    ON cierre_diario (revisado_por_usuario_id);

CREATE INDEX idx_cierre_fecha
    ON cierre_diario (fecha_cierre);

CREATE INDEX idx_cierre_estado
    ON cierre_diario (estado);


-- Solo puede existir un cierre NO rechazado
-- para un mismo tanque y fecha.
CREATE UNIQUE INDEX uq_cierre_tanque_fecha_activo
    ON cierre_diario (
        tanque_id,
        fecha_cierre
    )
    WHERE estado <> 'RECHAZADO';


 
-- 14. NOTIFICACION
 

CREATE INDEX idx_notificacion_usuario
    ON notificacion (usuario_id);

CREATE INDEX idx_notificacion_fecha
    ON notificacion (fecha_creacion);

CREATE INDEX idx_notificacion_usuario_no_leida
    ON notificacion (
        usuario_id,
        fecha_creacion
    )
    WHERE leida = FALSE;


 
-- 15. AUDIT LOG
 

CREATE INDEX idx_audit_usuario
    ON audit_log (usuario_id);

CREATE INDEX idx_audit_fecha
    ON audit_log (fecha_hora);

CREATE INDEX idx_audit_entidad
    ON audit_log (entidad, entidad_id);

CREATE INDEX idx_audit_accion
    ON audit_log (accion);


 
-- 16. CONFIGURACION SISTEMA
 

CREATE INDEX idx_configuracion_actualizado_por
    ON configuracion_sistema (actualizado_por_usuario_id);


COMMIT;