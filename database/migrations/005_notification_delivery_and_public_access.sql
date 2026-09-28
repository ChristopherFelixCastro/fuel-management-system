-- ============================================================================
-- MIGRACIÓN 005: Outbox de Entrega de Notificaciones y Acceso Público a Tickets
-- Proyecto: La Bomba — Tickets Digitales e Inventario de Combustible
-- ============================================================================

-- 1. TABLA: ticket_acceso_publico
--    Permite la visualización pública del ticket vía token opaco sin exponer
--    el UUID interno. Solo almacena el nonce aleatorio y el hash SHA-256 del token.
CREATE TABLE IF NOT EXISTS ticket_acceso_publico (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL,
    nonce VARCHAR(64) NOT NULL,
    token_hash VARCHAR(64) NOT NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_expiracion TIMESTAMPTZ NOT NULL,
    fecha_revocacion TIMESTAMPTZ NULL,

    CONSTRAINT fk_ticket_acceso_ticket
        FOREIGN KEY (ticket_id)
        REFERENCES ticket(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_ticket_acceso_token_hash
        UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS idx_ticket_acceso_lookup
    ON ticket_acceso_publico (token_hash, fecha_expiracion)
    WHERE fecha_revocacion IS NULL;

CREATE INDEX IF NOT EXISTS idx_ticket_acceso_ticket
    ON ticket_acceso_publico (ticket_id)
    WHERE fecha_revocacion IS NULL;


-- 2. TABLA: notificacion_entrega
--    Outbox transaccional para entrega asíncrona hacia proveedores externos (Brevo / Infobip).
--    Preserva la trazabilidad histórica de despachos (ON DELETE RESTRICT hacia ticket).
CREATE TABLE IF NOT EXISTS notificacion_entrega (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL,
    tipo_evento VARCHAR(40) NOT NULL, -- 'TICKET_APROBADO'
    canal VARCHAR(20) NOT NULL,       -- 'EMAIL' | 'SMS'
    destinatario VARCHAR(200) NULL,   -- NULL únicamente cuando estado = 'OMITIDA'
    estado VARCHAR(30) NOT NULL DEFAULT 'PENDIENTE', -- 'PENDIENTE' | 'EN_PROCESO' | 'ENVIADA' | 'FALLIDA' | 'OMITIDA'
    intentos INT NOT NULL DEFAULT 0,
    max_intentos INT NOT NULL DEFAULT 3,
    ultimo_error TEXT NULL,
    provider_message_id VARCHAR(120) NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_proximo_intento TIMESTAMPTZ NULL,
    fecha_envio TIMESTAMPTZ NULL,
    procesando_desde TIMESTAMPTZ NULL,
    worker_id VARCHAR(100) NULL,

    CONSTRAINT fk_notificacion_entrega_ticket
        FOREIGN KEY (ticket_id)
        REFERENCES ticket(id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_notificacion_entrega_ticket_evento_canal
        UNIQUE (ticket_id, tipo_evento, canal),

    CONSTRAINT chk_notificacion_entrega_canal
        CHECK (canal IN ('EMAIL', 'SMS')),

    CONSTRAINT chk_notificacion_entrega_estado
        CHECK (estado IN ('PENDIENTE', 'EN_PROCESO', 'ENVIADA', 'FALLIDA', 'OMITIDA')),

    CONSTRAINT chk_notificacion_entrega_intentos
        CHECK (intentos >= 0 AND max_intentos > 0),

    CONSTRAINT chk_notificacion_entrega_omision
        CHECK (
            (estado = 'OMITIDA' AND destinatario IS NULL AND ultimo_error IS NOT NULL)
            OR
            (estado <> 'OMITIDA' AND destinatario IS NOT NULL)
        )
);

CREATE INDEX IF NOT EXISTS idx_notificacion_entrega_lease
    ON notificacion_entrega (estado, fecha_proximo_intento, procesando_desde, fecha_creacion)
    WHERE estado IN ('PENDIENTE', 'FALLIDA', 'EN_PROCESO');

CREATE INDEX IF NOT EXISTS idx_notificacion_entrega_ticket
    ON notificacion_entrega (ticket_id);
