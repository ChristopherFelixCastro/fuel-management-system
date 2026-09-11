 
-- Proyecto: Fuel Management System
-- Archivo: 04_integrity_triggers.sql
-- Descripción: Triggers de protección de registros históricos
 

BEGIN;

 
-- AUDIT LOG
 

CREATE TRIGGER trg_audit_log_no_update
BEFORE UPDATE ON audit_log
FOR EACH ROW
EXECUTE FUNCTION fn_bloquear_modificacion_historica();

CREATE TRIGGER trg_audit_log_no_delete
BEFORE DELETE ON audit_log
FOR EACH ROW
EXECUTE FUNCTION fn_bloquear_modificacion_historica();


 
-- MOVIMIENTO INVENTARIO
 

CREATE TRIGGER trg_movimiento_inventario_no_update
BEFORE UPDATE ON movimiento_inventario
FOR EACH ROW
EXECUTE FUNCTION fn_bloquear_modificacion_historica();

CREATE TRIGGER trg_movimiento_inventario_no_delete
BEFORE DELETE ON movimiento_inventario
FOR EACH ROW
EXECUTE FUNCTION fn_bloquear_modificacion_historica();




CREATE TRIGGER trg_cierre_aprobado_no_update
BEFORE UPDATE ON cierre_diario
FOR EACH ROW
EXECUTE FUNCTION fn_proteger_cierre_aprobado();


CREATE TRIGGER trg_cierre_aprobado_no_delete
BEFORE DELETE ON cierre_diario
FOR EACH ROW
EXECUTE FUNCTION fn_proteger_cierre_aprobado();
COMMIT;