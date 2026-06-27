# Omne Infra Screen - Submission Notes

## Run it

```bash
docker compose up --build
```

Open <http://localhost:8080>. The UI is served by nginx and reaches the API through the BFF.

To tear everything down, including the database volume:

```bash
docker compose down -v
```

## Test it

```bash
dotnet test Omne.Screen.sln -c Release
```

The unit tests use EF Core InMemory, so they do not require Postgres.

## What was added

- Multi-stage, non-root Dockerfiles for the API, BFF, and UI.
- `docker-compose.yml` for Postgres, API, BFF, and UI with health-gated startup on a named bridge network.
- A GitHub Actions workflow that restores, builds, and tests the .NET solution, builds the UI, builds branch+SHA tagged images, generates SBOMs, runs Trivy, and uploads test/security artifacts.

## Trade-offs and security notes

- API/BFF use `aspnet:10.0` plus `curl` so the health checks can probe real HTTP endpoints. A chiseled image would be smaller, but would need an extra probe binary.
- Only the UI is published to the host. API, BFF, and Postgres stay on the internal Docker network.
- Containers run as non-root users.
- Local database settings are injected via environment variables with defaults for one-command bring-up. In a production setup, credentials would come from a proper secret source.
- CI is configured to fail on fixable HIGH/CRITICAL vulnerabilities, which is useful signal but can block the pipeline on dependency issues inherited from the starter repo.

## If I had more time

- Pin image digests and full action SHAs.
- Push images to GHCR with provenance/attestations.
- Add a small integration test that brings the stack up and hits `/api/widgets`.
- Harden the compose services further with read-only filesystems, dropped capabilities, and resource limits.
