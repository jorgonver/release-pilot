#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SQL_FILE="$REPO_ROOT/sql/audit/001_create_audit_log.sql"

CONNECTION_STRING="${AUDIT_DB_CONNECTION_STRING:-Host=localhost;Port=5432;Database=releasepilot;Username=releasepilot;Password=releasepilot}"

if ! command -v psql >/dev/null 2>&1; then
  echo "[FAIL] psql is required but was not found." >&2
  echo "Install PostgreSQL client tools and retry." >&2
  exit 1
fi

if [[ ! -f "$SQL_FILE" ]]; then
  echo "[FAIL] SQL file not found: $SQL_FILE" >&2
  exit 1
fi

echo "[INFO] Applying audit schema using: $SQL_FILE"
psql "$CONNECTION_STRING" -v ON_ERROR_STOP=1 -f "$SQL_FILE"
echo "[PASS] Audit schema setup completed."
