# Backend audit

Started 2026-08-26, while working autonomously on `feature/workout-diary` after
fixing the 13 failing tests (see commit `ad13f63`). This is a survey, not a todo
list that's been executed — items are only fixed elsewhere in this branch if the
corresponding commit says so.

## 1. Incomplete / suspicious code found

### Fixed this session (see later commits on this branch)
- `WeeklyTrainingPlan.Create` — broken stub overload + unused `CoachId` param.
  Fixed in commit `ad13f63` (already applied before this audit was written).

### Not fixed — flagged for awareness, low blast radius but touches multiple entities
- **`CreatedAt` property hiding (CS0108 build warning).** `WorkoutSession`,
  `User`, and `WeeklyTrainingPlan` each declare their own `public DateTime
  CreatedAt { get; private set; }`, which *hides* (not overrides)
  `AuditableEntity.CreatedAt` instead of reusing it. Functionally this seems to
  work today because `WorkoutSessionConfiguration` / etc. map `x.CreatedAt`
  against the derived (hiding) property and `FitRosDbContext.SaveChangesAsync`
  sets `entry.Property(nameof(IAuditableEntity.CreatedAt))` by name, which EF
  resolves to whichever property is actually mapped. But it means the base
  class's own `CreatedAt` is silently dead weight for those three entities, and
  it's fragile — a future refactor that removes the derived property, or code
  that reads `((IAuditableEntity)entity).CreatedAt` reflectively, could silently
  break. Not touched here because a fix means picking one property per entity
  and could ripple into EF configs/migrations; that's a decision worth doing
  deliberately, not as a drive-by.
- **`ClientProfile.SetModified(Guid)` hides `AuditableEntity.SetModified(Guid)`**
  (same CS0108 class of issue as above, same reasoning — not touched).
- ~~**`FitRosDbContext.Remove<TEntity>(TEntity)` hides `DbContext.Remove<TEntity>`**~~
  **Fixed further down this branch.** (CS0114). The derived method just does
  `Set<TEntity>().Remove(entity)`, matching base behavior, so hiding was safe -
  but `override` doesn't compile here (the base method returns
  `EntityEntry<TEntity>`, this one returns `void`, and return types must match
  to override). Added the `new` keyword instead, in both
  `FitRosDbContext.Remove` and the identical test-double hider in
  `GetMyClientsTests.cs`, to make the intentional hiding explicit and silence
  the warning without changing behavior.
- Two cosmetic naming issues, not fixed (renames ripple into imports/namespaces
  for no functional gain): the folder
  `FitRos.Application/Features/WorkoutRoutines/GetWorkoutToutineById` has a typo
  ("Toutine"), and `FitRos.Application/Features/ClientProfiles/ChangeClientStatus/ToggleClientStatusCommandHandler .cs`
  has a trailing space before `.cs` in the filename.
- Two `CS8714` nullability warnings in `AuthenticationBehavior`/
  `AuthorizationBehavior` (generic `TRequest` doesn't satisfy MediatR's
  `notnull` constraint cleanly). Pre-existing, cosmetic-only build warnings, not
  touched.

### Checked and NOT problems
- Every other `public static X Create(...)` factory in `FitRos.Domain` was read
  and cross-checked against its call sites — no other unused-parameter or
  overload-collision bugs like the `WeeklyTrainingPlan` one were found.
- No remaining `NotImplementedException`, `TODO`, `FIXME`, or `HACK` markers
  anywhere in the source tree (the only `NotImplementedException` was the
  `WeeklyTrainingPlan.Create` stub fixed in `ad13f63`).
