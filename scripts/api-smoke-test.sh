#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5252}"
APP_NAME="${APP_NAME:-smoke-app}"
API_PROJECT_PATH="${API_PROJECT_PATH:-src/ReleasePilot.Api}"
RANDOM_SUFFIX="$(date +%s)"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
API_PID=""
AUTO_STARTED_API=0
API_LOG_FILE=""

fail() {
  echo "[FAIL] $1" >&2
  exit 1
}

log() {
  echo "[INFO] $1"
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

ensure_jq() {
  if command -v jq >/dev/null 2>&1; then
    return
  fi

  log "'jq' not found. Attempting automatic installation..."

  if ! command -v apt-get >/dev/null 2>&1; then
    fail "jq is required and apt-get is not available. Install jq manually and retry."
  fi

  if [[ "${EUID:-$(id -u)}" -eq 0 ]]; then
    apt-get update >/dev/null 2>&1 || fail "Failed to run apt-get update for jq installation."
    apt-get install -y jq >/dev/null 2>&1 || fail "Failed to install jq automatically."
    log "jq installed successfully."
    return
  fi

  if command -v sudo >/dev/null 2>&1 && sudo -n true >/dev/null 2>&1; then
    sudo apt-get update >/dev/null 2>&1 || fail "Failed to run sudo apt-get update for jq installation."
    sudo apt-get install -y jq >/dev/null 2>&1 || fail "Failed to install jq automatically with sudo."
    log "jq installed successfully with sudo."
    return
  fi

  fail "jq is required. Automatic install needs root or passwordless sudo. Run: sudo apt-get install -y jq"
}

require_command curl
ensure_jq

LAST_BODY=""
LAST_STATUS=""

request() {
  local method="$1"
  local path="$2"
  local payload="${3:-}"
  local body_file
  body_file="$(mktemp)"

  if [[ -n "$payload" ]]; then
    LAST_STATUS="$(curl -sS -o "$body_file" -w "%{http_code}" -X "$method" "$BASE_URL$path" -H "Content-Type: application/json" -d "$payload")"
  else
    LAST_STATUS="$(curl -sS -o "$body_file" -w "%{http_code}" -X "$method" "$BASE_URL$path")"
  fi

  LAST_BODY="$(cat "$body_file")"
  rm -f "$body_file"
}

assert_status() {
  local expected="$1"
  if [[ "$LAST_STATUS" != "$expected" ]]; then
    echo "Response body: $LAST_BODY" >&2
    fail "Expected HTTP $expected, got $LAST_STATUS"
  fi
}

json_get() {
  local jq_filter="$1"
  jq -er "$jq_filter" <<<"$LAST_BODY"
}

assert_json_condition() {
  local jq_condition="$1"
  jq -e "$jq_condition" <<<"$LAST_BODY" >/dev/null || fail "JSON assertion failed: $jq_condition | Body: $LAST_BODY"
}

is_api_available() {
  curl -sS -o /dev/null --connect-timeout 1 --max-time 2 "$BASE_URL/api/promotions"
}

cleanup() {
  if [[ "$AUTO_STARTED_API" -eq 1 && -n "$API_PID" ]]; then
    log "Stopping API process started by smoke test (pid=$API_PID)"
    kill "$API_PID" >/dev/null 2>&1 || true
    wait "$API_PID" 2>/dev/null || true
  fi

  if [[ -n "$API_LOG_FILE" && -f "$API_LOG_FILE" ]]; then
    rm -f "$API_LOG_FILE"
  fi
}

trap cleanup EXIT

ensure_api_running() {
  if is_api_available; then
    log "API is already running at $BASE_URL"
    return
  fi

  require_command dotnet
  API_LOG_FILE="$(mktemp)"

  log "API not reachable; starting it from '$API_PROJECT_PATH'"
  (
    cd "$REPO_ROOT"
    dotnet run --project "$API_PROJECT_PATH" --no-build >"$API_LOG_FILE" 2>&1
  ) &

  API_PID="$!"
  AUTO_STARTED_API=1

  for _ in $(seq 1 40); do
    if is_api_available; then
      log "API became available at $BASE_URL"
      return
    fi
    sleep 1
  done

  echo "--- API startup logs ---" >&2
  cat "$API_LOG_FILE" >&2 || true
  fail "API did not become ready at $BASE_URL within timeout"
}

log "Checking API availability at $BASE_URL"
ensure_api_running

VERSION1="1.0.${RANDOM_SUFFIX}.1"
VERSION2="1.0.${RANDOM_SUFFIX}.2"
VERSION3="1.0.${RANDOM_SUFFIX}.3"

log "1) RequestPromotion (for complete flow)"
request POST "/api/promotions" "{
  \"applicationName\": \"$APP_NAME\",
  \"version\": \"$VERSION1\",
  \"sourceEnvironment\": \"dev\",
  \"targetEnvironment\": \"staging\",
  \"workItems\": [
    { \"externalId\": \"SMOKE-101\", \"title\": \"Smoke story\" },
    { \"externalId\": \"SMOKE-102\", \"title\": \"Smoke bug\" }
  ]
}"
assert_status 201
PROMO_COMPLETE_ID="$(json_get '.id')"

log "2) GetPromotionById"
request GET "/api/promotions/$PROMO_COMPLETE_ID"
assert_status 200
assert_json_condition ".id == \"$PROMO_COMPLETE_ID\""
assert_json_condition ".stateHistory | length >= 1"

log "3) Approve -> Start -> Complete"
request POST "/api/promotions/$PROMO_COMPLETE_ID/approve" "{ \"requestedByRole\": \"Approver\" }"
assert_status 200
assert_json_condition '.status == "Approved"'

request POST "/api/promotions/$PROMO_COMPLETE_ID/start"
assert_status 200
assert_json_condition '.status == "InProgress"'

request POST "/api/promotions/$PROMO_COMPLETE_ID/complete"
assert_status 200
assert_json_condition '.status == "Completed"'
assert_json_condition '.completedAt != null'

log "4) RequestPromotion (for rollback flow)"
request POST "/api/promotions" "{
  \"applicationName\": \"$APP_NAME\",
  \"version\": \"$VERSION2\",
  \"sourceEnvironment\": \"dev\",
  \"targetEnvironment\": \"staging\",
  \"workItems\": [
    { \"externalId\": \"SMOKE-201\", \"title\": \"Rollback story\" }
  ]
}"
assert_status 201
PROMO_ROLLBACK_ID="$(json_get '.id')"

