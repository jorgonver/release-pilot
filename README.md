# Application & Environment Lifecycle Manager

## Quick Start (After Clone, Docker First)

Use this path if you want the fastest way to run the full stack after cloning.

1. Start infrastructure and services:

```bash
cd <repo-root>
docker compose up --build
```

2. In a second terminal, apply audit schema:

```bash
cd <repo-root>
./scripts/setup-audit-db.sh
```

If `psql` is not installed locally, use this Docker-native alternative:

```bash
cd <repo-root>
docker exec -i releasepilot-postgres psql -U releasepilot -d releasepilot < sql/audit/001_create_audit_log.sql
```

3. In a third terminal, run the API smoke test:

```bash
cd <repo-root>
./scripts/api-smoke-test.sh
```

If the script ends with `[PASS]`, the project is running correctly.

## Quick Start (After Clone, Local No Docker)

Use this path if you want to run processes directly with `dotnet run`.

Prerequisites for this path:

- Local Postgres is running and reachable
- Local RabbitMQ is running and reachable

1. Apply DB schemas:

```bash
cd <repo-root>
./scripts/setup-promotion-db.sh
./scripts/setup-audit-db.sh
```

2. Start API (terminal 1):

```bash
cd <repo-root>/src
dotnet run --project ReleasePilot.Api
```

3. Start Outbox Publisher (terminal 2):

```bash
cd <repo-root>/src
dotnet run --project ReleasePilot.OutboxPublisher
```

4. Start Audit Worker (terminal 3):

```bash
cd <repo-root>/src
dotnet run --project ReleasePilot.AuditWorker
```

5. Validate from another terminal:

```bash
cd <repo-root>
./scripts/api-smoke-test.sh
```

## Verification Checklist

If the following checks pass, the setup is healthy:

1. API smoke test returns `[PASS]`:

```bash
cd <repo-root>
./scripts/api-smoke-test.sh
```

2. Outbox is being processed (rows become `processed_at` not null):

```bash
docker exec -it releasepilot-postgres psql -U releasepilot -d releasepilot -c "SELECT id, event_type, processed_at, attempt_count, last_error FROM outbox_messages ORDER BY created_at DESC LIMIT 20;"
```

3. Audit rows are being persisted:

```bash
docker exec -it releasepilot-postgres psql -U releasepilot -d releasepilot -c "SELECT event_type, promotion_id, occurred_at, acting_user FROM audit_log ORDER BY id DESC LIMIT 20;"
```

## Testing

Run all unit tests:

```bash
cd <repo-root>
dotnet test src/release-pilot.sln
```

Run only domain tests:

```bash
cd <repo-root>
dotnet test src/ReleasePilot.Domain.Tests/ReleasePilot.Domain.Tests.csproj
```

Run only application tests:

```bash
cd <repo-root>
dotnet test src/ReleasePilot.Application.Tests/ReleasePilot.Application.Tests.csproj
```

Collect code coverage (Cobertura):

```bash
cd <repo-root>
dotnet test src/release-pilot.sln --collect:"XPlat Code Coverage"
```

Coverage files are written under each test project's `TestResults/` directory.

## Smoke Test Harness

The repository includes an endpoint smoke test script at `scripts/api-smoke-test.sh`.

### Prerequisites

- `.NET SDK 9`
- `Docker` + `Docker Compose` (for Docker-first quick start)
- `curl`
- `jq` (for JSON assertions in the script)
- `psql` (PostgreSQL client, required by DB setup scripts)

Install `jq` on Ubuntu/Debian:

```bash
sudo apt-get update
sudo apt-get install -y jq
```

### Run Smoke Test

```bash
cd <repo-root>
./scripts/api-smoke-test.sh
```

Notes:

- The script auto-starts the API if it is not running and stops it after completion if it started it.
- If `jq` is missing, the script attempts an automatic install when root or passwordless sudo is available.

## API Command Examples

Base URL used below:

```bash
BASE_URL=http://localhost:5252
```

1. Request promotion (`POST /api/promotions`):

```bash
curl -sS -X POST "$BASE_URL/api/promotions" \
	-H "Content-Type: application/json" \
	-d '{
		"applicationName": "checkout-service",
		"version": "1.2.3",
		"sourceEnvironment": "dev",
		"targetEnvironment": "staging",
		"actingUser": "requester-user",
		"workItems": [
			{
				"externalId": "WI-123",
				"title": "Fix checkout timeout"
			}
		]
	}' | tee /tmp/request-promotion.json
```

