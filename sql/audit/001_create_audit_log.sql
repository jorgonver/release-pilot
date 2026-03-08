CREATE TABLE IF NOT EXISTS audit_log (
    id BIGSERIAL PRIMARY KEY,
    event_id UUID NOT NULL UNIQUE,
    event_type TEXT NOT NULL,
    promotion_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    acting_user TEXT NOT NULL,
    payload_json JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
