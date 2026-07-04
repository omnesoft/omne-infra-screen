# Solution Notes — Shahid Hannure

## How to run

**Prerequisites:** Docker Desktop (or Docker Engine + Compose plugin)

```bash
# 1. Copy the env template and fill in credentials (defaults work for local dev)
cp .env.example .env

# 2. Build images and start all services
docker compose up --build
```

Open **http://localhost:8080** — the widget list should appear within a few seconds
once the API finishes seeding the database.

To stop and remove containers (data volume is preserved):
```bash
docker compose down
```

To also wipe the database volume:
```bash
docker compose down -v
```

## How to run the tests

```bash
# Unit tests only (no Docker, no database required)
dotnet test Omne.Screen.sln -c Release
```

## What was built

| Piece | File |
|---|---|
| API container | `src/Api/Dockerfile` |
| BFF container | `src/Bff/Dockerfile` |
| UI container + nginx proxy | `src/ui/Dockerfile`, `src/ui/nginx.conf` |
| Compose orchestration | `docker-compose.yml` |
| CI pipeline | `.github/workflows/ci.yml` |

## CI pipeline summary

On every push and PR the pipeline runs four jobs in order:

- `test` and `build-ui` run in parallel — dotnet build + xUnit tests (TRX artifact), and `npm ci` + Vite build
- `build-images` runs after both pass — builds all three images tagged `<branch>-<short-sha>`
- `scan` runs last — generates a CycloneDX SBOM with Trivy, then runs a separate vulnerability scan that **fails the job on any fixable HIGH or CRITICAL CVE**; both the SBOM and the scan report are uploaded as artifacts regardless of outcome

## Trade-offs and what I'd harden with more time

**Credentials in `.env`**
For local dev the `.env` file holds plaintext Postgres credentials. In production these would come from a secret store (AWS Secrets Manager, Vault, or GitHub Actions secrets) and never touch the filesystem. The `.env` file is gitignored and `.env.example` ships a `change_me` placeholder as a reminder.

**`EnsureCreated` instead of migrations**
The API calls `EnsureCreated()` on startup to keep the slice self-contained. In production this would be a proper `dotnet ef migrations` step run as a separate init container or pre-deploy job, so schema changes are versioned and rollback is possible.

**Images not pushed to a registry**
The CI builds and scans the images but doesn't push them anywhere — there's no registry configured for this screen. The next step would be adding OIDC-based auth to ECR , ACR or GHCR and a `docker push` step gated on the scan passing.

**Scan scope is API only**
Trivy runs against the API image. BFF and UI images are built but not scanned. With more time I'd scan all three and consider a `.trivyignore` file for any accepted/unfixable findings with documented justification.

**nginx runs as root for port 80**
The nginx master process binds port 80 (requires root) then drops to the `nginx` user for worker processes — this is the standard nginx:alpine pattern. A stricter setup would switch to port 8080 and run fully rootless, at the cost of a slightly more complex nginx config.

**No image layer caching in CI**
The `docker build` steps in CI don't use `--cache-from` / BuildKit cache mounts, so each run does a full rebuild. Adding `docker/build-push-action` with a GitHub Actions cache backend would cut image build time significantly on repeated runs.

**Time spent:** 1 Hours 45 mins

** I have used Amazon Q VS code plugin with claude-sonnet-4.6 model for most of the workout **
