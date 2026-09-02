# Deploying FitRos.API to Render + Neon

Notes to myself for the first deploy — nothing below has been run yet. No
Render account, no Neon project, no service exists. This is the checklist I'm
following when I actually do it.

Same shape as my other side projects: one web service on Render, Postgres on
Neon, the DB reached only through a connection-string env var, TLS handled by
Render. .NET isn't auto-detected by Render, so this deploy needs the
`Dockerfile` + `render.yaml` sitting alongside this file.

Files added for the deploy:
- `Backend/Dockerfile` — multi-stage .NET 8 build, binds Kestrel to `$PORT`.
- `Backend/.dockerignore`
- `Backend/render.yaml` — Render Blueprint (web service + env var list).

---

## 0. Prerequisites / decisions still open

- **Push first.** Render deploys from GitHub, so none of this works until the
  branch is pushed: `git push origin master`.
- Free tier assumed (`plan: free` in `render.yaml`). Free web services spin
  down after ~15 min idle (first request after that takes ~30–50 s) and have
  **no shell tab** — that's why migrations below run from my machine instead.
- `region: oregon` in `render.yaml` — closest free US region. Change if I end
  up wanting Frankfurt/Singapore instead.

---

## 1. Create the Neon database

1. Go to https://neon.tech, sign in, **New Project**.
   - Name: `fitros` (anything works).
   - Postgres version: 16 is fine.
   - Region: pick the one nearest the Render region above (Oregon → "US
     West").
2. After it's created, open **Dashboard → Connection Details**.
3. Copy the connection string. Neon gives a URI like:
   ```
   postgresql://fitros_owner:npg_XXXXXXXX@ep-cool-name-12345.us-west-2.aws.neon.tech/fitros?sslmode=require
   ```
4. **Convert it to Npgsql keyword form** (Npgsql doesn't take the URI form).
   From the parts of the URI above:
   ```
   Host=ep-cool-name-12345.us-west-2.aws.neon.tech;Database=fitros;Username=fitros_owner;Password=npg_XXXXXXXX;SSL Mode=Require;Trust Server Certificate=true;Pooling=true
   ```
   - Use the **pooled** host if Neon shows one (has `-pooler` in it) — better
     for a small web service.
   - Keep `SSL Mode=Require`. `Trust Server Certificate=true` avoids shipping
     Neon's CA; drop it if I'd rather add the CA properly later.
5. Keep this string — it's the `ConnectionStrings__DefaultConnection` value in
   step 3.

---

## 2. Apply the database schema to Neon (from my machine)

The API doesn't migrate on startup, and the free tier has no shell, so
migrations run locally against Neon once now and again whenever a new
migration is added.

From `Backend/`:
```bash
# one-time: dotnet-ef is already used in this repo
dotnet tool install --global dotnet-ef   # skip if already installed

dotnet ef database update \
  -p FitRos.Infrastructure -s FitRos.API \
  --connection "Host=...;Database=fitros;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
```
Use the same connection string as step 1.4. When it finishes, the Neon DB has
all tables and the seeded owner user.

> Heads up: `dotnet ef migrations add` reports model drift on a
> seed-timestamp diff for the owner row. It doesn't affect `database update` —
> the schema applies fine either way.

---

## 3. Create the Render service

### Option A — Blueprint (uses `render.yaml`, recommended)
1. https://dashboard.render.com → **New → Blueprint**.
2. Connect the GitHub repo (`LSvargas25/fitros-api`) and the branch (`master`).
3. Render reads `render.yaml` and shows one service `fitros-api` with the env
   vars. It prompts for every `sync: false` var — fill them in (step 3 values
   below). `Jwt__SigningKey` is `generateValue: true`, leave it.
4. **Apply** → first build + deploy starts.

### Option B — manual (no Blueprint)
1. **New → Web Service**, connect the repo, branch `master`.
2. Runtime: **Docker**. Dockerfile path `./Dockerfile`, context `.`.
3. Instance type: Free.
4. Add the env vars from the list below by hand.
5. **Create Web Service**.

### Env vars to set (both options need the `sync: false` ones)

| Key | Value |
|-----|-------|
| `ConnectionStrings__DefaultConnection` | Npgsql string from step 1.4 |
| `Jwt__SigningKey` | a random string ≥ 32 chars (Blueprint auto-generates; else `openssl rand -base64 48`) |
| `Jwt__Issuer` | `FitRos.API` (already in `render.yaml` / appsettings default) |
| `Jwt__Audience` | `FitRos.Client` |
| `Smtp__Username` | Gmail address used to send password-reset mail |
| `Smtp__Password` | Gmail **App Password** (16 chars, not the account password) |
| `Smtp__From` | the "from" address (usually same as `Smtp__Username`) |
| `Frontend__BaseUrl` | public URL of the deployed Angular app, e.g. `https://fitros.vercel.app` — used in reset-password email links |
| `ASPNETCORE_ENVIRONMENT` | `Production` (set in `render.yaml`) |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` (set in `render.yaml`) |
| `Auth__OwnerSeedPasswordHash` *(optional)* | a `PasswordHasher<T>.HashPassword` value to overwrite the seeded owner's password with at boot, instead of leaving the placeholder from the migrations in place |

The migrations seed `owner@fitros.com` with a placeholder password
(`ChangeMe123!`) — fine for local dev, not something to leave live. After the
first deploy, either log in once and hit `POST /api/auth/forgot-password` to
get a real password set through the normal reset-token flow, or set
`Auth__OwnerSeedPasswordHash` above before that first boot.

The app boots without the `Smtp__*` vars, but any flow that sends email
(password reset) throws until they're set.

---

## 4. First deploy — what to expect

- Build takes a few minutes (SDK image pull + restore + publish).
- On success the log shows `Now listening on: http://+:10000` and Render marks
  it **Live** at `https://fitros-api.onrender.com` (or whatever name I pick).
- There's **no Swagger UI** in Production (`Program.cs` only maps Swagger when
  `IsDevelopment()`). Test with a real call instead, e.g.
  `POST https://fitros-api.onrender.com/api/auth/login`.
- Health checks: `render.yaml` sets `healthCheckPath: /health`, backed by the
  unauthenticated `{ status: "ok" }` route in `Program.cs`. Render restarts the
  instance automatically if it stops responding.

---

## 5. Follow-ups (small code changes, not done yet)

1. **CORS origin** — `Program.cs` hardcodes `WithOrigins("http://localhost:4200")`.
   The deployed Angular app has a different origin, so browser calls to the API
   get blocked until it's added. Make it configurable, e.g. read
   `Cors:AllowedOrigins` from config and add a `Cors__AllowedOrigins__0` env
   var on Render.

2. **HTTPS redirect behind the proxy** — `app.UseHttpsRedirection()` runs
   unconditionally. The `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` env var (set
   in the Dockerfile and `render.yaml`) makes ASP.NET trust Render's
   `X-Forwarded-Proto: https`, so it should see the request as already-HTTPS
   and not redirect-loop. If a redirect loop shows up, that's the first thing
   to check.

---

## 6. Redeploying later

- `autoDeploy: true` — pushing to `master` triggers a new build automatically.
- After adding EF migrations, re-run the `dotnet ef database update` from
  step 2 against Neon before (or right after) the deploy, since the app won't
  apply them itself.

---

## 7. Not verified yet

- The `Dockerfile` hasn't been built locally (no Docker installed here). The
  `dotnet publish FitRos.API -c Release` step inside it is the same command
  already used successfully for local runs; only the container assembly and
  the `$PORT` entrypoint are unproven. Worth a local
  `docker build -t fitros-api -f Dockerfile .` from `Backend/` before trusting
  the Render build.
- `render.yaml` keys follow the current Render Blueprint spec but haven't been
  applied to a real account yet.