- `WorkoutSession.Create(Guid gymId, Guid coachId, Guid clientId, string notes,
  int duration)` (line ~118) is the pre-existing bug flagged before this session
  started (semantically mismatched 5-arg overload forwarding into the 6-arg
  primary constructor with swapped meanings — `coachId`→`userId`,
  `clientId`→`routineId`, etc.). Confirmed again: not used by anything in the
  current test suite, so it doesn't currently cause a failure, but it's a latent
  trap for the next caller. Left alone per current instructions (no known
  failure depends on it); worth a dedicated fix + test if this session reaches
  point 4 with time to spare, or otherwise for a human to schedule.

## 2. WorkoutSession set editing/deletion

Before this session, `WorkoutSessionsController` only supported **adding** a
set (`POST /api/workout-sessions/{id}/sets`) — there was no way to correct a
typo'd rep count or remove a duplicate/mistaken set entry. Added
`UpdateSetInWorkoutSession` and `RemoveSetFromWorkoutSession` commands +
`PUT`/`DELETE /api/workout-sessions/{id}/sets/{setId}` endpoints, following the
same CQRS shape as `AddSetToWorkoutSession` (own ownership check, `IncludeSets()`
for the shadow `_sets` navigation, validator, tests). See the commit that
follows this one for details.

## 3. ClientKpiSnapshot / ClientProgressReportSnapshot

**Finding: neither is generated anywhere.** Searched the whole solution for
`ClientKpiSnapshot.Create(`, `ClientProgressReportSnapshot.Create(`, and any
`.Add(...)` calls against their `DbSet`s — zero matches outside the entity
files themselves. Both exist only as:
- domain entities (`FitRos.Domain/Entities/Analytics/ClientKpiSnapshot.cs`,
  `FitRos.Domain/Entities/Reports/ClientProgressReportSnapshot.cs`)
- EF configurations + migrations (tables exist in the DB schema)
- `DbSet<T>` properties on `FitRosDbContext` / `IFitRosDbContext`

No handler, no controller endpoint, no scheduled job, nothing in
`AddPhysicalMeasureCommandHandler` (the natural trigger point, since KPI deltas
are computed between physical measures) creates either one. There's also no
query/read side — no "get KPI history" or "get progress report" endpoint — so
right now these two tables are pure dead schema.

**`ClientKpiSnapshot` — implemented on-demand generation this session** (see
the commit after this one). It's a well-defined, low-risk shape: given a
`clientProfileId`, take the two most recent `PhysicalMeasure` rows, compute
`WeightDelta` / `BodyFatDelta` / `WaistDelta` against the prior one (null if
there's no prior measure), and persist a snapshot tied to the latest
`PhysicalMeasureId`. Added `GenerateClientKpiSnapshotCommand` +
`POST /api/client-profiles/{id}/kpi-snapshot` (mirrors the existing
`AddPhysicalMeasure` permission model: Owner/Admin any client in their gym,
Coach only their own assigned client).

**`ClientProgressReportSnapshot` — NOT implemented, needs a product decision.**
Its only real field is `reportJson` (a free-form string blob) — there's no
existing DTO, template, or hint anywhere in the codebase about what a "progress
report" should actually contain (which KPIs, over what time range, prose vs.
structured data, etc.). Building a generator would mean inventing that shape
without any product input, which the instructions for this session explicitly
said to avoid. **This needs Steven's input**: what should a client progress
report contain, and does it need to be product-facing (e.g. exported as a PDF)
or just an internal audit trail?

## 4. Other low-risk items picked up after point 3

- `FitRosDbContext.Remove<TEntity>` hiding warning — fixed, see section 1.
- Fixed `WorkoutSession.Create(gymId, coachId, clientId, notes, duration)` —
  see the dedicated commit for this. Same class of bug as
  `WeeklyTrainingPlan.Create`: it forwarded into the 6-arg primary factory with
  swapped meanings (`coachId` landing in `userId`, `clientId` landing in
  `routineId`, etc.) and was never used by anything, so it never surfaced as a
  test failure. Removed the overload rather than fixing its semantics, since
  the base 6-arg `Create` already exists and nothing calls the 5-arg one
  (confirmed via search) — a misleading, unused overload is worse than no
  overload. Added a regression test asserting `Create` requires all of
  `gymId`/`userId`/`routineId`/`routineName` to make sense.
