-- Permite representar correctamente una aprobación cuya diferencia recalculada
-- contra el stock vigente sea cero. No modifica filas existentes.
BEGIN;

DO $$
DECLARE
    v_definition TEXT;
BEGIN
    SELECT pg_get_constraintdef(oid)
    INTO v_definition
    FROM pg_constraint
    WHERE conname = 'chk_ajuste_cantidad'
      AND conrelid = 'ajuste_inventario'::regclass;

    IF v_definition IS NULL
       OR v_definition NOT LIKE '%estado%APROBADO%cantidad = 0%' THEN
        ALTER TABLE ajuste_inventario
            DROP CONSTRAINT IF EXISTS chk_ajuste_cantidad;

        ALTER TABLE ajuste_inventario
            ADD CONSTRAINT chk_ajuste_cantidad
            CHECK (
                cantidad > 0
                OR (estado = 'APROBADO' AND cantidad = 0)
            );
    END IF;
END;
$$;

COMMIT;
