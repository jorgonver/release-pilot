CREATE TABLE IF NOT EXISTS idempotency_requests (
    idempotency_key TEXT NOT NULL,
    request_method TEXT NOT NULL,
    request_path TEXT NOT NULL,
    request_hash TEXT NOT NULL,
    status_code INTEGER NULL,
    response_content_type TEXT NULL,
    response_body TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    completed_at TIMESTAMPTZ NULL,
    PRIMARY KEY (idempotency_key, request_method, request_path)
);

CREATE INDEX IF NOT EXISTS ix_idempotency_requests_completed_at
    ON idempotency_requests (completed_at, created_at);