- Added a read endpoint for a client's KPI snapshot history:
  `GET /api/client-profiles/{id}/kpi-snapshots`, pairing with the
  `GenerateClientKpiSnapshotCommand` write endpoint from section 3 (same
  permission model, ordered most-recent-first).

## 5. Still open — needs Steven's decision, not touched

- ~~`ClientProgressReportSnapshot` generation~~ — **RESOLVED**, see section 8.
  Steven supplied the report contents (Mi-Progreso metrics, month-over-month).

## 6. Session 2026-08-27 (autonomous) — warning cleanup done

Ran while Steven was out. Build + 445/445 tests green after every change; one
commit per fix on `feature/workout-diary`.

### Fixed
- **`ClientProfile.SetModified(Guid)` (CS0108)** — removed. The override body
  was byte-identical to `AuditableEntity.SetModified`; base setters are
  `protected` so the inherited method covers it. Commit `2abeac8`.
- **`CreatedAt` property-hiding on `WorkoutSession` / `User` /
  `WeeklyTrainingPlan` (CS0108 ×3)** — removed the derived shadows, entities
  now use `AuditableEntity.CreatedAt`. Verified schema-neutral: scaffolding a
  migration before vs. after the change produces the *same* (pre-existing)
  seed-timestamp diff and **no** column add/alter/drop for `CreatedAt` on any
  of the three tables. `builder.Property(x => x.CreatedAt)` and
  `entry.Property("CreatedAt")` resolve to the inherited property unchanged.
  Commit `1948e40`.
- **`CS8714` ×2** on `AuthenticationBehavior` / `AuthorizationBehavior` —
  added `where TRequest : notnull` (matches `ValidationBehavior`). Commit
  `81f4e05`.
- **Cosmetic naming** — folder `GetWorkoutToutineById` → `GetWorkoutRoutineById`;
  fixed the one namespace still carrying the "Toutine" typo
  (`WorkoutRoutineExerciseDetailsDto`) + dropped 2 dead `using`s that reached
  it; renamed `ToggleClientStatusCommandHandler .cs` / `...Validator .cs` to
  drop the space before `.cs`. Commit `faffa95`.

### Pre-existing model drift — FIXED (commit `fb33b24`)
`dotnet ef migrations has-pending-model-changes` reported drift even at
`c7019af` (before this session): `SeedOwner` in `FitRosDbContext` built the
`HasData` seed from a live `User.CreateOwnerApp(...)` whose constructor stamps
`CreatedAt = DateTime.UtcNow`, so the model differed from the snapshot on every
build.

`User.CreateOwnerApp` now takes an optional `DateTime? createdAtUtc` (null =
constructor default, keeps the 3 test call sites unchanged); `SeedOwner` passes
a constant `2024-01-01T00:00:00Z`. Migration `PinOwnerSeedCreatedAt` carries the
single `UpdateData`. Snapshot regen also refreshed two stale
`.ValueGeneratedOnAdd()` annotations (ExerciseSet.Id / WorkoutSession.Id) — no
schema impact, not in the migration `Up()`. `has-pending-model-changes` now
reports "No changes". 445/445 green.

### Remaining build warnings (6) — none in this session's scope
- `CS8981` ×2 — migration class `updateownerpasswordhash` is all-lowercase.
  Renaming a migration type ripples into `__EFMigrationsHistory` semantics;
  leave it.
- `CS0618` — `UseXminAsConcurrencyToken` is obsolete in the current Npgsql.
  It's a deliberate optimistic-concurrency choice; migrating to
  `IsRowVersion()`/`[Timestamp]` is a schema + behavior decision.
- `CS0105` ×2 — duplicate `using` in `FitRos.API/Middleware/GlobalExceptionMiddleware.cs`
  and `FitRos.API/Program.cs`. Trivial, but out of the requested scope.
