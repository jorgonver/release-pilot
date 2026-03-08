CREATE TABLE IF NOT EXISTS promotions (
    id UUID PRIMARY KEY,
    application_name TEXT NOT NULL,
    version TEXT NOT NULL,
    source_environment TEXT NOT NULL,
    target_environment TEXT NOT NULL,
    status INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    rolled_back_reason TEXT NULL,
    completed_at TIMESTAMPTZ NULL,
    work_items_json JSONB NOT NULL,
    state_history_json JSONB NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_promotions_application_name
    ON promotions (application_name);

CREATE INDEX IF NOT EXISTS ix_promotions_created_at
    ON promotions (created_at);
