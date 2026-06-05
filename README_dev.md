# Developer guide — running the stack & tests

This is the practical "how do I run it" companion to [`README.md`](./README.md)
(layout) and [`BRIEF.md`](./BRIEF.md) (the exercise). Request chain:

```
Browser → UI (React/Vite) → BFF (/api → YARP) → API (.NET 10) → PostgreSQL
```

## Run the whole stack at once (Docker Compose)

This brings up **PostgreSQL + API + BFF + UI** with health checks and a named
network. It's the one-command end-to-end path.

### 1. Configure secrets

Compose reads its variables from a `.env` file in the repo root. Copy the
template and fill in the blanks (`POSTGRES_USER` / `POSTGRES_PASSWORD` ship
empty, and `API_CONNECTION_STRING` has `<user>` / `<password>` placeholders):

```bash
cp .env.example .env
```

Then edit `.env` so the values are consistent across both lines, e.g.:

```dotenv
POSTGRES_DB=omne_screen
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
API_CONNECTION_STRING=Host=postgres;Port=5432;Database=omne_screen;Username=postgres;Password=postgres
```

> The username/password in `API_CONNECTION_STRING` must match `POSTGRES_USER` /
> `POSTGRES_PASSWORD` — the API connects to the `postgres` service over the
> internal `omne` network using these credentials.

### 2. Bring it up

```bash
docker compose up --build
```

Compose starts the services in dependency order, each waiting on the previous
one's health check: `postgres` → `api` → `bff` → `ui`. The API has a 60s
`start_period` (the .NET publish image cold-starts in ~30s), so the first run
takes a minute or so before everything reports healthy.

Open the UI and you should see the seeded widgets listed:

```
http://localhost:8080
```

Only the UI publishes a host port (`8080`). The API, BFF, and Postgres are
reachable only on the internal `omne` network — exactly as they would be behind
the UI/proxy in a real deploy.

### 3. Useful commands

```bash
docker compose up --build -d        # start detached (background)
docker compose ps                   # service status + health
docker compose logs -f api          # follow one service's logs
docker compose down                 # stop and remove containers + network
docker compose down -v              # also drop the pgdata volume (fresh DB)
```

If you change a Dockerfile or source, re-run with `--build` to rebuild images.
Postgres data persists in the `pgdata` named volume across restarts; use
`down -v` when you want a clean database.

## Run the tests

The unit tests (`test/Api.Tests`, xUnit) use **EF Core InMemory** — no database
or Docker required.

```bash
dotnet restore Omne.Screen.sln
dotnet test   Omne.Screen.sln -c Release
```

To produce a TRX report (same as CI):

```bash
dotnet test Omne.Screen.sln -c Release \
  --logger "trx;LogFileName=test-results.trx" \
  --results-directory TestResults
```

### Run the unit tests in Docker (no .NET SDK on the host)

If you don't have the .NET 10 SDK installed locally, you can run the same unit
tests in a pinned SDK container via [`Dockerfile.test`](./Dockerfile.test). It
reuses the exact SDK digest from [`src/Api/Dockerfile`](./src/Api/Dockerfile)
and runs `dotnet test` as the build command, so the build **fails** if any test
fails:

```bash
docker build -f Dockerfile.test .
```

To pull out the TRX report (same format as CI), build the `export` stage to a
local directory:

```bash
docker build -f Dockerfile.test --target export \
  --output type=local,dest=TestResults .
```

A companion [`Dockerfile.test.dockerignore`](./Dockerfile.test.dockerignore)
lets this build see the `test/` directory, which the root `.dockerignore`
deliberately excludes from the production image builds. The tests use EF Core
InMemory, so this needs **no PostgreSQL and no running stack**.

The UI build is verified by compiling it:

```bash
cd src/ui
npm ci
npm run build
```

CI runs all of the above on every push to `main` and on pull requests — see
[`.github/workflows/ci.yml`](./.github/workflows/ci.yml).

## Security measures

The container and CI setup follows a few deliberate hardening choices:

### Images & Dockerfiles

- **Base images pinned by digest.** Every `FROM` pins an immutable `@sha256:…`
  digest, not just a floating tag (e.g.
  `mcr.microsoft.com/dotnet/aspnet:10.0@sha256:8c0b…`). The tag is kept
  alongside for readability, but the digest is what's resolved — so a rebuild
  pulls the *exact* same base layers and can't be silently swapped under us
  (defends against tag mutation / supply-chain drift).
