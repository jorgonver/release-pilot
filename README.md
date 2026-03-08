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

The solution now includes RabbitMQ + Postgres + API + Audit Worker via `docker-compose.yml`.

### Start Stack

```bash
cd /home/jorge/projects/release-pilot
docker compose up --build
```

Services:

- API: `http://localhost:5252`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ Management UI: `http://localhost:15672` (user: `guest`, pass: `guest`)
- Postgres: `localhost:5432` (db: `releasepilot`, user: `releasepilot`, pass: `releasepilot`)

### Event Publishing and Audit Log

- Every promotion state transition emits a domain event.
- API publishes each event to RabbitMQ exchange `releasepilot.promotions`.
- Decoupled `ReleasePilot.AuditWorker` consumes from queue `releasepilot.audit`.
- Worker persists audit entries into Postgres table `audit_log` with:
	- `event_type`
	- `promotion_id`
	- `occurred_at`
	- `acting_user`

Set acting user via the `actingUser` field in each command request payload.

### Verify Audit Records

After invoking API transitions:

```bash
docker exec -it releasepilot-postgres psql -U releasepilot -d releasepilot -c "SELECT event_type, promotion_id, occurred_at, acting_user FROM audit_log ORDER BY id DESC LIMIT 20;"
```

