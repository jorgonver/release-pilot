#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5252}"
APP_NAME="${APP_NAME:-smoke-app}"
RANDOM_SUFFIX="$(date +%s)"

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

require_command curl
require_command python3

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
  local expression="$1"
  python3 -c "import json,sys; data=json.loads(sys.argv[1]); print(eval(sys.argv[2]))" "$LAST_BODY" "$expression"
}

assert_json_condition() {
  local condition="$1"
  local ok
  ok="$(python3 -c "import json,sys; data=json.loads(sys.argv[1]); print('true' if eval(sys.argv[2]) else 'false')" "$LAST_BODY" "$condition")"
  [[ "$ok" == "true" ]] || fail "JSON assertion failed: $condition | Body: $LAST_BODY"
}

log "Checking API availability at $BASE_URL"
request GET "/api/environments"
assert_status 200

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
PROMO_COMPLETE_ID="$(json_get "data['id']")"

log "2) GetPromotionById"
request GET "/api/promotions/$PROMO_COMPLETE_ID"
assert_status 200
assert_json_condition "data['id'] == '$PROMO_COMPLETE_ID'"
assert_json_condition "len(data['stateHistory']) >= 1"

log "3) Approve -> Start -> Complete"
request POST "/api/promotions/$PROMO_COMPLETE_ID/approve" "{ \"requestedByRole\": \"Approver\" }"
assert_status 200
assert_json_condition "data['status'] == 'Approved'"

request POST "/api/promotions/$PROMO_COMPLETE_ID/start"
assert_status 200
assert_json_condition "data['status'] == 'InProgress'"

request POST "/api/promotions/$PROMO_COMPLETE_ID/complete"
assert_status 200
assert_json_condition "data['status'] == 'Completed'"
assert_json_condition "data['completedAt'] is not None"

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
PROMO_ROLLBACK_ID="$(json_get "data['id']")"

request POST "/api/promotions/$PROMO_ROLLBACK_ID/approve" "{ \"requestedByRole\": \"Approver\" }"
assert_status 200
request POST "/api/promotions/$PROMO_ROLLBACK_ID/start"
assert_status 200
request POST "/api/promotions/$PROMO_ROLLBACK_ID/rollback" "{ \"reason\": \"Smoke rollback validation\" }"
assert_status 200
assert_json_condition "data['status'] == 'RolledBack'"
assert_json_condition "data['rolledBackReason'] == 'Smoke rollback validation'"

log "5) RequestPromotion (for cancel flow)"
request POST "/api/promotions" "{
  \"applicationName\": \"$APP_NAME\",
  \"version\": \"$VERSION3\",
  \"sourceEnvironment\": \"dev\",
  \"targetEnvironment\": \"staging\",
  \"workItems\": []
}"
assert_status 201
PROMO_CANCEL_ID="$(json_get "data['id']")"

request POST "/api/promotions/$PROMO_CANCEL_ID/cancel"
assert_status 200
assert_json_condition "data['status'] == 'Cancelled'"

log "6) List endpoints"
request GET "/api/promotions"
assert_status 200
assert_json_condition "isinstance(data, list)"
assert_json_condition "any(item['id'] == '$PROMO_COMPLETE_ID' for item in data)"

request GET "/api/promotions/applications"
assert_status 200
assert_json_condition "'$APP_NAME' in data"

request GET "/api/promotions/applications/$APP_NAME?page=1&pageSize=20"
assert_status 200
assert_json_condition "data['totalCount'] >= 3"
assert_json_condition "len(data['items']) >= 3"

request GET "/api/promotions/applications/$APP_NAME/environments/status"
assert_status 200
assert_json_condition "data['applicationName'] == '$APP_NAME'"
assert_json_condition "len(data['environments']) == 3"

log "Smoke test completed successfully."
echo "[PASS] All endpoints validated for app '$APP_NAME'"