request POST "/api/promotions/$PROMO_ROLLBACK_ID/approve" "{ \"requestedByRole\": \"Approver\" }"
assert_status 200
request POST "/api/promotions/$PROMO_ROLLBACK_ID/start"
assert_status 200
request POST "/api/promotions/$PROMO_ROLLBACK_ID/rollback" "{ \"reason\": \"Smoke rollback validation\" }"
assert_status 200
assert_json_condition '.status == "RolledBack"'
assert_json_condition '.rolledBackReason == "Smoke rollback validation"'

log "5) RequestPromotion (for cancel flow)"
request POST "/api/promotions" "{
  \"applicationName\": \"$APP_NAME\",
  \"version\": \"$VERSION3\",
  \"sourceEnvironment\": \"dev\",
  \"targetEnvironment\": \"staging\",
  \"workItems\": []
}"
assert_status 201
PROMO_CANCEL_ID="$(json_get '.id')"

request POST "/api/promotions/$PROMO_CANCEL_ID/cancel"
assert_status 200
assert_json_condition '.status == "Cancelled"'

log "6) List endpoints"
request GET "/api/promotions"
assert_status 200
assert_json_condition 'type == "array"'
assert_json_condition ".[] | select(.id == \"$PROMO_COMPLETE_ID\") | .id == \"$PROMO_COMPLETE_ID\""

request GET "/api/promotions/applications"
assert_status 200
assert_json_condition ".[] | select(. == \"$APP_NAME\")"

request GET "/api/promotions/applications/$APP_NAME?page=1&pageSize=20"
assert_status 200
assert_json_condition '.totalCount >= 3'
assert_json_condition '.items | length >= 3'

request GET "/api/promotions/applications/$APP_NAME/environments/status"
assert_status 200
assert_json_condition ".applicationName == \"$APP_NAME\""
assert_json_condition '.environments | length == 3'

log "Smoke test completed successfully."
echo "[PASS] All endpoints validated for app '$APP_NAME'"