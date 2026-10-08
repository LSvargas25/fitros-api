using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Notifications;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitRos.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a self-contained demo gym (one login per role plus the data every
/// screen needs) so the public deploy can be tried without asking for
/// credentials. Runs at boot only when <c>Seed:Demo</c> is true.
///
/// Idempotent:
/// - the gym has a fixed Id; its content (exercises, routines, plans, foods,
///   meal plans, measures, sessions, notifications) is created only together
///   with the gym, in a single SaveChanges, so it is all-or-nothing;
/// - demo users are matched by email. On every run a missing one is recreated
///   and an existing one is "repaired" (re-activated, password reset to
///   <see cref="DemoPassword"/>) so visitors can't lock the next visitor out.
/// </summary>
public sealed class DemoDataSeeder
{
    public const string DemoPassword = "FitRos#2026";

    public const string OwnerEmail = "owner@fitros.demo";
    public const string AdminEmail = "admin@fitros.demo";
    public const string CoachEmail = "coach@fitros.demo";
    public const string ClientEmail = "client@fitros.demo";

    public static readonly Guid DemoGymId = Guid.Parse("de305d54-75b4-431b-adb2-eb6b9e546014");

    private const string GymName = "Iron Valley Fitness";

    private sealed record DemoUser(
        string Email,
        string FirstName,
        string LastName,
        UserRole Role,
        string? CoachEmail = null);

    // The first four are the advertised logins; the rest give the coaches a
    // real roster. All share DemoPassword.
    private static readonly DemoUser[] Users =
    {
        new(OwnerEmail, "Olivia", "Rojas", UserRole.OwnerApp),
        new(AdminEmail, "Mateo", "Vargas", UserRole.Admin),
        new(CoachEmail, "Andrea", "Solís", UserRole.Coach),
        new(ClientEmail, "Lucía", "Mora", UserRole.Client, CoachEmail),

        new("coach2@fitros.demo", "Diego", "Castro", UserRole.Coach),
        new("carlos.jimenez@fitros.demo", "Carlos", "Jiménez", UserRole.Client, CoachEmail),
        new("valeria.chaves@fitros.demo", "Valeria", "Chaves", UserRole.Client, CoachEmail),
        new("sofia.araya@fitros.demo", "Sofía", "Araya", UserRole.Client, CoachEmail),
        new("javier.quesada@fitros.demo", "Javier", "Quesada", UserRole.Client, CoachEmail),
        new("daniela.perez@fitros.demo", "Daniela", "Pérez", UserRole.Client, "coach2@fitros.demo"),
        new("andres.brenes@fitros.demo", "Andrés", "Brenes", UserRole.Client, "coach2@fitros.demo"),
    };

    public static IReadOnlyList<string> AllEmails { get; } = Users.Select(u => u.Email).ToArray();

    private readonly FitRosDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DemoDataSeeder> _logger;
    private readonly DateTime _today;

    public DemoDataSeeder(
        FitRosDbContext db,
        IPasswordHasher hasher,
        ILogger<DemoDataSeeder>? logger = null)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger ?? NullLogger<DemoDataSeeder>.Instance;
        _today = DateTime.UtcNow.Date;
    }

    public async Task<DemoSeedResult> SeedAsync(CancellationToken ct = default)
    {
        var gym = await _db.Gyms
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == DemoGymId, ct);

        var gymCreated = false;

        if (gym is null)
        {
            gym = Gym.CreateSeeded(
                DemoGymId,
                GymName,
                "Avenida Escazú, Torre 2, San José, Costa Rica",
                "+506 2222-2026");
            _db.Gyms.Add(gym);
            gymCreated = true;
        }
        else if (gym.IsDeleted)
        {
            _logger.LogWarning("Demo gym {GymId} was soft-deleted; demo users are repaired but its content is not re-created.", DemoGymId);
        }
        else
        {
            gym.Activate();
        }

        var passwordHash = _hasher.Hash(DemoPassword);
        var users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        int created = 0, repaired = 0;

        foreach (var spec in Users)
        {
            var normalized = spec.Email.ToUpperInvariant();
            var user = await _db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

            if (user is null)
            {
                user = spec.Role == UserRole.OwnerApp
                    ? User.CreateOwnerApp(Guid.NewGuid(), spec.Email, spec.FirstName, spec.LastName, passwordHash)
                    : User.CreateForGym(DemoGymId, spec.Email, spec.FirstName, spec.LastName, passwordHash, spec.Role);
                user.ClearDomainEvents();
                _db.Users.Add(user);
                created++;
            }
            else if (Repair(user, passwordHash))
            {
                repaired++;
            }

            users[spec.Email] = user;
        }

        // Client profiles: created for any demo client that doesn't have one
        // (a fresh seed, or a client user that had to be re-created).
        var profiles = new Dictionary<string, ClientProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in Users.Where(u => u.Role == UserRole.Client))
        {
            var user = users[spec.Email];
            var profile = await _db.ClientProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

            if (profile is null)
            {
                profile = ClientProfile.Create(DemoGymId, user.Id, users[spec.CoachEmail!].Id);
                _db.ClientProfiles.Add(profile);
            }
            else if (profile.Status == ClientStatus.Inactive)
            {
                profile.Activate();
            }

            profiles[spec.Email] = profile;
        }

        if (gymCreated)
            SeedGymContent(users, profiles);

        foreach (var aggregate in _db.ChangeTracker.Entries<AggregateRoot>())
            aggregate.Entity.ClearDomainEvents();

        await _db.SaveChangesAsync(ct);

        var result = new DemoSeedResult(gymCreated, created, repaired);
        _logger.LogInformation(
            "Demo seed done: gym created={GymCreated}, users created={Created}, users repaired={Repaired}",
            result.GymCreated, result.UsersCreated, result.UsersRepaired);

        return result;
    }

    private bool Repair(User user, string passwordHash)
    {
        var changed = false;

        if (user.Status != UserStatus.Active)
        {
            user.Activate();
            changed = true;
        }

        if (!user.EmailVerified)
        {
            user.MarkEmailVerified();
            changed = true;
        }

        if (!_hasher.Verify(DemoPassword, user.PasswordHash))
        {
            user.ChangePasswordHash(passwordHash);
            changed = true;
        }

        return changed;
    }

    // =========================================================
    // Gym content - only on the run that creates the gym
    // =========================================================

    private void SeedGymContent(
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, ClientProfile> profiles)
    {
        var exercises = SeedExercises();
        var routines = SeedRoutines(exercises);
        var foods = SeedFoods();

        var andrea = users[CoachEmail];
        var diego = users["coach2@fitros.demo"];

        // Lucía (the demo client login) trains every day, so "today's workout"
        // always has something to show whatever day a visitor drops by.
        var lucia = profiles[ClientEmail];
        var luciaPlan = WeeklyTrainingPlan.Create(lucia.Id, andrea.Id, DemoGymId, "Push / Pull / Legs — Lucía");
        luciaPlan.AssignRoutineToDay(DayOfWeek.Monday, routines["push"].Id, "Enfócate en la técnica del press.");
        luciaPlan.AssignRoutineToDay(DayOfWeek.Tuesday, routines["pull"].Id);
        luciaPlan.AssignRoutineToDay(DayOfWeek.Wednesday, routines["legs"].Id, "Sube 2,5 kg en sentadilla si completas todas las series.");
        luciaPlan.AssignRoutineToDay(DayOfWeek.Thursday, routines["push"].Id);
        luciaPlan.AssignRoutineToDay(DayOfWeek.Friday, routines["pull"].Id);
        luciaPlan.AssignRoutineToDay(DayOfWeek.Saturday, routines["legs"].Id);
        luciaPlan.AssignRoutineToDay(DayOfWeek.Sunday, routines["fullbody"].Id, "Sesión ligera: 60 % del peso habitual.");
        luciaPlan.Activate();
        _db.WeeklyTrainingPlans.Add(luciaPlan);

        foreach (var email in new[] { "carlos.jimenez@fitros.demo", "valeria.chaves@fitros.demo", "sofia.araya@fitros.demo", "javier.quesada@fitros.demo" })
        {
            var plan = WeeklyTrainingPlan.Create(profiles[email].Id, andrea.Id, DemoGymId, $"Push / Pull / Legs — {users[email].FirstName}");
            plan.AssignRoutineToDay(DayOfWeek.Monday, routines["push"].Id);
            plan.AssignRoutineToDay(DayOfWeek.Wednesday, routines["pull"].Id);
            plan.AssignRoutineToDay(DayOfWeek.Friday, routines["legs"].Id);
            plan.Activate();
            _db.WeeklyTrainingPlans.Add(plan);
        }

        foreach (var email in new[] { "daniela.perez@fitros.demo", "andres.brenes@fitros.demo" })
        {
            var plan = WeeklyTrainingPlan.Create(profiles[email].Id, diego.Id, DemoGymId, $"Full body 3 días — {users[email].FirstName}");
            plan.AssignRoutineToDay(DayOfWeek.Monday, routines["fullbody"].Id);
            plan.AssignRoutineToDay(DayOfWeek.Wednesday, routines["fullbody"].Id);
            plan.AssignRoutineToDay(DayOfWeek.Friday, routines["fullbody"].Id);
            plan.Activate();
            _db.WeeklyTrainingPlans.Add(plan);
        }

        // A draft plan, so the coach can see the Draft -> Active lifecycle.
        var draft = WeeklyTrainingPlan.Create(profiles["sofia.araya@fitros.demo"].Id, andrea.Id, DemoGymId, "Bloque de fuerza (borrador)");
        draft.AssignRoutineToDay(DayOfWeek.Tuesday, routines["legs"].Id);
        _db.WeeklyTrainingPlans.Add(draft);

        SeedMealPlan(lucia, andrea, foods, "Plan de recomposición — Lucía", activate: true);
        SeedMealPlan(profiles["carlos.jimenez@fitros.demo"], andrea, foods, "Volumen limpio — Carlos", activate: true);

        // Measures: a gentle recomposition trend, oldest first.
        SeedMeasures(lucia, startWeight: 68.4m, weeklyWeight: -0.35m, startFat: 27.5m, weeklyFat: -0.45m, points: 7);
        SeedMeasures(profiles["carlos.jimenez@fitros.demo"], 78.0m, 0.30m, 18.0m, -0.10m, 6);
        SeedMeasures(profiles["valeria.chaves@fitros.demo"], 61.2m, -0.20m, 24.0m, -0.30m, 5);
        SeedMeasures(profiles["sofia.araya@fitros.demo"], 57.5m, 0.10m, 22.5m, -0.15m, 4);
        SeedMeasures(profiles["javier.quesada@fitros.demo"], 92.3m, -0.60m, 29.0m, -0.50m, 6);
        SeedMeasures(profiles["daniela.perez@fitros.demo"], 64.0m, -0.25m, 26.0m, -0.20m, 3);
        SeedMeasures(profiles["andres.brenes@fitros.demo"], 84.5m, -0.40m, 21.0m, -0.25m, 3);

        SeedWorkoutHistory(users[ClientEmail], luciaPlan, routines, exercises, weeks: 4);
        SeedWorkoutHistory(users["carlos.jimenez@fitros.demo"], null, routines, exercises, weeks: 3);

        SeedNotifications(users, profiles);
    }

    private Dictionary<string, Exercise> SeedExercises()
    {
        var specs = new (string Key, string Name, string Description, MuscleGroup Group)[]
        {
            ("bench", "Press de banca", "Barra al pecho con escápulas retraídas y pies firmes en el suelo.", MuscleGroup.Chest),
            ("incline", "Press inclinado con mancuernas", "Banco a 30°; baja controlando hasta la línea del pecho.", MuscleGroup.Chest),
            ("pullup", "Dominadas", "Agarre prono a la anchura de los hombros; sube hasta pasar la barbilla.", MuscleGroup.Back),
            ("row", "Remo con barra", "Torso a 45°, lleva la barra al ombligo sin balancear.", MuscleGroup.Back),
            ("squat", "Sentadilla trasera", "Barra alta, rodillas siguiendo la punta de los pies, profundidad paralela.", MuscleGroup.Legs),
            ("rdl", "Peso muerto rumano", "Bisagra de cadera con rodillas semiflexionadas y espalda neutra.", MuscleGroup.Legs),
            ("lunge", "Zancadas caminando", "Pasos largos, rodilla trasera cerca del suelo.", MuscleGroup.Legs),
            ("ohp", "Press militar", "De pie, glúteos y abdomen activos, barra en línea con la cara.", MuscleGroup.Shoulders),
            ("lateral", "Elevaciones laterales", "Mancuernas hasta la altura de los hombros, codos ligeramente flexionados.", MuscleGroup.Shoulders),
            ("curl", "Curl de bíceps con barra", "Codos pegados al torso, sin impulso.", MuscleGroup.Arms),
            ("triceps", "Extensión de tríceps en polea", "Codos fijos, extensión completa abajo.", MuscleGroup.Arms),
            ("plank", "Plancha", "Línea recta de hombros a talones; se mide en repeticiones de 10 s.", MuscleGroup.Core),
            ("burpee", "Burpees", "Movimiento continuo: sentadilla, plancha, flexión y salto.", MuscleGroup.FullBody),
        };

        var map = new Dictionary<string, Exercise>();
        foreach (var s in specs)
        {
            var exercise = Exercise.Create(s.Name, s.Description, s.Group, DemoGymId);
            _db.Exercises.Add(exercise);
            map[s.Key] = exercise;
        }

        return map;
    }

    private Dictionary<string, WorkoutRoutine> SeedRoutines(IReadOnlyDictionary<string, Exercise> ex)
    {
        WorkoutRoutine Build(string name, string description, bool publish, params (string Key, int Sets, int Reps, int RestSeconds)[] items)
        {
            var routine = WorkoutRoutine.Create(DemoGymId, name, description);
            var order = 1;
            foreach (var item in items)
                routine.AddExercise(ex[item.Key].Id, order++, item.Sets, item.Reps, item.RestSeconds);
            if (publish)
                routine.Publish();
            _db.WorkoutRoutines.Add(routine);
            return routine;
        }

        var map = new Dictionary<string, WorkoutRoutine>
        {
            ["push"] = Build("Empuje (Push)", "Pecho, hombros y tríceps.", true,
                ("bench", 4, 8, 120), ("incline", 3, 10, 90), ("ohp", 3, 8, 90), ("lateral", 3, 15, 60), ("triceps", 3, 12, 60)),
            ["pull"] = Build("Tirón (Pull)", "Espalda y bíceps.", true,
                ("pullup", 4, 6, 120), ("row", 4, 8, 90), ("curl", 3, 12, 60), ("plank", 3, 4, 45)),
            ["legs"] = Build("Pierna", "Cuádriceps, isquiotibiales y glúteos.", true,
                ("squat", 4, 6, 150), ("rdl", 3, 8, 120), ("lunge", 3, 12, 90), ("plank", 3, 4, 45)),
            ["fullbody"] = Build("Full body principiante", "Patrones básicos para empezar o descargar.", true,
                ("squat", 3, 10, 90), ("bench", 3, 10, 90), ("row", 3, 10, 90), ("burpee", 2, 10, 60)),
        };

        // A draft, so the routine editor has something still editable.
        Build("Hipertrofia avanzada (borrador)", "Bloque de 6 semanas en preparación.", false,
            ("incline", 4, 10, 75), ("lateral", 4, 15, 45));

        return map;
    }

    private Dictionary<string, Food> SeedFoods()
    {
        var specs = new (string Key, string Name, FoodCategory Cat, decimal Kcal, decimal P, decimal C, decimal F, decimal? Serving)[]
        {
            ("oats", "Avena en hojuelas", FoodCategory.Carbohydrate, 389m, 16.9m, 66.3m, 6.9m, 40m),
            ("eggs", "Huevo entero", FoodCategory.Protein, 155m, 13m, 1.1m, 11m, 50m),
            ("chicken", "Pechuga de pollo", FoodCategory.Protein, 165m, 31m, 0m, 3.6m, 150m),
            ("rice", "Arroz blanco cocido", FoodCategory.Carbohydrate, 130m, 2.7m, 28m, 0.3m, 150m),
            ("beans", "Frijoles negros cocidos", FoodCategory.Protein, 132m, 8.9m, 23.7m, 0.5m, 120m),
            ("broccoli", "Brócoli", FoodCategory.Vegetable, 34m, 2.8m, 6.6m, 0.4m, 100m),
            ("banana", "Banano", FoodCategory.Fruit, 89m, 1.1m, 22.8m, 0.3m, 120m),
            ("yogurt", "Yogur griego natural", FoodCategory.Dairy, 97m, 9m, 3.9m, 5m, 170m),
            ("avocado", "Aguacate", FoodCategory.Fat, 160m, 2m, 8.5m, 14.7m, 70m),
            ("salmon", "Salmón", FoodCategory.Protein, 208m, 20m, 0m, 13m, 150m),
            ("whey", "Proteína whey", FoodCategory.Supplement, 400m, 80m, 8m, 6m, 30m),
        };

        var map = new Dictionary<string, Food>();
        foreach (var s in specs)
        {
            var food = Food.Create(s.Name, s.Cat, s.Kcal, s.P, s.C, s.F, s.Serving, DemoGymId);
            _db.Foods.Add(food);
            map[s.Key] = food;
        }

        return map;
    }

    private void SeedMealPlan(
        ClientProfile client,
        User coach,
        IReadOnlyDictionary<string, Food> food,
        string name,
        bool activate)
    {
        var plan = MealPlan.Create(client.Id, coach.Id, DemoGymId, name);

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var fish = day is DayOfWeek.Tuesday or DayOfWeek.Friday;

            plan.AddEntry(day, MealType.Breakfast, food["oats"].Id, 50m);
            plan.AddEntry(day, MealType.Breakfast, food["eggs"].Id, 100m);
            plan.AddEntry(day, MealType.Breakfast, food["banana"].Id, 120m);

            plan.AddEntry(day, MealType.Lunch, fish ? food["salmon"].Id : food["chicken"].Id, 150m);
            plan.AddEntry(day, MealType.Lunch, food["rice"].Id, 150m);
            plan.AddEntry(day, MealType.Lunch, food["beans"].Id, 100m);
            plan.AddEntry(day, MealType.Lunch, food["broccoli"].Id, 100m);

            plan.AddEntry(day, MealType.Snack, food["yogurt"].Id, 170m);
            plan.AddEntry(day, MealType.Snack, food["whey"].Id, 30m);

            plan.AddEntry(day, MealType.Dinner, food["chicken"].Id, 120m);
            plan.AddEntry(day, MealType.Dinner, food["avocado"].Id, 70m);
            plan.AddEntry(day, MealType.Dinner, food["broccoli"].Id, 150m);
        }

        if (activate)
            plan.Activate();

        _db.MealPlans.Add(plan);
    }

    private void SeedMeasures(
        ClientProfile client,
        decimal startWeight,
        decimal weeklyWeight,
        decimal startFat,
        decimal weeklyFat,
        int points)
    {
        for (var i = 0; i < points; i++)
        {
            // Every two weeks, the most recent one a few days ago.
            var weeks = i * 2m;
            client.AddMeasure(
                weight: startWeight + weeklyWeight * weeks,
                bodyFatPercentage: startFat + weeklyFat * weeks,
                muscleMass: Math.Round(startWeight * 0.42m + 0.12m * weeks, 2),
                waist: Math.Round(startWeight * 1.08m + weeklyWeight * 0.8m * weeks, 2),
                chest: Math.Round(startWeight * 1.35m + 0.05m * weeks, 2),
                arms: Math.Round(startWeight * 0.42m + 0.03m * weeks, 2));
        }

        var measures = client.Measures.ToList();
        for (var i = 0; i < measures.Count; i++)
        {
            var recordedAt = _today.AddDays(-3 - 14 * (measures.Count - 1 - i)).AddHours(7);
            _db.Entry(measures[i]).Property(nameof(PhysicalMeasure.RecordedAt)).CurrentValue = recordedAt;
        }
    }

    private void SeedWorkoutHistory(
        User user,
        WeeklyTrainingPlan? plan,
        IReadOnlyDictionary<string, WorkoutRoutine> routines,
        IReadOnlyDictionary<string, Exercise> exercises,
        int weeks)
    {
        // Starting loads per exercise; each week adds a little to show progress.
        var baseLoad = new Dictionary<Guid, decimal>
        {
            [exercises["bench"].Id] = 32.5m, [exercises["incline"].Id] = 12m,
            [exercises["pullup"].Id] = 0m, [exercises["row"].Id] = 30m,
            [exercises["squat"].Id] = 45m, [exercises["rdl"].Id] = 40m,
            [exercises["lunge"].Id] = 10m, [exercises["ohp"].Id] = 22.5m,
            [exercises["lateral"].Id] = 5m, [exercises["curl"].Id] = 15m,
            [exercises["triceps"].Id] = 15m, [exercises["plank"].Id] = 0m,
            [exercises["burpee"].Id] = 0m,
        };

        var routineById = routines.Values.ToDictionary(r => r.Id);

        for (var daysAgo = weeks * 7; daysAgo >= 1; daysAgo--)
        {
            var date = _today.AddDays(-daysAgo);

            WorkoutRoutine? routine;
            if (plan is not null)
            {
                var day = plan.Days.FirstOrDefault(d => d.Day == date.DayOfWeek);
                routine = day is null ? null : routineById[day.WorkoutRoutineId];
            }
            else
            {
                routine = date.DayOfWeek switch
                {
                    DayOfWeek.Monday => routines["push"],
                    DayOfWeek.Wednesday => routines["pull"],
                    DayOfWeek.Friday => routines["legs"],
                    _ => null
                };
            }

            if (routine is null)
                continue;

            var session = WorkoutSession.Create(DemoGymId, user.Id, routine.Id, routine.Name, routine.Version, date.AddHours(12));

            // Roughly one missed session a week keeps the history believable.
            if (daysAgo % 6 == 0)
            {
                session.Skip();
                _db.WorkoutSessions.Add(session);
                continue;
            }

            session.Start();
            var progress = (weeks * 7 - daysAgo) / 7 * 2.5m;
            foreach (var item in routine.Exercises.OrderBy(e => e.Order))
            {
                var load = baseLoad[item.ExerciseId];
                var weight = load == 0 ? 0 : load + progress;
                for (var set = 1; set <= item.SuggestedSets; set++)
                    session.AddSet(item.ExerciseId, set, Math.Max(1, item.SuggestedReps - (set == item.SuggestedSets ? 1 : 0)), weight);
            }
            session.Complete();
            _db.WorkoutSessions.Add(session);
        }
    }

    private void SeedNotifications(
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, ClientProfile> profiles)
    {
        void Add(string email, string title, string message, NotificationType type, int hoursAgo, bool read, Guid? reference = null)
        {
            var user = users[email];
            var gymId = user.Role == UserRole.OwnerApp ? (Guid?)null : DemoGymId;
            var n = Notification.Create(user.Id, gymId, title, message, type, reference);
            if (read)
                n.MarkAsRead();
            _db.Notifications.Add(n);
            _db.Entry(n).Property(nameof(Notification.CreatedAt)).CurrentValue = DateTime.UtcNow.AddHours(-hoursAgo);
        }

        Add(OwnerEmail, "Nuevo gimnasio en la plataforma", $"{GymName} completó su registro en FitRos.", NotificationType.GymCreated, 24 * 30, read: true, DemoGymId);
        Add(OwnerEmail, "Administrador asignado", $"Mateo Vargas ahora administra {GymName}.", NotificationType.AdminAssignedToGym, 24 * 29, read: false, DemoGymId);

        Add(AdminEmail, "Te asignaron un gimnasio", $"Ahora administras {GymName}.", NotificationType.AdminAssignedToGym, 24 * 29, read: true, DemoGymId);
        Add(AdminEmail, "Nuevo coach", "Andrea Solís se unió al equipo de coaches.", NotificationType.CoachCreated, 24 * 28, read: true);
        Add(AdminEmail, "Nuevo coach", "Diego Castro se unió al equipo de coaches.", NotificationType.CoachCreated, 24 * 27, read: false);
        Add(AdminEmail, "Nuevo cliente", "Javier Quesada se registró en el gimnasio.", NotificationType.ClientCreated, 30, read: false);

        Add(CoachEmail, "Cliente asignado", "Lucía Mora es ahora tu clienta.", NotificationType.ClientAssigned, 24 * 26, read: true, profiles[ClientEmail].Id);
        Add(CoachEmail, "Cliente asignado", "Javier Quesada es ahora tu cliente.", NotificationType.ClientAssigned, 28, read: false, profiles["javier.quesada@fitros.demo"].Id);
        Add(CoachEmail, "Rutina publicada", "\"Pierna\" ya está disponible para asignar.", NotificationType.RoutinePublished, 24 * 20, read: true);

        Add(ClientEmail, "Nuevo plan de entrenamiento", "Andrea te asignó \"Push / Pull / Legs — Lucía\".", NotificationType.RoutineAssigned, 24 * 25, read: true);
        Add(ClientEmail, "Entrenamiento de hoy", "Tienes una sesión programada para hoy. ¡A darle!", NotificationType.WorkoutScheduled, 2, read: false);
    }
}

public sealed record DemoSeedResult(bool GymCreated, int UsersCreated, int UsersRepaired);
