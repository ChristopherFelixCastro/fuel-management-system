-- Ejecutar exclusivamente sobre una base de pruebas inicializada con los DDL,
-- funciones y datos de prueba. Todo se revierte al finalizar.
BEGIN;

INSERT INTO proveedor(rnc,nombre) VALUES ('TEST-INV-2026','Proveedor temporal');

INSERT INTO estacion(codigo,nombre) VALUES ('EST-TMP-TRANSFER','Destino temporal');
INSERT INTO tanque(estacion_id,tipo_combustible_id,codigo,nombre,capacidad_maxima,stock_actual,nivel_critico)
SELECT e.id,tc.id,'TNQ-TMP-DEST','Destino temporal',2000,0,10 FROM estacion e CROSS JOIN tipo_combustible tc WHERE e.codigo='EST-TMP-TRANSFER' AND tc.codigo='DIESEL';
UPDATE tanque SET stock_actual=800 WHERE codigo='TNQ-DIESEL-001';
UPDATE tanque SET stock_actual=0 WHERE codigo='TNQ-DIESEL-002';

DO $$
DECLARE v_user UUID; v_origin UUID; v_dest UUID; v_station UUID; v_fuel SMALLINT; v_available NUMERIC;
BEGIN
 SELECT id INTO v_user FROM usuario WHERE nombre_usuario='supervisor.test';
 SELECT id,estacion_id,tipo_combustible_id INTO v_origin,v_station,v_fuel FROM tanque WHERE codigo='TNQ-DIESEL-001';
 SELECT id INTO v_dest FROM tanque WHERE codigo='TNQ-TMP-DEST';
 SELECT stock_disponible INTO v_available FROM fn_obtener_stock_disponible(v_station,v_fuel);
 BEGIN
   PERFORM fn_registrar_transferencia(v_origin,v_dest,v_user,v_available+1,NULL);
   RAISE EXCEPTION 'TEST_FAILED_TRANSFERENCIA_DEBIO_RECHAZARSE';
 EXCEPTION WHEN OTHERS THEN
   IF SQLERRM NOT LIKE '%STOCK_DISPONIBLE_INSUFICIENTE_POR_RESERVAS%' THEN RAISE; END IF;
 END;
END $$;

DO $$
DECLARE v_user UUID; v_tank UUID; v_supplier UUID; v_receipt UUID; v_before NUMERIC; v_after NUMERIC;
BEGIN
 SELECT id INTO v_user FROM usuario WHERE nombre_usuario='supervisor.test';
 SELECT id,stock_actual INTO v_tank,v_before FROM tanque WHERE codigo='TNQ-DIESEL-002';
 SELECT id INTO v_supplier FROM proveedor WHERE rnc='TEST-INV-2026';
 v_receipt:=fn_registrar_recepcion(v_supplier,v_tank,v_user,'FAC-TEST-001',50,NOW(),NULL);
 SELECT stock_actual INTO v_after FROM tanque WHERE id=v_tank;
 IF v_after<>v_before+50 THEN RAISE EXCEPTION 'TEST_FAILED_RECEPCION_STOCK'; END IF;
 IF NOT EXISTS(SELECT 1 FROM movimiento_inventario WHERE recepcion_id=v_receipt AND tipo_movimiento='RECEPCION') THEN RAISE EXCEPTION 'TEST_FAILED_RECEPCION_MOVIMIENTO'; END IF;
 IF NOT EXISTS(SELECT 1 FROM audit_log WHERE entidad_id=v_receipt AND accion='REGISTRAR_RECEPCION') THEN RAISE EXCEPTION 'TEST_FAILED_RECEPCION_AUDITORIA'; END IF;
 BEGIN
   PERFORM fn_registrar_recepcion(v_supplier,v_tank,v_user,'FAC-TEST-CAP',10000,NOW(),NULL);
   RAISE EXCEPTION 'TEST_FAILED_CAPACIDAD_DEBIO_RECHAZARSE';
 EXCEPTION WHEN OTHERS THEN IF SQLERRM NOT LIKE '%CAPACIDAD_TANQUE_EXCEDIDA%' THEN RAISE; END IF; END;
END $$;

DO $$
DECLARE v_user UUID; v_tank UUID; v_adjustment UUID; v_before NUMERIC; v_after_report NUMERIC; v_after_approval NUMERIC;
BEGIN
 SELECT id INTO v_user FROM usuario WHERE nombre_usuario='despachador.test';
 SELECT id,stock_actual INTO v_tank,v_before FROM tanque WHERE codigo='TNQ-DIESEL-001';
 v_adjustment:=fn_reportar_ajuste(v_tank,v_user,v_before-25,'Conteo de prueba',NULL);
 SELECT stock_actual INTO v_after_report FROM tanque WHERE id=v_tank;
 IF v_after_report<>v_before THEN RAISE EXCEPTION 'TEST_FAILED_REPORTE_MODIFICO_STOCK'; END IF;
 PERFORM fn_aprobar_ajuste(v_adjustment,(SELECT id FROM usuario WHERE nombre_usuario='supervisor.test'));
 SELECT stock_actual INTO v_after_approval FROM tanque WHERE id=v_tank;
 IF v_after_approval<>v_before-25 THEN RAISE EXCEPTION 'TEST_FAILED_APROBACION_NO_USO_CONTEO'; END IF;
 IF NOT EXISTS(SELECT 1 FROM movimiento_inventario WHERE ajuste_id=v_adjustment AND tipo_movimiento='AJUSTE_NEGATIVO') THEN RAISE EXCEPTION 'TEST_FAILED_MOVIMIENTO_AJUSTE'; END IF;
END $$;

