
-- Archivo: 03_integrity_functions.sql
-- Descripción: Funciones de protección de integridad


BEGIN;

CREATE OR REPLACE FUNCTION fn_bloquear_modificacion_historica()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION
        'La tabla % es histórica y no permite operaciones %.',
        TG_TABLE_NAME,
        TG_OP;
END;
$$;


CREATE OR REPLACE FUNCTION fn_proteger_cierre_aprobado()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN

    IF OLD.estado = 'APROBADO' THEN
        RAISE EXCEPTION
            'CIERRE_APROBADO_INMUTABLE';
    END IF;

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;

    RETURN NEW;

END;
$$;
COMMIT;