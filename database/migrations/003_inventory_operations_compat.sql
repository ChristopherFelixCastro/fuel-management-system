-- =========================================================
-- Proyecto: Fuel Management System
-- Archivo: 05_inventory_operations.sql
-- Descripción:
--   Operaciones atómicas de inventario:
--   - recepción
--   - transferencia
--   - aprobación/rechazo de ajustes
-- =========================================================

BEGIN;

-- =========================================================
-- 1. REGISTRAR RECEPCIÓN DE COMBUSTIBLE
-- =========================================================

CREATE OR REPLACE FUNCTION fn_registrar_recepcion(
    p_proveedor_id UUID,
    p_tanque_id UUID,
    p_usuario_id UUID,
    p_numero_factura VARCHAR(50),
    p_cantidad NUMERIC(12,2),
    p_fecha_recepcion TIMESTAMPTZ,
    p_observaciones VARCHAR(500) DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_recepcion_id UUID;
    v_stock_actual NUMERIC(12,2);
    v_capacidad NUMERIC(12,2);
    v_stock_nuevo NUMERIC(12,2);
    v_tanque_activo BOOLEAN;
BEGIN

    IF p_cantidad IS NULL OR p_cantidad <= 0 THEN
        RAISE EXCEPTION 'CANTIDAD_RECEPCION_INVALIDA';
    END IF;

    SELECT
        stock_actual,
        capacidad_maxima,
        activo
    INTO
        v_stock_actual,
        v_capacidad,
        v_tanque_activo
    FROM tanque
    WHERE id = p_tanque_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'TANQUE_INEXISTENTE';
    END IF;

    IF v_tanque_activo = FALSE THEN
        RAISE EXCEPTION 'TANQUE_INACTIVO';
    END IF;

    IF EXISTS (SELECT 1 FROM cierre_diario WHERE tanque_id=p_tanque_id AND fecha_cierre=p_fecha_recepcion::DATE AND estado='APROBADO') THEN
        RAISE EXCEPTION 'CIERRE_APROBADO_INMUTABLE';
    END IF;

    v_stock_nuevo := v_stock_actual + p_cantidad;

    IF v_stock_nuevo > v_capacidad THEN
        RAISE EXCEPTION 'CAPACIDAD_TANQUE_EXCEDIDA';
    END IF;

    INSERT INTO recepcion_combustible (
        proveedor_id,
        tanque_id,
        registrada_por_usuario_id,
        numero_factura,
        cantidad_recibida,
        fecha_recepcion,
        observaciones
    )
    VALUES (
        p_proveedor_id,
        p_tanque_id,
        p_usuario_id,
        p_numero_factura,
        p_cantidad,
        p_fecha_recepcion,
        p_observaciones
    )
    RETURNING id INTO v_recepcion_id;

    UPDATE tanque
    SET
        stock_actual = v_stock_nuevo,
        fecha_actualizacion = NOW()
    WHERE id = p_tanque_id;

    INSERT INTO movimiento_inventario (
        tanque_id,
        registrado_por_usuario_id,
        tipo_movimiento,
        cantidad,
        saldo_anterior,
        saldo_posterior,
        recepcion_id,
        fecha_movimiento,
        observaciones
    )
    VALUES (
        p_tanque_id,
        p_usuario_id,
        'RECEPCION',
        p_cantidad,
        v_stock_actual,
        v_stock_nuevo,
        v_recepcion_id,
        NOW(),
        p_observaciones
    );

    PERFORM fn_registrar_auditoria(p_usuario_id,'REGISTRAR_RECEPCION','recepcion_combustible',v_recepcion_id,NULL,jsonb_build_object('tanqueId',p_tanque_id,'cantidad',p_cantidad));

    RETURN v_recepcion_id;

END;
$$;


-- =========================================================
-- 2. REGISTRAR TRANSFERENCIA ENTRE TANQUES
-- =========================================================

CREATE OR REPLACE FUNCTION fn_registrar_transferencia(
    p_tanque_origen_id UUID,
    p_tanque_destino_id UUID,
    p_usuario_id UUID,
    p_cantidad NUMERIC(12,2),
    p_observaciones VARCHAR(500) DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_transferencia_id UUID;

    v_stock_origen NUMERIC(12,2);
    v_stock_destino NUMERIC(12,2);

    v_capacidad_destino NUMERIC(12,2);

    v_combustible_origen SMALLINT;
    v_combustible_destino SMALLINT;

    v_origen_activo BOOLEAN;
    v_destino_activo BOOLEAN;
    v_estacion_origen_id UUID;
    v_estacion_destino_id UUID;
    v_stock_reservado_origen NUMERIC(12,2);
    v_stock_disponible_origen NUMERIC(12,2);

    v_nuevo_origen NUMERIC(12,2);
    v_nuevo_destino NUMERIC(12,2);
BEGIN

    IF p_tanque_origen_id = p_tanque_destino_id THEN
        RAISE EXCEPTION 'TANQUES_ORIGEN_DESTINO_IGUALES';
    END IF;

    IF p_cantidad IS NULL OR p_cantidad <= 0 THEN
        RAISE EXCEPTION 'CANTIDAD_TRANSFERENCIA_INVALIDA';
    END IF;

    -- Bloqueo determinístico para reducir riesgo de deadlocks.
    PERFORM id
    FROM tanque
    WHERE id IN (p_tanque_origen_id, p_tanque_destino_id)
    ORDER BY id
    FOR UPDATE;

    SELECT
        stock_actual,
        tipo_combustible_id,
        activo,
        estacion_id
    INTO
        v_stock_origen,
        v_combustible_origen,
        v_origen_activo,
        v_estacion_origen_id
    FROM tanque
    WHERE id = p_tanque_origen_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'TANQUE_ORIGEN_INEXISTENTE';
    END IF;

    SELECT
        stock_actual,
        capacidad_maxima,
        tipo_combustible_id,
        activo,
        estacion_id
    INTO
        v_stock_destino,
        v_capacidad_destino,
        v_combustible_destino,
        v_destino_activo,
        v_estacion_destino_id
    FROM tanque
    WHERE id = p_tanque_destino_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'TANQUE_DESTINO_INEXISTENTE';
    END IF;

    IF v_origen_activo = FALSE OR v_destino_activo = FALSE THEN
        RAISE EXCEPTION 'TANQUE_INACTIVO';
    END IF;

    IF EXISTS (SELECT 1 FROM cierre_diario WHERE tanque_id IN (p_tanque_origen_id,p_tanque_destino_id) AND fecha_cierre=CURRENT_DATE AND estado='APROBADO') THEN
        RAISE EXCEPTION 'CIERRE_APROBADO_INMUTABLE';
    END IF;

    IF v_combustible_origen <> v_combustible_destino THEN
        RAISE EXCEPTION 'COMBUSTIBLE_TRANSFERENCIA_INCOMPATIBLE';
    END IF;

    IF v_stock_origen < p_cantidad THEN
        RAISE EXCEPTION 'STOCK_ORIGEN_INSUFICIENTE';
    END IF;

    -- Una transferencia entre estaciones no puede consumir combustible ya
    -- reservado por tickets vigentes de la estación de origen. Dentro de la
    -- misma estación el stock agregado no cambia, por lo que no afecta la
    -- disponibilidad reservada.
    IF v_estacion_origen_id <> v_estacion_destino_id THEN
        PERFORM pg_advisory_xact_lock(hashtextextended(v_estacion_origen_id::TEXT || ':' || v_combustible_origen::TEXT, 0));
        SELECT stock_reservado, stock_disponible
        INTO v_stock_reservado_origen, v_stock_disponible_origen
        FROM fn_obtener_stock_disponible(
            v_estacion_origen_id,
            v_combustible_origen
        );

        IF COALESCE(v_stock_disponible_origen, 0) < p_cantidad THEN
            RAISE EXCEPTION 'STOCK_DISPONIBLE_INSUFICIENTE_POR_RESERVAS';
        END IF;
    END IF;

    IF v_stock_destino + p_cantidad > v_capacidad_destino THEN
        RAISE EXCEPTION 'CAPACIDAD_DESTINO_EXCEDIDA';
    END IF;

    v_nuevo_origen := v_stock_origen - p_cantidad;
    v_nuevo_destino := v_stock_destino + p_cantidad;

    INSERT INTO transferencia_inventario (
        tanque_origen_id,
        tanque_destino_id,
        registrada_por_usuario_id,
        cantidad,
        observaciones
    )
    VALUES (
        p_tanque_origen_id,
        p_tanque_destino_id,
        p_usuario_id,
        p_cantidad,
        p_observaciones
    )
    RETURNING id INTO v_transferencia_id;

    UPDATE tanque
    SET
        stock_actual = v_nuevo_origen,
        fecha_actualizacion = NOW()
    WHERE id = p_tanque_origen_id;

    UPDATE tanque
    SET
        stock_actual = v_nuevo_destino,
        fecha_actualizacion = NOW()
    WHERE id = p_tanque_destino_id;

    INSERT INTO movimiento_inventario (
        tanque_id,
        registrado_por_usuario_id,
        tipo_movimiento,
        cantidad,
        saldo_anterior,
        saldo_posterior,
        transferencia_id,
        fecha_movimiento,
        observaciones
    )
    VALUES (
        p_tanque_origen_id,
        p_usuario_id,
        'TRANSFERENCIA_SALIDA',
        p_cantidad,
        v_stock_origen,
        v_nuevo_origen,
        v_transferencia_id,
        NOW(),
        p_observaciones
    );

    INSERT INTO movimiento_inventario (
        tanque_id,
        registrado_por_usuario_id,
        tipo_movimiento,
        cantidad,
        saldo_anterior,
        saldo_posterior,
        transferencia_id,
        fecha_movimiento,
        observaciones
    )
    VALUES (
        p_tanque_destino_id,
        p_usuario_id,
        'TRANSFERENCIA_ENTRADA',
        p_cantidad,
        v_stock_destino,
        v_nuevo_destino,
        v_transferencia_id,
        NOW(),
        p_observaciones
    );

    PERFORM fn_registrar_auditoria(p_usuario_id,'REGISTRAR_TRANSFERENCIA','transferencia_inventario',v_transferencia_id,NULL,jsonb_build_object('origenId',p_tanque_origen_id,'destinoId',p_tanque_destino_id,'cantidad',p_cantidad));

    RETURN v_transferencia_id;

END;
$$;


-- =========================================================
-- 3. REPORTAR AJUSTE PENDIENTE DESDE CONTEO FÍSICO
-- =========================================================

CREATE OR REPLACE FUNCTION fn_reportar_ajuste(
    p_tanque_id UUID,
    p_usuario_id UUID,
    p_conteo_fisico NUMERIC(12,2),
    p_motivo VARCHAR(300),
    p_observaciones VARCHAR(500) DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_ajuste_id UUID;
    v_stock_actual NUMERIC(12,2);
    v_capacidad NUMERIC(12,2);
    v_estacion_tanque UUID;
    v_estacion_usuario UUID;
    v_tipo_ajuste VARCHAR(20);
    v_cantidad NUMERIC(12,2);
BEGIN
    IF p_conteo_fisico IS NULL OR p_conteo_fisico < 0 THEN
        RAISE EXCEPTION 'CONTEO_FISICO_INVALIDO';
    END IF;

    IF p_motivo IS NULL OR TRIM(p_motivo) = '' THEN
        RAISE EXCEPTION 'MOTIVO_AJUSTE_REQUERIDO';
    END IF;

    SELECT stock_actual, capacidad_maxima, estacion_id
    INTO v_stock_actual, v_capacidad, v_estacion_tanque
    FROM tanque
    WHERE id = p_tanque_id AND activo = TRUE
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'TANQUE_INEXISTENTE_O_INACTIVO';
    END IF;

    IF p_conteo_fisico > v_capacidad THEN
        RAISE EXCEPTION 'CAPACIDAD_TANQUE_EXCEDIDA';
    END IF;

    SELECT estacion_id
    INTO v_estacion_usuario
    FROM usuario
    WHERE id = p_usuario_id AND activo = TRUE;

    IF NOT FOUND OR v_estacion_usuario IS DISTINCT FROM v_estacion_tanque THEN
        RAISE EXCEPTION 'DESPACHADOR_ESTACION_INVALIDA';
    END IF;

    v_cantidad := ABS(p_conteo_fisico - v_stock_actual);
    IF v_cantidad = 0 THEN
        RAISE EXCEPTION 'AJUSTE_SIN_DIFERENCIA';
    END IF;

    v_tipo_ajuste := CASE
        WHEN p_conteo_fisico > v_stock_actual THEN 'POSITIVO'
        ELSE 'NEGATIVO'
    END;

    INSERT INTO ajuste_inventario (
        tanque_id, reportado_por_usuario_id, tipo_ajuste, cantidad,
        conteo_fisico, motivo, observaciones
    ) VALUES (
        p_tanque_id, p_usuario_id, v_tipo_ajuste, v_cantidad,
        p_conteo_fisico, p_motivo, p_observaciones
    )
    RETURNING id INTO v_ajuste_id;

    PERFORM fn_registrar_auditoria(p_usuario_id,'REPORTAR_AJUSTE','ajuste_inventario',v_ajuste_id,NULL,jsonb_build_object('tanqueId',p_tanque_id,'conteoFisico',p_conteo_fisico,'estado','PENDIENTE'));

    -- El reporte no actualiza tanque ni movimiento: solo la aprobación
    -- autorizada podrá hacerlo mediante fn_aprobar_ajuste.
    RETURN v_ajuste_id;
END;
$$;


-- =========================================================
-- 4. APROBAR AJUSTE DE INVENTARIO
-- =========================================================

CREATE OR REPLACE FUNCTION fn_aprobar_ajuste(
    p_ajuste_id UUID,
    p_usuario_revisor_id UUID
)
RETURNS VOID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_tanque_id UUID;
    v_tipo VARCHAR(20);
    v_cantidad NUMERIC(12,2);
    v_conteo_fisico NUMERIC(12,2);
    v_estado VARCHAR(25);

    v_stock_actual NUMERIC(12,2);
    v_capacidad NUMERIC(12,2);
    v_stock_nuevo NUMERIC(12,2);

    v_tipo_movimiento VARCHAR(30);
    v_estacion_id UUID;
    v_tipo_combustible_id SMALLINT;
    v_stock_disponible NUMERIC(12,2);
BEGIN

    SELECT
        tanque_id,
        tipo_ajuste,
        cantidad,
        conteo_fisico,
        estado
    INTO
        v_tanque_id,
        v_tipo,
        v_cantidad,
        v_conteo_fisico,
        v_estado
    FROM ajuste_inventario
    WHERE id = p_ajuste_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'AJUSTE_INEXISTENTE';
    END IF;

    IF v_estado <> 'PENDIENTE' THEN
        RAISE EXCEPTION 'AJUSTE_YA_REVISADO';
    END IF;

    SELECT
        stock_actual,
        capacidad_maxima,
        estacion_id,
        tipo_combustible_id
    INTO
        v_stock_actual,
        v_capacidad,
        v_estacion_id,
        v_tipo_combustible_id
    FROM tanque
    WHERE id = v_tanque_id
    FOR UPDATE;

    IF v_conteo_fisico IS NULL THEN
        RAISE EXCEPTION 'CONTEO_FISICO_REQUERIDO';
    END IF;

    IF EXISTS (
        SELECT 1 FROM cierre_diario
        WHERE tanque_id = v_tanque_id
          AND fecha_cierre = CURRENT_DATE
          AND estado = 'APROBADO'
    ) THEN
        RAISE EXCEPTION 'CIERRE_APROBADO_INMUTABLE';
    END IF;

    v_stock_nuevo := v_conteo_fisico;
    v_cantidad := ABS(v_stock_nuevo - v_stock_actual);

    IF v_cantidad = 0 THEN
        UPDATE ajuste_inventario
        SET estado = 'APROBADO', revisado_por_usuario_id = p_usuario_revisor_id,
            fecha_revision = NOW(), tipo_ajuste = v_tipo, cantidad = 0
        WHERE id = p_ajuste_id;
        PERFORM fn_registrar_auditoria(p_usuario_revisor_id,'APROBAR_AJUSTE','ajuste_inventario',p_ajuste_id,NULL,jsonb_build_object('conteoFisico',v_conteo_fisico,'cantidad',0));
        RETURN;
    END IF;

    IF v_stock_nuevo > v_stock_actual THEN
        v_tipo := 'POSITIVO';
        v_tipo_movimiento := 'AJUSTE_POSITIVO';

        IF v_stock_nuevo > v_capacidad THEN
            RAISE EXCEPTION 'CAPACIDAD_TANQUE_EXCEDIDA';
        END IF;

    ELSE
        v_tipo := 'NEGATIVO';
        v_tipo_movimiento := 'AJUSTE_NEGATIVO';
        PERFORM pg_advisory_xact_lock(hashtextextended(v_estacion_id::TEXT || ':' || v_tipo_combustible_id::TEXT, 0));
        SELECT stock_disponible INTO v_stock_disponible
        FROM fn_obtener_stock_disponible(v_estacion_id, v_tipo_combustible_id);
        IF COALESCE(v_stock_disponible, 0) < v_cantidad THEN
            RAISE EXCEPTION 'STOCK_DISPONIBLE_INSUFICIENTE_POR_RESERVAS';
        END IF;
    END IF;

    UPDATE tanque
    SET
        stock_actual = v_stock_nuevo,
        fecha_actualizacion = NOW()
    WHERE id = v_tanque_id;

    UPDATE ajuste_inventario
    SET
        estado = 'APROBADO',
        tipo_ajuste = v_tipo,
        cantidad = v_cantidad,
        revisado_por_usuario_id = p_usuario_revisor_id,
        fecha_revision = NOW()
    WHERE id = p_ajuste_id;

    INSERT INTO movimiento_inventario (
        tanque_id,
        registrado_por_usuario_id,
        tipo_movimiento,
        cantidad,
        saldo_anterior,
        saldo_posterior,
        ajuste_id,
        fecha_movimiento
    )
    VALUES (
        v_tanque_id,
        p_usuario_revisor_id,
        v_tipo_movimiento,
        v_cantidad,
        v_stock_actual,
        v_stock_nuevo,
        p_ajuste_id,
        NOW()
    );

    PERFORM fn_registrar_auditoria(p_usuario_revisor_id,'APROBAR_AJUSTE','ajuste_inventario',p_ajuste_id,NULL,jsonb_build_object('conteoFisico',v_conteo_fisico,'cantidad',v_cantidad,'tipo',v_tipo));

END;
$$;


-- =========================================================
-- 5. RECHAZAR AJUSTE
-- =========================================================

CREATE OR REPLACE FUNCTION fn_rechazar_ajuste(
    p_ajuste_id UUID,
    p_usuario_revisor_id UUID,
    p_motivo VARCHAR(300)
)
RETURNS VOID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_estado VARCHAR(25);
BEGIN

    IF p_motivo IS NULL OR TRIM(p_motivo) = '' THEN
        RAISE EXCEPTION 'MOTIVO_RECHAZO_REQUERIDO';
    END IF;

    SELECT estado
    INTO v_estado
    FROM ajuste_inventario
    WHERE id = p_ajuste_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'AJUSTE_INEXISTENTE';
    END IF;

    IF v_estado <> 'PENDIENTE' THEN
        RAISE EXCEPTION 'AJUSTE_YA_REVISADO';
    END IF;

    UPDATE ajuste_inventario
    SET
        estado = 'RECHAZADO',
        revisado_por_usuario_id = p_usuario_revisor_id,
        fecha_revision = NOW(),
        motivo_rechazo = p_motivo
    WHERE id = p_ajuste_id;

    PERFORM fn_registrar_auditoria(p_usuario_revisor_id,'RECHAZAR_AJUSTE','ajuste_inventario',p_ajuste_id,NULL,jsonb_build_object('motivo',p_motivo));

END;
$$;

COMMIT;
