# Omne Infra Screen — Submission Notes

## Run it (one command)

```bash
docker compose up --build
```

Then open <http://localhost:8080> — the UI lists the seeded widgets
(UI → BFF → API → Postgres). Tear down, including the DB volume:

```bash
docker compose down -v
```

## Tests

```bash
dotnet test Omne.Screen.sln -c Release
```

Unit tests use EF Core InMemory — no database required. CI runs them on every
push/PR and uploads the `.trx` as an artifact.

## What's in the box

- `src/Api/Dockerfile`, `src/Bff/Dockerfile` — multi-stage .NET 10 builds
  (SDK → `aspnet:10.0` runtime), restore layer cached on project files,
  non-root (`USER $APP_UID`, uid 1654).
- `src/ui/Dockerfile` + `src/ui/nginx.conf` — Node build stage →
  `nginx-unprivileged` serving the static bundle and proxying `/api` to the
  BFF. Non-root nginx on 8080.
- `docker-compose.yml` — Postgres + API + BFF + UI on a named bridge network,
  health-gated startup (`depends_on: condition: service_healthy` down the
  whole chain).
- `.github/workflows/ci.yml` — restore/build/test + UI build, image builds
  tagged `branch-shortSHA`, SBOMs (Syft), a Trivy scan that **fails the job on
  fixable HIGH/CRITICAL**, with test results, SBOMs, and scan reports uploaded
  as artifacts.

## Startup ordering

`pg_isready` gates Postgres → API waits for Postgres healthy → the API's
`/health` (which pings the DB and 503s when it can't reach it) gates the API →
BFF waits for the API → UI waits for the BFF. One `up`, no manual steps, no
race against a cold database.

## Design decisions & trade-offs

- **Runtime base = Debian `aspnet:10.0` + `curl`, not chiseled.** Chiseled is
  smaller with a smaller CVE surface, but ships no shell, so an in-container
  HTTP healthcheck against `/health` isn't possible without bundling a probe.
  I chose a genuinely-gating health check over the smaller image inside the
  time box. *Harden:* `aspnet:10.0-noble-chiseled` + a tiny static healthcheck
  (busybox / purpose-built probe) to get both.
- **BFF healthcheck uses `/health`.** The repo already exposes a BFF health
  endpoint, so the compose probe checks the listener through that route.
- **Trivy gates on _fixable_ HIGH/CRITICAL (`ignore-unfixed: true`).** Failing
  on unpatchable base-image CVEs is noise the team can't action; failing on
  fixable ones is real signal. Drop `ignore-unfixed` to be strict.
- **Secrets.** The Postgres password is a literal in compose because it's a
  throwaway local DB. Nothing sensitive is baked into an image — the API
  connection string and the BFF upstream are injected via env at runtime. Prod
  would pull these from a secret store (out of scope).
- **No registry push.** Images are built, loaded, and scanned in CI but not
  pushed (no registry creds in scope). Pushing to GHCR with an attestation is
  a small add.
- **Entry assembly resolved at runtime**
  (`dotnet "$(basename -s .runtimeconfig.json *.runtimeconfig.json).dll"`) so
  the Dockerfiles don't hardcode the published DLL name.

## Security choices

Non-root for every service; minimal published surface (only the UI is exposed —
API / BFF / Postgres stay on the internal network); pinned base-image and
action versions; an SBOM per image; a gated vulnerability scan; fresh
`npm ci` / `dotnet restore` (no host artifacts copied into images).

## Assumptions (verified against the repo)

1. The API seeds on startup with `EnsureCreated()` and is not gated to
   `Development`, so `ASPNETCORE_ENVIRONMENT=Production` is valid in compose.
2. API and BFF are standalone projects with no shared `ProjectReference`s.
3. UI build output is `src/ui/dist` and `src/ui/package-lock.json` is
   committed.
4. Services listen on 8080 in-container; only the UI is published on host
   `8080`.

## What I'd do next (stopped at the cap)

Chiseled images + a bundled healthcheck probe; push to GHCR with build
provenance; Trivy SARIF → GitHub code scanning; a real integration test
(Testcontainers Postgres) hitting `/api/widgets`; compose hardening
(`read_only` root FS, `cap_drop: [ALL]`, `no-new-privileges`, resource limits);
nginx `resolver` + variable `proxy_pass` so the UI survives a BFF restart; move
the DB credential to a compose `secret` / env file.

## Time spent

~3h — containerization ~1.25h, CI ~1.25h, docs/review ~0.5h.