DO $$
DECLARE v_dispatcher UUID; v_supervisor UUID; v_tank UUID; v_adjustment UUID; v_before NUMERIC;
BEGIN
 SELECT id INTO v_dispatcher FROM usuario WHERE nombre_usuario='despachador.test';
 SELECT id INTO v_supervisor FROM usuario WHERE nombre_usuario='supervisor.test';
 SELECT id,stock_actual INTO v_tank,v_before FROM tanque WHERE codigo='TNQ-DIESEL-001';
 v_adjustment:=fn_reportar_ajuste(v_tank,v_dispatcher,v_before+10,'Ajuste a rechazar',NULL);
 PERFORM fn_rechazar_ajuste(v_adjustment,v_supervisor,'Conteo no confirmado');
 IF (SELECT estado FROM ajuste_inventario WHERE id=v_adjustment)<>'RECHAZADO' THEN RAISE EXCEPTION 'TEST_FAILED_RECHAZO'; END IF;
 IF (SELECT stock_actual FROM tanque WHERE id=v_tank)<>v_before THEN RAISE EXCEPTION 'TEST_FAILED_RECHAZO_MODIFICO_STOCK'; END IF;
END $$;

-- Si el stock cambia hasta coincidir con el conteo antes de aprobar, la
-- aprobación debe registrar diferencia cero y no crear un movimiento ficticio.
DO $$
DECLARE v_dispatcher UUID; v_supervisor UUID; v_tank UUID; v_adjustment UUID; v_target NUMERIC;
BEGIN
 SELECT id INTO v_dispatcher FROM usuario WHERE nombre_usuario='despachador.test';
 SELECT id INTO v_supervisor FROM usuario WHERE nombre_usuario='supervisor.test';
 SELECT id,stock_actual-5 INTO v_tank,v_target FROM tanque WHERE codigo='TNQ-DIESEL-001';
 v_adjustment:=fn_reportar_ajuste(v_tank,v_dispatcher,v_target,'Conteo que converge antes de aprobar',NULL);

 -- Simula otra operación confirmada entre reporte y aprobación.
 UPDATE tanque SET stock_actual=v_target WHERE id=v_tank;
 PERFORM fn_aprobar_ajuste(v_adjustment,v_supervisor);

 IF (SELECT estado FROM ajuste_inventario WHERE id=v_adjustment)<>'APROBADO' THEN
   RAISE EXCEPTION 'TEST_FAILED_AJUSTE_CERO_ESTADO';
 END IF;
 IF (SELECT cantidad FROM ajuste_inventario WHERE id=v_adjustment)<>0 THEN
   RAISE EXCEPTION 'TEST_FAILED_AJUSTE_CERO_CANTIDAD';
 END IF;
 IF EXISTS(SELECT 1 FROM movimiento_inventario WHERE ajuste_id=v_adjustment) THEN
   RAISE EXCEPTION 'TEST_FAILED_AJUSTE_CERO_CREO_MOVIMIENTO';
 END IF;
END $$;

DO $$
DECLARE v_supervisor UUID; v_dispatcher UUID; v_tank UUID; v_supplier UUID; v_stock NUMERIC;
        v_ticket UUID; v_ticket_amount NUMERIC; v_odometer NUMERIC;
BEGIN
 SELECT id INTO v_supervisor FROM usuario WHERE nombre_usuario='supervisor.test';
 SELECT id INTO v_dispatcher FROM usuario WHERE nombre_usuario='despachador.test';
 SELECT id,stock_actual INTO v_tank,v_stock FROM tanque WHERE codigo='TNQ-DIESEL-001';
 SELECT id INTO v_supplier FROM proveedor WHERE rnc='TEST-INV-2026';
 INSERT INTO cierre_diario(tanque_id,creado_por_usuario_id,revisado_por_usuario_id,fecha_cierre,stock_inicial,stock_teorico_final,stock_fisico_final,diferencia,estado,fecha_revision)
 VALUES(v_tank,v_dispatcher,v_supervisor,CURRENT_DATE,v_stock,v_stock,v_stock,0,'APROBADO',NOW());
 BEGIN
   PERFORM fn_registrar_recepcion(v_supplier,v_tank,v_supervisor,'FAC-TEST-CLOSED',1,NOW(),NULL);
   RAISE EXCEPTION 'TEST_FAILED_CIERRE_DEBIO_BLOQUEAR';
 EXCEPTION WHEN OTHERS THEN IF SQLERRM NOT LIKE '%CIERRE_APROBADO_INMUTABLE%' THEN RAISE; END IF; END;

 SELECT tk.id,tk.cantidad_autorizada,v.odometro_actual
 INTO v_ticket,v_ticket_amount,v_odometer
 FROM ticket tk
 JOIN solicitud s ON s.id=tk.solicitud_id
 JOIN vehiculo v ON v.id=s.vehiculo_id
 WHERE tk.estado IN ('CREADO','ENVIADO')
   AND tk.fecha_expiracion>NOW()
   AND tk.estacion_id=(SELECT estacion_id FROM tanque WHERE id=v_tank)
   AND tk.tipo_combustible_id=(SELECT tipo_combustible_id FROM tanque WHERE id=v_tank)
 LIMIT 1;

 BEGIN
   PERFORM fn_registrar_despacho(v_ticket,v_dispatcher,v_tank,v_ticket_amount,v_odometer,NULL,NULL);
   RAISE EXCEPTION 'TEST_FAILED_CIERRE_DEBIO_BLOQUEAR_DESPACHO';
 EXCEPTION WHEN OTHERS THEN IF SQLERRM NOT LIKE '%CIERRE_APROBADO_INMUTABLE%' THEN RAISE; END IF; END;
END $$;

ROLLBACK;