- `ASP0019` — `FitRos.API/Controllers/UsersController.cs:116` uses
  `Headers.Add` instead of the indexer/`Append`. Trivial, out of scope.

### Not backed up to GitHub yet
`git fetch` works as of the 11:34 follow-up (credentials now resolve), but
`origin/master` is still `c30ed8f` and `origin/feature/workout-diary` still
`9613069` — nothing has been pushed. Local `master` and `feature/workout-diary`
both sit at `fb33b24` (0 behind origin, 19 / 11 ahead). Steven still needs to
run: `git push origin master feature/workout-diary`.

## 7. Session 2026-08-27 (cont.) — Nutrition module (RESOLVED)

Point "módulo de nutrición" is done. New CQRS module on `feature/workout-diary`,
mirroring the Exercises + WeeklyTrainingPlans patterns. Build + 471/471 tests
green; `has-pending-model-changes` clean.

### Food (catalog, tenant-scoped like `Exercise`)
- Entity `FitRos.Domain/Entities/Nutrition/Food.cs`: name, `FoodCategory`,
  macros **per 100 g** (`CaloriesPer100g` / `ProteinPer100g` / `CarbsPer100g` /
  `FatPer100g`), optional `ServingSizeGrams`, `IsArchived` soft-delete.
- CRUD: `POST/GET/{id}/GET/PUT/DELETE /api/foods` (`FoodsController`), CQRS
  handlers under `Features/Foods/`. List filters by category + `includeArchived`.
- **Design decision (spec said "por 100g o por porción"):** macros are stored
  per 100 g — the unambiguous unit for computing meal totals from a gram
  quantity — and `ServingSizeGrams` is an optional hint for the UI. If Steven
  wants per-serving entry in the UI, the frontend converts; the API stays
  per-100 g.

### MealPlan (aggregate, mirrors `WeeklyTrainingPlan` + `TrainingPlanDay`)
- `MealPlan` (Draft/Active/Archived) + child `MealPlanEntry`
  (`Day` × `MealType` × `FoodId` × `QuantityGrams`), unique on
  (plan, day, meal, food). "Assign a plan to a client" = the plan is created
  bound to a `ClientProfileId` (same model as training plans).
- Endpoints on `MealPlansController` (`api/meal-plans`): create; get by id (with
  per-entry + total macros computed on the fly from `Food`); list per client;
  get active per client; rename / activate / archive; add / update-quantity /
  remove entry.
- Permission model copied from WeeklyTrainingPlans (Owner any; Admin same-gym;
  Coach only their assigned client), factored into
  `Features/MealPlans/Common/MealPlanAccess`.
- **Consistent-with-training-plans quirk:** `Activate` does not auto-deactivate
  the client's other active plan (neither does `ActivatePlanHandler`).
  `GetActive*` just returns the first `Active` row. Left matching for parity;
  flag if single-active should be enforced for both modules.

### Migration
`20260827185510_AddNutritionModule` — creates `Foods`, `MealPlans`,
`MealPlanEntries`. Not yet applied to any database (`dotnet ef database update`).

## 8. Session 2026-08-27 (cont.) — Monthly progress report (RESOLVED)

Point "reporte mensual de progreso" is done, with the contents Steven
specified: the "Mi Progreso" metrics (weight, body-fat %, waist, completed
sets) for a month vs the previous month. Build + 478/478 green.

### Computed on the fly, not duplicated
`ProgressReportBuilder` (`Features/ClientProfiles/ProgressReport/Common/`)
derives everything from data that already exists:
- weight / body-fat / waist: latest `PhysicalMeasure` in the target month vs
  latest in the previous month; delta null if either month has no measure.
