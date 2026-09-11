

BEGIN;

CREATE OR REPLACE FUNCTION fn_generar_numero_ticket()
RETURNS VARCHAR(25)
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_anio SMALLINT;
    v_numero INTEGER;
    v_prefijo VARCHAR(20);
BEGIN
    -- Año actual
    v_anio := EXTRACT(YEAR FROM CURRENT_DATE)::SMALLINT;

    -- Prefijo configurable
    SELECT valor
    INTO v_prefijo
    FROM configuracion_sistema
    WHERE clave = 'PREFIJO_TICKET';

    IF v_prefijo IS NULL OR TRIM(v_prefijo) = '' THEN
        v_prefijo := 'COM';
    END IF;

    -- Incremento atómico y seguro ante concurrencia
    INSERT INTO secuencia_ticket_anual (
        anio,
        ultimo_numero,
        fecha_actualizacion
    )
    VALUES (
        v_anio,
        1,
        NOW()
    )
    ON CONFLICT (anio)
    DO UPDATE SET
        ultimo_numero = secuencia_ticket_anual.ultimo_numero + 1,
        fecha_actualizacion = NOW()
    RETURNING ultimo_numero
    INTO v_numero;

    RETURN
        v_prefijo
        || '-'
        || v_anio
        || '-'
        || LPAD(v_numero::TEXT, 6, '0');
END;
$$;

COMMIT;