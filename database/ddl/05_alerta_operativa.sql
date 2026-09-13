-- =========================================================
-- Proyecto: Fuel Management System
-- Archivo:  05_alerta_operativa.sql
-- Módulo:   Cierres, Alertas y Reportes (Jaime)
-- Rama:     feature/closures-reports
-- =========================================================
--
-- TABLA: alerta_operativa
--
-- Almacena alertas operativas generadas por el módulo de
-- cierres, alertas y reportes. Es distinta de la tabla
-- "notificacion" (que es para mensajes personales al usuario).
--
-- NOTA PARA CHRISTOPHER (coordinación de esquema):
--   Esta tabla fue creada dentro de feature/closures-reports.
--   Si ya tenías planeada una tabla de alertas/notificaciones
--   del sistema con estructura similar, coordinar la fusión
--   antes de hacer merge a develop para evitar duplicidad.
--   Contactar a Jaime para alinear el diseño final.
--
-- Tipos de alerta (campo "tipo"):
--   LOW_INVENTORY      — stock de tanque <= nivel_critico
--   NEAR_EXPIRY        — ticket próximo a vencer (< 2 días)
--   EXPIRED            — ticket vencido sin consumir
--   INTEGRATION_FAILURE — fallo al exportar/enviar reporte
--   ADJUSTMENT_PENDING  — ajuste_inventario en estado PENDIENTE
--
-- Severidades: INFO | WARNING | CRITICAL
-- Estados:     PENDIENTE | RESUELTA
-- =========================================================

BEGIN;

CREATE TABLE alerta_operativa (
    id                     UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tipo                   VARCHAR(40)  NOT NULL,
    severidad              VARCHAR(20)  NOT NULL DEFAULT 'WARNING',
    entidad_origen         VARCHAR(80),
    entidad_id             UUID,
    mensaje                VARCHAR(500) NOT NULL,
    estado                 VARCHAR(20)  NOT NULL DEFAULT 'PENDIENTE',
    fecha_creacion         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    fecha_resolucion       TIMESTAMPTZ,
    resuelta_por_usuario_id UUID,

    CONSTRAINT fk_alerta_resuelta_por
        FOREIGN KEY (resuelta_por_usuario_id)
        REFERENCES usuario(id)
        ON DELETE RESTRICT,

    CONSTRAINT chk_alerta_tipo
        CHECK (tipo IN (
            'LOW_INVENTORY',
            'NEAR_EXPIRY',
            'EXPIRED',
            'INTEGRATION_FAILURE',
            'ADJUSTMENT_PENDING'
        )),

    CONSTRAINT chk_alerta_severidad
        CHECK (severidad IN ('INFO', 'WARNING', 'CRITICAL')),

    CONSTRAINT chk_alerta_estado
        CHECK (estado IN ('PENDIENTE', 'RESUELTA')),

    -- Si está RESUELTA, debe tener fecha y responsable.
    CONSTRAINT chk_alerta_resolucion
        CHECK (
            estado = 'PENDIENTE'
            OR (
                fecha_resolucion       IS NOT NULL
                AND resuelta_por_usuario_id IS NOT NULL
            )
        )
);

-- Índice compuesto para detectar duplicados activos rápidamente
-- (tipo + entidad + PENDIENTE → evita alertas duplicadas).
CREATE INDEX idx_alerta_tipo_estado
    ON alerta_operativa (tipo, estado);

-- Índice para búsquedas por entidad origen + id
CREATE INDEX idx_alerta_entidad
    ON alerta_operativa (entidad_origen, entidad_id);

-- Índice para listado cronológico descendente
CREATE INDEX idx_alerta_fecha_creacion
    ON alerta_operativa (fecha_creacion DESC);

COMMIT;
