#!/usr/bin/env bash
# Seeds the cinema API with showtimes, reservations, and one confirmed purchase.
# Requires: docker compose (Provided API + Redis) and `dotnet run` for ApiApplication.
#
# Usage:
#   ./scripts/seed-test-data.sh
#   BASE_URL=http://localhost:7628 ./scripts/seed-test-data.sh
#   BASE_URL=https://localhost:7629 ./scripts/seed-test-data.sh

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:7628}"
MOVIE_ID="${MOVIE_ID:-ID-test1}"
CURL_OPTS=(-sS)
if [[ "${BASE_URL}" == https://* ]]; then
  CURL_OPTS+=(-k)
fi

echo "==> Checking API at ${BASE_URL}"
if ! curl "${CURL_OPTS[@]}" "${BASE_URL}/swagger/v1/swagger.json" >/dev/null; then
  echo "API is not reachable. Start it with: cd ApiApplication && dotnet run"
  exit 1
fi

json_field() {
  local json="$1"
  local field="$2"
  python3 -c "import json,sys; data=json.load(sys.stdin); print(data${field})" <<<"${json}"
}

post_json() {
  local path="$1"
  local body="$2"
  local response
  local code
  response=$(curl "${CURL_OPTS[@]}" -w "\n%{http_code}" -X POST "${BASE_URL}${path}" \
    -H "Content-Type: application/json" \
    -d "${body}")
  code=$(printf '%s' "${response}" | tail -n1)
  body_out=$(printf '%s' "${response}" | sed '$d')
  if [[ "${code}" -lt 200 || "${code}" -ge 300 ]]; then
    echo "Request failed (${code}) POST ${path}" >&2
    echo "${body_out}" >&2
    exit 1
  fi
  printf '%s' "${body_out}"
}

echo "==> Creating showtimes"
SHOW1=$(post_json "/api/showtimes" "{\"movieId\":\"${MOVIE_ID}\",\"auditoriumId\":1,\"sessionDate\":\"2026-10-20T18:00:00\"}")
SHOW2=$(post_json "/api/showtimes" "{\"movieId\":\"${MOVIE_ID}\",\"auditoriumId\":2,\"sessionDate\":\"2026-10-20T20:30:00\"}")
SHOW3=$(post_json "/api/showtimes" "{\"movieId\":\"${MOVIE_ID}\",\"auditoriumId\":3,\"sessionDate\":\"2026-10-21T15:00:00\"}")

SHOW1_ID=$(json_field "${SHOW1}" "['id']")
SHOW2_ID=$(json_field "${SHOW2}" "['id']")
SHOW3_ID=$(json_field "${SHOW3}" "['id']")

echo "    showtime #1 id=${SHOW1_ID} (auditorium 1)"
echo "    showtime #2 id=${SHOW2_ID} (auditorium 2)"
echo "    showtime #3 id=${SHOW3_ID} (auditorium 3)"

echo "==> Creating reservations"
RES1=$(post_json "/api/showtimes/${SHOW1_ID}/reservations" \
  '{"seats":[{"row":1,"seatNumber":1},{"row":1,"seatNumber":2},{"row":1,"seatNumber":3}]}')
RES2=$(post_json "/api/showtimes/${SHOW2_ID}/reservations" \
  '{"seats":[{"row":5,"seatNumber":10},{"row":5,"seatNumber":11}]}')
RES3=$(post_json "/api/showtimes/${SHOW3_ID}/reservations" \
  '{"seats":[{"row":2,"seatNumber":4}]}')

RES1_ID=$(json_field "${RES1}" "['reservationId']")
RES2_ID=$(json_field "${RES2}" "['reservationId']")
RES3_ID=$(json_field "${RES3}" "['reservationId']")

echo "    reservation #1 ${RES1_ID} (show ${SHOW1_ID}, seats 1-1..1-3) — will confirm"
echo "    reservation #2 ${RES2_ID} (show ${SHOW2_ID}, seats 5-10..5-11) — left open"
echo "    reservation #3 ${RES3_ID} (show ${SHOW3_ID}, seat 2-4) — left open"

echo "==> Confirming reservation #1 (buy seats)"
CONFIRMED=$(post_json "/api/reservations/${RES1_ID}/confirm" "{}")
PAID=$(json_field "${CONFIRMED}" "['paid']")
echo "    reservation ${RES1_ID} paid=${PAID}"

OUT_DIR="$(cd "$(dirname "$0")" && pwd)"
STATE_FILE="${OUT_DIR}/last-seed.json"
cat > "${STATE_FILE}" <<EOF
{
  "baseUrl": "${BASE_URL}",
  "movieId": "${MOVIE_ID}",
  "showtimes": [
    { "id": ${SHOW1_ID}, "auditoriumId": 1, "sessionDate": "2026-10-20T18:00:00" },
    { "id": ${SHOW2_ID}, "auditoriumId": 2, "sessionDate": "2026-10-20T20:30:00" },
    { "id": ${SHOW3_ID}, "auditoriumId": 3, "sessionDate": "2026-10-21T15:00:00" }
  ],
  "reservations": [
    { "id": "${RES1_ID}", "showtimeId": ${SHOW1_ID}, "paid": true, "note": "confirmed purchase" },
    { "id": "${RES2_ID}", "showtimeId": ${SHOW2_ID}, "paid": false, "note": "open reservation for confirm tests" },
    { "id": "${RES3_ID}", "showtimeId": ${SHOW3_ID}, "paid": false, "note": "single-seat open reservation" }
  ]
}
EOF

echo "==> Seed complete"
echo "    State saved to scripts/last-seed.json"
echo ""
echo "Quick checks:"
echo "  curl ${BASE_URL}/api/showtimes/${SHOW1_ID}"
echo "  curl -X POST ${BASE_URL}/api/reservations/${RES2_ID}/confirm"
echo "  Open Swagger: ${BASE_URL}/swagger"
