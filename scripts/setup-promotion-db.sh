#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SQL_DIR="$REPO_ROOT/sql/api"

CONNECTION_STRING="${PROMOTION_DB_CONNECTION_STRING:-Host=localhost;Port=5432;Database=releasepilot;Username=releasepilot;Password=releasepilot}"

to_libpq_conninfo() {
  local raw="$1"

  if [[ "$raw" == postgresql://* || "$raw" == postgres://* ]]; then
    echo "$raw"
    return
  fi

  local normalized="$raw"
  normalized="${normalized//; /;}"
  normalized="${normalized//;/ }"
  normalized="${normalized//Host=/host=}"
  normalized="${normalized//Port=/port=}"
  normalized="${normalized//Database=/dbname=}"
  normalized="${normalized//Username=/user=}"
  normalized="${normalized//User Id=/user=}"
  normalized="${normalized//User=/user=}"
  normalized="${normalized//Password=/password=}"

  echo "$normalized"
}

if ! command -v psql >/dev/null 2>&1; then
  echo "[FAIL] psql is required but was not found." >&2
  echo "Install PostgreSQL client tools and retry." >&2
  exit 1
fi

if [[ ! -d "$SQL_DIR" ]]; then
  echo "[FAIL] SQL directory not found: $SQL_DIR" >&2
  exit 1
fi

mapfile -t SQL_FILES < <(find "$SQL_DIR" -maxdepth 1 -type f -name "*.sql" | sort)

if [[ "${#SQL_FILES[@]}" -eq 0 ]]; then
  echo "[FAIL] No SQL files found in: $SQL_DIR" >&2
  exit 1
fi

LIBPQ_CONNINFO="$(to_libpq_conninfo "$CONNECTION_STRING")"

for sql_file in "${SQL_FILES[@]}"; do
  echo "[INFO] Applying promotions schema using: $sql_file"
  psql "$LIBPQ_CONNINFO" -v ON_ERROR_STOP=1 -f "$sql_file"
done

echo "[PASS] Promotion schema setup completed."
