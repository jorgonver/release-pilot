CREATE TABLE IF NOT EXISTS outbox_messages (
    id UUID PRIMARY KEY,
    correlation_id TEXT NOT NULL,
    event_type TEXT NOT NULL,
    aggregate_id UUID NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    payload_json JSONB NOT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    processed_at TIMESTAMPTZ NULL,
    next_attempt_at TIMESTAMPTZ NULL,
    last_error TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE outbox_messages
    ADD COLUMN IF NOT EXISTS correlation_id TEXT;

UPDATE outbox_messages
SET correlation_id = COALESCE(NULLIF(correlation_id, ''), id::text)
WHERE correlation_id IS NULL OR correlation_id = '';

ALTER TABLE outbox_messages
    ALTER COLUMN correlation_id SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_outbox_messages_pending
    ON outbox_messages (processed_at, next_attempt_at, attempt_count, occurred_at);