- **Non-root runtime.** The API and BFF drop to the image's `app` user
  (`USER app`); the UI runs as `nginx`. Published files are copied
  `--chown`'d to that user, and nginx writes its PID, logs, and temp/proxy
  buffers under `/tmp/nginx` so it never needs root-owned paths at runtime.
- **Multi-stage builds.** The .NET SDK and Node toolchains live only in build
  stages; the final images ship just the published output on the slim
  `aspnet` / `nginx:alpine` runtimes — no compilers, no source, smaller attack
  surface.
- **Minimal runtime packages.** Only `curl` is added to the .NET runtime (for
  the health check), with `--no-install-recommends` and the apt lists removed
  in the same layer.
- **`.dockerignore`** keeps build artifacts, `node_modules`, tests, and the
  `.git/` history out of the build context, so nothing sensitive or unneeded
  is baked into an image layer.

### Runtime / configuration

- **No hard-coded secrets.** DB credentials come from the `.env` file into the
  Postgres container and into the API's `ConnectionStrings__OmneScreen` at run
  time. `.env` is git-ignored; only `.env.example` (placeholders) is committed.
- **Minimal exposed surface.** Only the UI publishes a host port (`8080`); the
  API, BFF, and Postgres are reachable only on the internal `omne` bridge
  network.
- **Configurable proxy target.** The BFF upstream and the UI's `BFF_ORIGIN`
  are injected via env at start (no rebuild to repoint), so addresses aren't
  frozen into the image.

### CI pipeline ([`.github/workflows/ci.yml`](./.github/workflows/ci.yml))

- **SBOM generation.** Trivy emits a CycloneDX SBOM per image
  (`sbom-<name>.cdx.json`), uploaded as an artifact — a record of exactly what
  shipped.
- **Vulnerability gate.** Trivy scans each image's SBOM for `HIGH,CRITICAL`
  CVEs and **fails the job** (`exit-code: 1`) on any fixable finding.
  `ignore-unfixed` keeps the gate actionable; results are published as SARIF
  to the Security tab and kept as downloadable artifacts.
- **Pinned GitHub Actions.** Every `uses:` is pinned to a full commit SHA (with
  the version in a comment), the same supply-chain reasoning as the image
  digests.
- **Least-privilege token.** The workflow defaults to `contents: read`; only
  the image job widens to `packages: read` + `security-events: write`, and only
  to pull Trivy's DB and publish SARIF.

## Blockers encountered

### No `curl`/`wget` in the .NET runtime image

The Compose health checks for the API and BFF (`docker-compose.yml`) shell out
to `curl` to hit `/health`. The first cut failed: `mcr.microsoft.com/dotnet/aspnet:10.0`
is a slim image that ships **neither `curl` nor `wget`**, so the health check
command wasn't found and the containers sat in an `unhealthy` state — which in
turn blocked everything downstream, since `bff` and `ui` start with
`depends_on: condition: service_healthy`. The whole stack never came up.

Options considered:

- **Install `curl` into the runtime image** (chosen). One `apt-get install
  --no-install-recommends curl` in the runtime stage, as root, before
  `USER app` — with `rm -rf /var/lib/apt/lists/*` in the same layer to keep it
  small. See `src/Api/Dockerfile` / `src/Bff/Dockerfile`.
- *Probe from the .NET app instead* (e.g. a `dotnet`-based health command) —
  avoids the extra package but is more moving parts than a one-line curl.
- *Use the UI's approach* — the `nginx:alpine` image already ships busybox
  `wget`, so the UI health check uses `wget` and needs no install. That trick
  doesn't carry over to the `aspnet` image, which has neither.

A related gotcha on the UI side (documented inline in `docker-compose.yml`):
busybox `wget` must target `http://127.0.0.1:8080/`, not `localhost` — busybox
resolves `localhost` to `::1` (IPv6) first, but nginx only listens on IPv4
(`listen 8080;`), so `localhost` gives "connection refused".

## Prerequisites

- Docker (with Compose v2 — the `docker compose` subcommand) for the full stack
- .NET 10 SDK + Node 20+ only if running the backend/UI outside containers
  (see [`README.md`](./README.md) for the non-Docker workflow)
