# Omne — Sr. Infrastructure / DevOps Screening Slice

This is the starter repository for the **3–4 hour screening exercise**. It is a
deliberately small, runnable slice of the Omne stack so you can focus on
containerization and CI/CD rather than on writing application code.

> Read the exercise brief in [`BRIEF.md`](./BRIEF.md) for the full task and what we
> score. This README only describes the code you've been given.

## Submission notes

Local run/test instructions and trade-offs for this submission are in
[`SOLUTION.md`](./SOLUTION.md).

Quick start:

```powershell
Copy-Item .env.example .env
# set POSTGRES_PASSWORD in .env
docker compose up --build
```

Then open <http://localhost:8080> and verify:

```powershell
curl http://localhost:8080/healthz
curl http://localhost:8080/api/widgets
```

## What's here

```
omne-infra-screen/
├── src/
│   ├── Api/          .NET 10 minimal API — EF Core + PostgreSQL, /health, /api/widgets
│   ├── Bff/          .NET 10 YARP reverse proxy in front of the API (mirrors our React.BFF)
│   └── ui/           React 18 + Vite UI — lists widgets via /api (through the BFF)
├── test/
│   └── Api.Tests/    xUnit unit tests (no database required — uses EF InMemory)
├── Directory.Build.props      shared build config (net10.0)
├── Directory.Packages.props   central package versions
├── nuget.config               public feed only — restores without Omne credentials
└── Omne.Screen.sln
```

Request chain: **Browser → UI (React/Vite) → BFF (`/api` → YARP) → API → PostgreSQL**.

The API mirrors how our real services are shaped:
- Connection string is read from `ConnectionStrings__OmneScreen` (env / config) — **never hard-coded**.
- `GET /health` pings the database and returns `503` when it can't connect.
- `GET /api/widgets` and `GET /api/widgets/stats` exercise EF Core + Postgres.

The BFF mirrors our `React.BFF`: a YARP reverse proxy that forwards `/api/**` to
the API. Its proxy target is overridable at runtime via
`ReverseProxy__Clusters__api-cluster__Destinations__api__Address`.

The UI calls the API with relative `/api/*` paths, so it works the same in dev and
in a container. In dev, Vite proxies `/api` to the BFF; in a container, your
nginx/Dockerfile is expected to serve the static build and route `/api` to the BFF.

## Prerequisites

- .NET 10 SDK
- Node 20+ (for the UI)
- Docker (you'll be adding the container + compose setup)

## Build & test the backend (without Docker)

```bash
dotnet build Omne.Screen.sln -c Release
dotnet test  Omne.Screen.sln -c Release
```

Run the API against a local Postgres (the API listens on `http://localhost:5080`):

```bash
ConnectionStrings__OmneScreen="Host=localhost;Port=5432;Database=omne_screen;Username=<db-user>;Password=<db-password>" \
  dotnet run --project src/Api
# then: curl localhost:5080/health
```

The BFF listens on `http://localhost:5100` and proxies `/api` to the API.

## Run the UI (dev)

```bash
cd src/ui
npm install
npm run dev          # http://localhost:5173, proxies /api → BFF (http://localhost:5100)
npm run build        # production build into src/ui/dist
```

For a full end-to-end run (UI → BFF → API → Postgres) you'll want the
`docker-compose.yml` you're about to write.

## Your task (summary — see the brief for detail and scoring)

1. **Containerize** the API, the BFF, and the React UI (multi-stage, non-root).
2. **`docker-compose.yml`** bringing up API + BFF + UI + PostgreSQL with health
   checks and a named network — one command to start, and the UI lists widgets
   in the browser.
3. **CI pipeline** (GitHub Actions preferred): build + run the unit tests, build
   the UI, build images tagged with `branch + short SHA`, generate an **SBOM**, run
   a **vulnerability scan (Trivy/Grype) that fails the job on high/critical**, and
   upload test results + SBOM + scan report as artifacts.
4. A short **README** of your own: how to run it, and the trade-offs you made.

**Out of scope for this screen** (they belong to the longer exercise): Kubernetes /
Helm, observability dashboards, Vercel, Keycloak/OIDC, secret stores, full
integration tests. Don't spend time on them.

Please respect the ~4 hour cap and tell us what you'd do next where you stopped.

**Submitting:** fork this repo, work on a branch, and open a **pull request** back to
it (include time spent + next steps in the PR description). We don't accept zip
files — see [`BRIEF.md`](./BRIEF.md) for the full submission steps.
