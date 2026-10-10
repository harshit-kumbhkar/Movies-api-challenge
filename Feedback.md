### Feedback

*Please add below any feedback you want to send to the team*

## Provided API issues found and fixed (docker-compose)

1. **Misnamed movie database file**
   - The container includes `/app/amovies-db.json` (250 IMDb titles).
   - The application loads `/app/movies-db.json`.
   - Because of that mismatch, only the hardcoded mock movie `ID-test1` was available; real ids like `tt0111161` returned 404.
   - **Fix:** mount `./provided-api/movies-db.json` (copied from `amovies-db.json`) to `/app/movies-db.json` in `docker-compose.yaml`.

2. **Intentional failure middleware**
   - The API includes `FailureMiddleware`, controlled by env var `FailurePercentage`.
   - With the default rate, REST calls intermittently return HTTP 500 `"Expected failure"`.
   - **Fix:** set `FailurePercentage=0` in `docker-compose.yaml`.

3. **API key required (documented, not a compose bug)**
   - Header `X-Apikey: 68e5fbda-9ec9-4858-97b2-4a8349764c63` is required for REST and gRPC.
   - The sample gRPC client in this challenge did not send that header (fixed in `ApiClientGrpc`).

4. **HTTPS certificate**
   - Local Docker cert is self-signed; gRPC clients must accept the development certificate (already handled in the challenge client).

## Remaining notes

- After compose changes, recreate the API container (`docker compose up -d --force-recreate api`) so the volume and env var apply.
- Redis has no auth in the provided compose file; acceptable for local challenge use.
