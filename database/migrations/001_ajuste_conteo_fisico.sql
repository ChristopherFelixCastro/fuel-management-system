-- Aplicación manual, idempotente y no destructiva para bases ya creadas.
-- No se ejecuta automáticamente por EF Core.
BEGIN;

ALTER TABLE ajuste_inventario
    ADD COLUMN IF NOT EXISTS conteo_fisico NUMERIC(12,2);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'chk_ajuste_conteo_fisico'
          AND conrelid = 'ajuste_inventario'::regclass
    ) THEN
        ALTER TABLE ajuste_inventario
            ADD CONSTRAINT chk_ajuste_conteo_fisico
            CHECK (conteo_fisico IS NULL OR conteo_fisico >= 0);
    END IF;
END;
$$;

COMMIT;
