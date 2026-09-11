-- =========================================================
-- Proyecto: Fuel Management System
-- Archivo: 07_closure_operations.sql
-- Descripción:
--   Creación, aprobación y rechazo de cierres diarios
-- =========================================================

BEGIN;

-- =========================================================
-- 1. CREAR CIERRE DIARIO
-- =========================================================

CREATE OR REPLACE FUNCTION fn_crear_cierre_diario(
    p_tanque_id UUID,
    p_usuario_id UUID,
    p_fecha DATE,
    p_stock_fisico_final NUMERIC(12,2),
    p_motivo_diferencia VARCHAR(500) DEFAULT NULL,
    p_observaciones VARCHAR(500) DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_cierre_id UUID;

    v_stock_inicial NUMERIC;
    v_recepciones NUMERIC;
    v_transferencias_entrada NUMERIC;
    v_transferencias_salida NUMERIC;
    v_despachos NUMERIC;
    v_ajustes_positivos NUMERIC;
    v_ajustes_negativos NUMERIC;
    v_stock_teorico NUMERIC;

    v_diferencia NUMERIC(12,2);

    v_rol VARCHAR(30);
    v_estacion_usuario UUID;
    v_estacion_tanque UUID;
    v_usuario_activo BOOLEAN;
BEGIN

    -- -----------------------------------------------------
    -- Validaciones básicas
    -- -----------------------------------------------------

    IF p_stock_fisico_final IS NULL
       OR p_stock_fisico_final < 0 THEN
        RAISE EXCEPTION 'STOCK_FISICO_FINAL_INVALIDO';
    END IF;

    IF p_fecha > CURRENT_DATE THEN
        RAISE EXCEPTION 'FECHA_CIERRE_FUTURA_NO_PERMITIDA';
    END IF;

    -- -----------------------------------------------------
    -- Validar despachador
    -- -----------------------------------------------------

    SELECT
        r.nombre,
        u.estacion_id,
        u.activo
    INTO
        v_rol,
        v_estacion_usuario,
        v_usuario_activo
    FROM usuario u
    JOIN rol r
        ON r.id = u.rol_id
    WHERE u.id = p_usuario_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'USUARIO_INEXISTENTE';
    END IF;

    IF v_usuario_activo = FALSE THEN
        RAISE EXCEPTION 'USUARIO_INACTIVO';
    END IF;

    IF v_rol <> 'DESPACHADOR' THEN
        RAISE EXCEPTION 'USUARIO_NO_ES_DESPACHADOR';
    END IF;

    -- -----------------------------------------------------
    -- Validar tanque y estación
    -- -----------------------------------------------------

    SELECT estacion_id
    INTO v_estacion_tanque
    FROM tanque
    WHERE id = p_tanque_id
      AND activo = TRUE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'TANQUE_INEXISTENTE_O_INACTIVO';
    END IF;

    IF v_estacion_usuario <> v_estacion_tanque THEN
        RAISE EXCEPTION 'DESPACHADOR_ESTACION_INVALIDA';
    END IF;

    -- -----------------------------------------------------
    -- Obtener cálculo del cierre
    -- -----------------------------------------------------

    SELECT
        c.stock_inicial,
        c.total_recepciones,
        c.total_transferencias_entrada,
        c.total_transferencias_salida,
        c.total_despachos,
        c.total_ajustes_positivos,
        c.total_ajustes_negativos,
        c.stock_teorico_final
    INTO
        v_stock_inicial,
        v_recepciones,
        v_transferencias_entrada,
        v_transferencias_salida,
        v_despachos,
        v_ajustes_positivos,
        v_ajustes_negativos,
        v_stock_teorico
    FROM fn_calcular_cierre_diario(
        p_tanque_id,
        p_fecha
    ) c;

    v_diferencia :=
        p_stock_fisico_final - v_stock_teorico;

    -- Si existe diferencia, exigimos explicación.
    IF v_diferencia <> 0
       AND (
            p_motivo_diferencia IS NULL
            OR TRIM(p_motivo_diferencia) = ''
       )
    THEN
        RAISE EXCEPTION 'MOTIVO_DIFERENCIA_REQUERIDO';
    END IF;

    -- -----------------------------------------------------
    -- Registrar cierre pendiente
    -- -----------------------------------------------------

    INSERT INTO cierre_diario (
        tanque_id,
        creado_por_usuario_id,
        fecha_cierre,

        stock_inicial,
        total_recepciones,
        total_transferencias_entrada,
        total_transferencias_salida,
        total_despachos,
        total_ajustes_positivos,
        total_ajustes_negativos,

        stock_teorico_final,
        stock_fisico_final,
        diferencia,

        estado,
        motivo_diferencia,
        observaciones
    )
    VALUES (
        p_tanque_id,
        p_usuario_id,
        p_fecha,

        v_stock_inicial,
        v_recepciones,
        v_transferencias_entrada,
        v_transferencias_salida,
        v_despachos,
        v_ajustes_positivos,
        v_ajustes_negativos,

        v_stock_teorico,
        p_stock_fisico_final,
        v_diferencia,

        'PENDIENTE_APROBACION',
        p_motivo_diferencia,
        p_observaciones
    )
    RETURNING id
    INTO v_cierre_id;

    RETURN v_cierre_id;

END;
$$;


-- =========================================================
-- 2. APROBAR CIERRE
-- =========================================================

CREATE OR REPLACE FUNCTION fn_aprobar_cierre(
    p_cierre_id UUID,
    p_supervisor_id UUID
)
RETURNS VOID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_estado VARCHAR(30);
    v_rol VARCHAR(30);
    v_activo BOOLEAN;
BEGIN

    SELECT
        r.nombre,
        u.activo
    INTO
        v_rol,
        v_activo
    FROM usuario u
    JOIN rol r
        ON r.id = u.rol_id
    WHERE u.id = p_supervisor_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'SUPERVISOR_INEXISTENTE';
    END IF;

    IF v_activo = FALSE THEN
        RAISE EXCEPTION 'SUPERVISOR_INACTIVO';
    END IF;

    IF v_rol <> 'SUPERVISOR' THEN
        RAISE EXCEPTION 'USUARIO_NO_ES_SUPERVISOR';
    END IF;

    SELECT estado
    INTO v_estado
    FROM cierre_diario
    WHERE id = p_cierre_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'CIERRE_INEXISTENTE';
    END IF;

    IF v_estado <> 'PENDIENTE_APROBACION' THEN
        RAISE EXCEPTION 'CIERRE_YA_REVISADO';
    END IF;

    UPDATE cierre_diario
    SET
        estado = 'APROBADO',
        revisado_por_usuario_id = p_supervisor_id,
        fecha_revision = NOW()
    WHERE id = p_cierre_id;

END;
$$;


-- =========================================================
-- 3. RECHAZAR CIERRE
-- =========================================================

CREATE OR REPLACE FUNCTION fn_rechazar_cierre(
    p_cierre_id UUID,
    p_supervisor_id UUID,
    p_motivo VARCHAR(300)
)
RETURNS VOID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_estado VARCHAR(30);
    v_rol VARCHAR(30);
    v_activo BOOLEAN;
BEGIN

    IF p_motivo IS NULL
       OR TRIM(p_motivo) = '' THEN
        RAISE EXCEPTION 'MOTIVO_RECHAZO_REQUERIDO';
    END IF;

    SELECT
        r.nombre,
        u.activo
    INTO
        v_rol,
        v_activo
    FROM usuario u
    JOIN rol r
        ON r.id = u.rol_id
    WHERE u.id = p_supervisor_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'SUPERVISOR_INEXISTENTE';
    END IF;

    IF v_activo = FALSE THEN
        RAISE EXCEPTION 'SUPERVISOR_INACTIVO';
    END IF;

    IF v_rol <> 'SUPERVISOR' THEN
        RAISE EXCEPTION 'USUARIO_NO_ES_SUPERVISOR';
    END IF;

    SELECT estado
    INTO v_estado
    FROM cierre_diario
    WHERE id = p_cierre_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'CIERRE_INEXISTENTE';
    END IF;

    IF v_estado <> 'PENDIENTE_APROBACION' THEN
        RAISE EXCEPTION 'CIERRE_YA_REVISADO';
    END IF;

    UPDATE cierre_diario
    SET
        estado = 'RECHAZADO',
        revisado_por_usuario_id = p_supervisor_id,
        fecha_revision = NOW(),
        motivo_rechazo = p_motivo
    WHERE id = p_cierre_id;

END;
$$;

COMMIT;