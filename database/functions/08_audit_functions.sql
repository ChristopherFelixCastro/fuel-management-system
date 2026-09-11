-- =========================================================
-- Proyecto: Fuel Management System
-- Archivo: 08_audit_functions.sql
-- Descripción: Auditoría encadenada con SHA-256
-- =========================================================

BEGIN;

CREATE OR REPLACE FUNCTION fn_registrar_auditoria(
    p_usuario_id UUID,
    p_accion VARCHAR(50),
    p_entidad VARCHAR(80),
    p_entidad_id UUID,
    p_datos_anteriores JSONB DEFAULT NULL,
    p_datos_nuevos JSONB DEFAULT NULL,
    p_direccion_ip VARCHAR(45) DEFAULT NULL,
    p_user_agent VARCHAR(500) DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_audit_id UUID;
    v_hash_anterior VARCHAR(64);
    v_hash_actual VARCHAR(64);
    v_fecha TIMESTAMPTZ;
    v_contenido TEXT;
BEGIN

    -- Impide que dos auditorías simultáneas rompan la cadena.
    PERFORM pg_advisory_xact_lock(9102026);

    v_fecha := NOW();

    SELECT hash_actual
    INTO v_hash_anterior
    FROM audit_log
    ORDER BY fecha_hora DESC, id DESC
    LIMIT 1;

    v_contenido :=
        COALESCE(p_usuario_id::TEXT, '')
        || '|'
        || COALESCE(p_accion, '')
        || '|'
        || COALESCE(p_entidad, '')
        || '|'
        || COALESCE(p_entidad_id::TEXT, '')
        || '|'
        || COALESCE(p_datos_anteriores::TEXT, '')
        || '|'
        || COALESCE(p_datos_nuevos::TEXT, '')
        || '|'
        || COALESCE(p_direccion_ip, '')
        || '|'
        || COALESCE(p_user_agent, '')
        || '|'
        || v_fecha::TEXT
        || '|'
        || COALESCE(v_hash_anterior, '');

    v_hash_actual :=
        encode(
            digest(v_contenido, 'sha256'),
            'hex'
        );

    INSERT INTO audit_log (
        usuario_id,
        accion,
        entidad,
        entidad_id,
        datos_anteriores,
        datos_nuevos,
        direccion_ip,
        user_agent,
        fecha_hora,
        hash_anterior,
        hash_actual
    )
    VALUES (
        p_usuario_id,
        p_accion,
        p_entidad,
        p_entidad_id,
        p_datos_anteriores,
        p_datos_nuevos,
        p_direccion_ip,
        p_user_agent,
        v_fecha,
        v_hash_anterior,
        v_hash_actual
    )
    RETURNING id
    INTO v_audit_id;

    RETURN v_audit_id;

END;
$$;

COMMIT;