CREATE TABLE IF NOT EXISTS audit_log (
    id BIGSERIAL PRIMARY KEY,
    event_id UUID NOT NULL UNIQUE,
    correlation_id TEXT NOT NULL,
    event_type TEXT NOT NULL,
    promotion_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    acting_user TEXT NOT NULL,
    payload_json JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE audit_log
    ADD COLUMN IF NOT EXISTS correlation_id TEXT;

UPDATE audit_log
SET correlation_id = COALESCE(NULLIF(correlation_id, ''), event_id::text)
WHERE correlation_id IS NULL OR correlation_id = '';

ALTER TABLE audit_log
    ALTER COLUMN correlation_id SET NOT NULL;