- completed sets: count of `ExerciseSet` rows under `WorkoutSession`s with
  `Status == Completed` and `ScheduledDate` in the month (loaded + counted in
  memory because `Sets` is a shadow `_sets` nav that can't be counted in SQL).
No new columns; nothing derived is stored on the read path.

### Endpoints (on `ClientProfilesController`)
- `GET  /api/client-profiles/{id}/progress-report?year=&month=` — live report,
  **not persisted**. Omit year+month for the current UTC month.
- `POST /api/client-profiles/{id}/progress-report?year=&month=` — computes the
  same and freezes it into a `ClientProgressReportSnapshot` (`ReportJson` =
  serialized report), anchored to a `PhysicalMeasure` (latest in the month,
  else latest overall; 400 if the client has none).
- `GET  /api/client-profiles/{id}/progress-reports` — persisted snapshot history.

Permission model (`ProgressReport/Common/ProgressReportAccess`): Owner any;
Admin same-gym; Coach only their assigned client; Client only their own
profile — same rule the KPI-snapshot handlers use inline.

### Deviation from the KPI-snapshot handlers (intentional)
The set-count query and the snapshot-history query call `IgnoreQueryFilters()`.
The tenant query filter is `GymId == currentUser.GymId` with no Owner bypass,
so without this an Owner (GymId null) would get 0 completed sets / an empty
history. Access is already checked by `ProgressReportAccess`, so ignoring the
tenant filter here is safe. `GetClientKpiSnapshotsHandler` does *not* do this
and has the same latent Owner limitation — worth aligning later.

No migration: `ClientProgressReportSnapshots` already existed as dead schema.

## 9. Session 2026-08-27 (cont.) — 4 items

Build + 485/485 tests green; `has-pending-model-changes` clean. One commit per item.

### 9.1 Auto-create ClientProfile on client creation (`76846f3`)
`POST /api/clients` (`CreateClientHandler`) already created the ClientProfile,
but across **two** `SaveChangesAsync` calls — a failure between them could leave
a Client user with no profile. Collapsed to a single transaction (User + empty
ClientProfile + notifications). `user.Id` is set in the ctor and there's no FK
from `ClientProfiles.UserId` to `Users`, so no ordering issue.

**Backfill decision — NOT needed.** Queried the local `fitros` DB:
`Users(Role=Client)` = 2, of which **0** lack a `ClientProfile`; 0 orphan
profiles. `CreateClientHandler` is the only production path that creates a
Client user. `CreateUserHandler` (generic, `roleToAssign` can be `Client`, does
**not** create a profile) has **zero production callers** — test-only; flagged
here so that if it's ever wired up it must also create the profile.

### 9.2 GetClientKpiSnapshotsHandler tenant-filter bug (`52cf9a1`)
Same defect §8 describes. The gym pre-check now exempts Owner
(`!IsOwner() && profile.GymId != currentUser.GymId`) and the
`ClientKpiSnapshots` query gets `.IgnoreQueryFilters()` (access already checked
by `ValidatePermissions`). `GenerateClientKpiSnapshotCommandHandler` has the
identical gym-pre-check line; **left untouched** — the item was scoped to the
read handler. Same one-line fix applies there if wanted.

### 9.3 [ProducesResponseType] vs real response (`1060968`)
- `WorkoutRoutinesController.Get` returned `PagedResult<T>`
  (`{page,pageSize,totalCount,items}`) but claimed `List<WorkoutRoutineListItem>`
  in both the attribute and the `ActionResult<>` signature — the Swagger
  contract that bit the frontend. Now `PagedResult<WorkoutRoutineListItem>`.
- `UsersController.GetUsersAdvanced` had a bare `[ProducesResponseType(200)]`
  (no schema) though it returns `CursorPagedResponse<UserListItemResponse>` —
  now typed.
- Audited **every** other `[ProducesResponseType(typeof(List<...>))]` endpoint
  (Admins, Coaches, Clients, Gyms, Notifications x2, Exercises, Foods,
  MealPlans, WeeklyTrainingPlans, WorkoutSessions, KPI snapshots,
  PhysicalMeasures, progress-report history, routine versions): every one of
  those handlers genuinely returns `List<T>` — annotations correct, untouched.
  `GetWorkoutRoutines` and `GetUsersAdvanced` were the only two that return an
  envelope.

### 9.4 WorkoutRoutine exercise composition — update targets (`dd11a28`)
Composition already existed: `AddExercise` / `MoveExercise` / `RemoveExercise`
on the `WorkoutRoutine` aggregate (all Draft-only via `EnsureDraftState()`),
exposed as `POST /{id}/exercises`, `PUT /{routineId}/exercises/{exerciseId}/move`,
`DELETE /{routineId}/exercises/{exerciseId}`. Add already carried
sets/reps/rest. **Missing:** editing an already-added exercise's target
sets/reps/rest without remove+re-add (which resets position). Added
`WorkoutRoutine.UpdateExercise(...)` + `UpdateExerciseInWorkoutRoutine`
command/handler/validator + `PUT /api/workoutroutines/{routineId}/exercises/{exerciseId}`.

**Pre-existing observation (not fixed):** all four routine-exercise endpoints
call their handler directly via `[FromServices]`, not MediatR, so the MediatR
`AuthenticationBehavior` / `AuthorizationBehavior` pipeline never runs for them,
and `WorkoutRoutinesController` has no `[Authorize]`. Effectively unauthenticated.
Systemic to this controller; worth a dedicated pass.

## 10. Session 2026-08-27 (cont.) — routine create bug + auth hole

Build + 491/491 green.

### 10.1 `POST /api/workoutroutines` → 400 "Nullable object must have a value." (`e5caef7`)
`CreateWorkoutRoutineHandler` did `_currentUser.GymId!.Value`. Any caller with
a null `GymId` (OwnerApp, or — see 10.2 — an unauthenticated request) hit an
unhandled `Nullable<T>.Value`, which `GlobalExceptionMiddleware` maps
(`InvalidOperationException => BadRequest`) and leaks the raw message. Replaced
with `?? throw new DomainException("You must be assigned to a gym to create a
workout routine.")`. The two `.Value` uses in `GetWorkoutRoutines`
(handler + validator) were checked — both properly `HasValue`-guarded.

Verified e2e on a running instance: Admin-in-a-gym creates a routine (201),
adds an exercise, updates its targets via the §9.4 endpoint, reads it back;
OwnerApp gets the clean 400.

### 10.2 WorkoutRoutinesController had no authentication (`602cce7`)
Confirmed live: with **no token**, `POST /api/workoutroutines` and the
add/move/remove/publish endpoints all executed (the MediatR auth behaviors
don't run for `[FromServices]` direct-handler calls, and there was no
`[Authorize]`). Added `[Authorize(Roles = "OwnerApp,Admin,Coach")]` on the
controller — all 8 endpoints now 401 without a token (verified live) and 403
for Client. `TestApiFactory` gained a `TestAuthHandler` so API tests still run
authenticated; a new test asserts 401 on every routine endpoint.

**Still open (security round):** these routine-exercise handlers do no per-gym
tenant check — an authenticated Admin/Coach from gym A can still act on gym B's
routine (`AddExerciseToWorkoutRoutineHandler` even calls `IgnoreQueryFilters()`).
`MoveExerciseInWorkoutRoutineHandler` opens a real DB transaction, which the
InMemory test provider can't honour. Both worth a dedicated pass.

### Dev instance
Restarted on `:7256` with this build (task in the session). E2E test data left
in the local DB for frontend verification of the exercise editor:
- gym "Test Gym Verify" (`84525303-…`)
- staff login `e2e.admin.<ts>@fitros.com` / `E2eAdmin123!` (Admin in that gym)
- routine "E2E Push Day" with one exercise "E2E Bench Press" (4×8, 120 s rest)
