-- =========================================================
-- Proyecto: Fuel Management System
-- Archivo: 03_ticket_sequence.sql
-- Descripción: Control seguro de numeración anual de tickets
-- =========================================================

BEGIN;

CREATE TABLE secuencia_ticket_anual (
    anio SMALLINT PRIMARY KEY,
    ultimo_numero INTEGER NOT NULL DEFAULT 0,
    fecha_actualizacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_secuencia_ticket_anio
        CHECK (anio >= 2020),

    CONSTRAINT chk_secuencia_ticket_numero
        CHECK (ultimo_numero >= 0)
);

COMMIT;