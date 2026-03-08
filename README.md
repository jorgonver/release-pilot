# Application & Environment Lifecycle Manager

## Smoke Test Harness

The repository includes an endpoint smoke test script at `scripts/api-smoke-test.sh`.

### Prerequisites

- `.NET SDK 9`
- `curl`
- `jq` (for JSON assertions in the script)

Install `jq` on Ubuntu/Debian:

```bash
sudo apt-get update
sudo apt-get install -y jq
```

### Run Smoke Test

```bash
cd /home/jorge/projects/release-pilot
./scripts/api-smoke-test.sh
```

Notes:

- The script auto-starts the API if it is not running and stops it after completion if it started it.
- If `jq` is missing, the script attempts an automatic install when root or passwordless sudo is available.

## Docker Event Pipeline

The solution now includes RabbitMQ + Postgres + API + Outbox Publisher Worker + Audit Worker via `docker-compose.yml`.

### Start Stack

Promotion schema setup is now an explicit prerequisite. Run it before starting the API:

```bash
cd /home/jorge/projects/release-pilot
./scripts/setup-promotion-db.sh
```

Optional override for a non-default database target:

```bash
PROMOTION_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=releasepilot;Username=releasepilot;Password=releasepilot" ./scripts/setup-promotion-db.sh
```

This setup applies all versioned scripts in `sql/api` (including promotions and outbox tables).

Audit schema setup is also an explicit prerequisite. Run it before starting the audit worker:

```bash
cd /home/jorge/projects/release-pilot
./scripts/setup-audit-db.sh
```

Optional override for a non-default database target:

```bash
AUDIT_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=releasepilot;Username=releasepilot;Password=releasepilot" ./scripts/setup-audit-db.sh
```

Then start the stack:

```bash
cd /home/jorge/projects/release-pilot
docker compose up --build
```

`docker-compose.yml` also includes a one-shot setup container (`promotions-db-setup`) that applies the promotions schema automatically for containerized runs.

Services:

- API: `http://localhost:5252`
- Outbox Publisher Worker: background service that reads `outbox_messages` and publishes to RabbitMQ
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ Management UI: `http://localhost:15672` (user: `guest`, pass: `guest`)
- Postgres: `localhost:5432` (db: `releasepilot`, user: `releasepilot`, pass: `releasepilot`)

### Event Publishing and Audit Log

- Every promotion state transition emits a domain event.
- API writes each integration event to Postgres `outbox_messages` in the same transaction as promotion state changes.
- Dedicated `ReleasePilot.OutboxPublisher` worker reads pending outbox rows and publishes them to RabbitMQ exchange `releasepilot.promotions`.
- Decoupled `ReleasePilot.AuditWorker` consumes from queue `releasepilot.audit`.
- Worker persists audit entries into Postgres table `audit_log` with:
	- `event_type`
	- `promotion_id`
	- `occurred_at`
	- `acting_user`
- Worker fails fast at startup if `audit_log` does not exist.

API promotion data is persisted in Postgres table `promotions`.
Outbox state is persisted in Postgres table `outbox_messages`.

Set acting user via the `actingUser` field in each command request payload.

### Verify Audit Records

After invoking API transitions:

```bash
docker exec -it releasepilot-postgres psql -U releasepilot -d releasepilot -c "SELECT event_type, promotion_id, occurred_at, acting_user FROM audit_log ORDER BY id DESC LIMIT 20;"
```

