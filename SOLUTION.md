# Omne Infra Screen - Submission Notes

## Run it

```bash
docker compose up --build
```

Open <http://localhost:8080> — the seeded widgets appear in the browser.

To override the default credentials, copy `.env.example` to `.env` and edit before starting:

```bash
cp .env.example .env   # then edit POSTGRES_PASSWORD before running compose
```

To tear everything down, including the database volume:

```bash
docker compose down -v
```

## Test it locally

Backend unit tests:

```bash
dotnet test Omne.Screen.sln -c Release
```

Full stack smoke check:

```bash
docker compose ps
curl http://localhost:8080/healthz
curl http://localhost:8080/api/widgets
curl http://localhost:8080/api/widgets/stats
```

Expected result:
- `docker compose ps` shows all services healthy
- `/healthz` returns `ok`
- `/api/widgets` returns the seeded widgets

## What was added

- Multi-stage, non-root Dockerfiles for the API, BFF, and UI.
- `docker-compose.yml` for Postgres, API, BFF, and UI with health-gated startup on a named bridge network.
- A GitHub Actions workflow that restores, builds, and tests the .NET solution, builds the UI, builds branch+SHA tagged images, generates SBOMs, runs Trivy, and uploads test/security artifacts.

## Trade-offs and security notes

- API/BFF use `aspnet:10.0` plus `curl` so the health checks can probe real HTTP endpoints. A chiseled image would be smaller, but would need an extra probe binary.
- Only the UI is published to the host. API, BFF, and Postgres stay on the internal Docker network.
- Containers run as non-root users.
- Database settings are injected from environment variables rather than committed as fixed values in compose.
- CI is configured to fail on fixable HIGH/CRITICAL vulnerabilities, which gives useful signal but can block the pipeline on base-image or inherited dependency issues.

## If I had more time

- Pin image digests and full action SHAs.
- Push images to GHCR with provenance/attestations.
- Add a small integration test that brings the stack up and hits `/api/widgets`.
- Harden the compose services further with read-only filesystems, dropped capabilities, and resource limits.
