BEGIN;

CREATE TABLE reservas (
    id UUID PRIMARY KEY,
    solicitud_id UUID NOT NULL UNIQUE,
    evento_id UUID NOT NULL,
    asiento_id UUID NOT NULL,
    usuario_id TEXT NOT NULL,
    estado TEXT NOT NULL,
    motivo_rechazo TEXT,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_vencimiento TIMESTAMPTZ,
    fecha_actualizacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_reservas_estado CHECK (
        estado IN ('retenida', 'confirmada', 'rechazada', 'vencida')),
    CONSTRAINT ck_reservas_motivo CHECK (
        motivo_rechazo IN ('asiento_ocupado', 'pago_fallido', 'pago_no_disponible')),
    CONSTRAINT ck_reservas_estado_motivo CHECK (
        (estado = 'rechazada' AND motivo_rechazo IS NOT NULL)
        OR (estado <> 'rechazada' AND motivo_rechazo IS NULL)),
    CONSTRAINT ck_reservas_vencimiento CHECK (
        estado = 'rechazada' OR fecha_vencimiento IS NOT NULL),
    CONSTRAINT ck_reservas_usuario CHECK (usuario_id ~ '[^[:space:]]')
);

CREATE UNIQUE INDEX ux_reserva_asiento_activo
ON reservas (evento_id, asiento_id)
WHERE estado IN ('retenida', 'confirmada');

CREATE INDEX ix_reservas_retenciones_vencimiento ON reservas (fecha_vencimiento)
WHERE estado = 'retenida';

COMMIT;
