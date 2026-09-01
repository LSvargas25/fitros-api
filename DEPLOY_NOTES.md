# Deploying FitRos.API to Render + Neon

Written 2026-08-27 while prepping the deploy. **Nothing here has been run** —
no Render account, Neon project, or service was created. These are the exact
manual steps for you to do.

Same shape as Widget Finanzas: one web service on Render, Postgres on Neon,
the DB reached only through a connection-string env var, TLS handled by Render.
Widget Finanzas has no `render.yaml`/`Dockerfile` of its own (it's Python and
Render auto-detects that). .NET is not auto-detected, so this deploy uses the
`Dockerfile` + `render.yaml` added alongside this file.

Files added for the deploy:
- `Backend/Dockerfile` — multi-stage .NET 8 build, binds Kestrel to `$PORT`.
- `Backend/.dockerignore`
- `Backend/render.yaml` — Render Blueprint (web service + env var list).

---

## 0. Prerequisites / decisions still yours

- **Push the branch first.** Nothing is on GitHub yet from the 2026-08-27
  session. Run: `git push origin master feature/workout-diary`.
  Render deploys from GitHub, so it can't see any of this until you push.
- Free tier assumed (`plan: free` in `render.yaml`). Free web services spin
  down after ~15 min idle (first request after that takes ~30–50 s) and have
  **no shell tab** — that's why migrations below run from your machine.
- `region: oregon` in `render.yaml` — closest free US region. Change if you
  want Frankfurt/Singapore.

---

## 1. Create the Neon database

1. Go to https://neon.tech, sign in, **New Project**.
   - Name: `fitros` (anything).
   - Postgres version: 16 is fine.
   - Region: pick the one nearest the Render region you chose (Oregon → "US
     West").
2. After it's created, open **Dashboard → Connection Details**.
3. Copy the connection string. Neon gives you a URI like:
   ```
   postgresql://fitros_owner:npg_XXXXXXXX@ep-cool-name-12345.us-west-2.aws.neon.tech/fitros?sslmode=require
   ```
4. **Convert it to Npgsql keyword form** (Npgsql does not take the URI form).
   From the parts of the URI above:
   ```
   Host=ep-cool-name-12345.us-west-2.aws.neon.tech;Database=fitros;Username=fitros_owner;Password=npg_XXXXXXXX;SSL Mode=Require;Trust Server Certificate=true;Pooling=true
   ```
   - Use the **pooled** host if Neon shows one (has `-pooler` in it) — better
     for a small web service.
   - Keep `SSL Mode=Require`. `Trust Server Certificate=true` avoids shipping
     Neon's CA; drop it if you'd rather add the CA properly.
5. Keep this string — it's the `ConnectionStrings__DefaultConnection` value in
   step 3.

---

## 2. Apply the database schema to Neon (from your machine)

The API does **not** migrate on startup, and the free tier has no shell, so
run migrations locally against Neon **once** now and again whenever you add
migrations.

From `Backend/`:
```bash
# one-time: dotnet-ef is already used in this repo
dotnet tool install --global dotnet-ef   # skip if already installed

dotnet ef database update \
  -p FitRos.Infrastructure -s FitRos.API \
  --connection "Host=...;Database=fitros;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
```
Use the same connection string as step 1.4. When it finishes, the Neon DB has
all tables and the seeded OwnerApp user.

> Known issue carried over from `BACKEND_AUDIT.md` §6: `dotnet ef migrations
> add` still reports model drift (a seed-timestamp diff on the OwnerApp row).
> It does not affect `database update` — the schema applies fine.

---

## 3. Create the Render service

### Option A — Blueprint (uses `render.yaml`, recommended)
1. https://dashboard.render.com → **New → Blueprint**.
2. Connect the GitHub repo (`LSvargas25/fitros-api`) and the branch (`master`).
3. Render reads `render.yaml` and shows one service `fitros-api` with the env
   vars. It will prompt for every `sync: false` var — fill them in (step 3
   values below). `Jwt__SigningKey` is `generateValue: true`, leave it.
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

The app boots without the `Smtp__*` vars, but any flow that sends email
(password reset) will throw until they're set.

---

## 4. First deploy — what to expect

- Build takes a few minutes (SDK image pull + restore + publish).
- On success the log shows `Now listening on: http://+:10000` and Render marks
  it **Live** at `https://fitros-api.onrender.com` (or your chosen name).
- There is **no Swagger UI** in Production (Program.cs only maps Swagger when
  `IsDevelopment()`). Test with a real call instead, e.g.
  `POST https://fitros-api.onrender.com/api/auth/login`.
- Health checks: `render.yaml` sets no `healthCheckPath` because the API has no
  unauthenticated 200 route. See "Recommended code tweaks" below.

---

## 5. Recommended code tweaks (small, not applied here)

These are app-code changes, so they were left for you to make deliberately:

1. **Health endpoint** — in `FitRos.API/Program.cs`, after `app.MapControllers();`:
   ```csharp
   app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
   ```
   then set `healthCheckPath: /health` in `render.yaml`. Render restarts the
   instance automatically if this stops responding.

2. **CORS origin** — `Program.cs` hardcodes `WithOrigins("http://localhost:4200")`.
   The deployed Angular app has a different origin, so browser calls to the API
   will be blocked until you add it. Make it configurable, e.g. read
   `Cors:AllowedOrigins` from config and add a `Cors__AllowedOrigins__0` env
   var on Render.

3. **HTTPS redirect behind the proxy** — `app.UseHttpsRedirection()` runs
   unconditionally. The `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` env var (set
   in the Dockerfile and `render.yaml`) makes ASP.NET trust Render's
   `X-Forwarded-Proto: https`, so it should see the request as already-HTTPS
   and not redirect-loop. If you ever see redirect loops, that's the first
   thing to check.

---

## 6. Redeploying later

- `autoDeploy: true` — pushing to `master` triggers a new build automatically.
- After adding EF migrations, re-run the `dotnet ef database update` from
  step 2 against Neon **before** (or right after) the deploy, since the app
  won't apply them itself.

---

## 7. Not verified

- The `Dockerfile` was **not** built here (no Docker on this machine). The
  `dotnet publish FitRos.API -c Release` step inside it is the same command
  already run successfully for local testing; only the container assembly and
  the `$PORT` entrypoint are unproven. Do a local
  `docker build -t fitros-api -f Dockerfile .` from `Backend/` before relying
  on the Render build if you can.
- `render.yaml` keys follow the current Render Blueprint spec but haven't been
  applied to a real account.