Capture the created promotion id:

```bash
PROMOTION_ID=$(jq -r '.id' /tmp/request-promotion.json)
echo "$PROMOTION_ID"
```

2. Approve promotion (`POST /api/promotions/{id}/approve`):

```bash
curl -sS -X POST "$BASE_URL/api/promotions/$PROMOTION_ID/approve" \
	-H "Content-Type: application/json" \
	-d '{
		"requestedByRole": "Approver",
		"actingUser": "approver-user"
	}'
```

3. Start deployment (`POST /api/promotions/{id}/start`):

```bash
curl -sS -X POST "$BASE_URL/api/promotions/$PROMOTION_ID/start" \
	-H "Content-Type: application/json" \
	-d '{
		"actingUser": "deployer-user"
	}'
```

4. Complete promotion (`POST /api/promotions/{id}/complete`):

```bash
curl -sS -X POST "$BASE_URL/api/promotions/$PROMOTION_ID/complete" \
	-H "Content-Type: application/json" \
	-d '{
		"actingUser": "deployer-user"
	}'
```

5. Rollback promotion (`POST /api/promotions/{id}/rollback`):

```bash
curl -sS -X POST "$BASE_URL/api/promotions/$PROMOTION_ID/rollback" \
	-H "Content-Type: application/json" \
	-d '{
		"reason": "Deployment verification failed",
		"actingUser": "deployer-user"
	}'
```

6. Cancel promotion (`POST /api/promotions/{id}/cancel`):

```bash
curl -sS -X POST "$BASE_URL/api/promotions/$PROMOTION_ID/cancel" \
	-H "Content-Type: application/json" \
	-d '{
		"actingUser": "requester-user"
	}'
```

Note: `complete` and `rollback` are alternative terminal actions after `start`; run one or the other in a single flow.

## Troubleshooting

### `docker compose up --build` fails because ports are already in use

Symptoms: errors about `5432`, `5672`, `15672`, or `5252` already bound.

Fix:

```bash
docker ps
docker stop <container_id>
```

Or stop the full stack in this repo and retry:

```bash
cd <repo-root>
docker compose down
docker compose up --build
```

### API or workers fail on DB schema errors

Symptoms: table-not-found errors for `promotions`, `outbox_messages`, or `audit_log`.

Fix:

```bash
cd <repo-root>
./scripts/setup-promotion-db.sh
./scripts/setup-audit-db.sh
```

Then rerun the failing process.

### `setup-*.sh` scripts fail with `psql: command not found`

Install PostgreSQL client tools:

```bash
sudo apt-get update
sudo apt-get install -y postgresql-client
```

### Smoke test fails with `jq: command not found`

Install `jq`:

```bash
sudo apt-get update
sudo apt-get install -y jq
```

### Workers start but no audit rows appear

Checks:

- Confirm outbox rows are being processed (`processed_at` not null) using commands in `Verification Checklist`.
- Confirm RabbitMQ is reachable at `localhost:5672`.
- Confirm audit schema has been created (`./scripts/setup-audit-db.sh`).
- Review worker logs:

```bash
cd <repo-root>
docker compose logs -f outbox-publisher audit-worker
```

## Docker Event Pipeline

The solution now includes RabbitMQ + Postgres + API + Outbox Publisher Worker + Audit Worker via `docker-compose.yml`.

### Start Stack (Docker)

For local non-container runs, follow `Quick Start (After Clone, Local No Docker)`.

Start the Docker stack:

```bash
cd <repo-root>
docker compose up --build
```

`docker-compose.yml` includes a one-shot setup container (`promotions-db-setup`) that applies API schema scripts (`sql/api/*.sql`) automatically for containerized runs.

`promotions-db-setup` is expected to exit after completing its work. This is normal.

- It usually appears as `Exited (0)` in `docker compose ps -a`.
- It usually does not appear in `docker compose ps` (running containers only).

For containerized runs, audit schema still needs to be created with `scripts/setup-audit-db.sh` (or equivalent SQL execution) before or while starting `audit-worker`.

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

For runtime validation commands, see `Verification Checklist` above.

