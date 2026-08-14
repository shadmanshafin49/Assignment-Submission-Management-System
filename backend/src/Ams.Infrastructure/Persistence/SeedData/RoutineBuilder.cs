using Ams.Domain.Entities;
using Ams.Domain.Enums;

namespace Ams.Infrastructure.Persistence.SeedData;

/// <summary>
/// Builds a conflict-free weekly routine for every class.
/// <para>
/// Writing 108 timetable rows by hand and hoping no teacher ends up in two rooms at once is how
/// seed data quietly becomes wrong, so the routine is solved rather than typed. The solver is a
/// randomised greedy with restarts over a fixed seed, which makes the output identical on every
/// run — the same database from the same code, every time — and the result is verified before it
/// is returned, so a broken routine fails the seeder instead of reaching the UI.
/// </para>
/// </summary>
public static class RoutineBuilder
{
    /// <summary>One placement: a course in a class's (day, period) slot.</summary>
    public record Placement(Guid ClassRoomId, Guid CourseId, WeekDay Day, int PeriodIndex);

    /// <summary>What the solver needs to know about one course.</summary>
    public record CourseSlot(
        Guid CourseId,
        Guid ClassRoomId,
        string SubjectCode,
        Guid? TeacherId,
        int WeeklyPeriods,
        FaithGroup? FaithGroup);

    private static readonly WeekDay[] Days = Enum.GetValues<WeekDay>();

    public static IReadOnlyList<Placement> Build(IReadOnlyList<CourseSlot> courses, int seed = 20260813)
    {
        // Faith-group courses of the same class share their periods: the Muslim and Hindu groups
        // are taught at the same time in different rooms. They are solved as one demand and then
        // expanded, which is both how the school runs it and what keeps the period arithmetic
        // honest — religion costs a class three periods, not six.
        var groups = BuildDemands(courses);

        for (var attempt = 0; attempt < 200; attempt++)
        {
            var rng = new Random(seed + attempt);
            if (TrySolve(groups, rng, out var placements))
            {
                Verify(placements, courses);
                return placements;
            }
        }

        throw new InvalidOperationException(
            "Could not build a conflict-free routine. Check the teacher loads in People.Staffing — "
            + "a teacher may be timetabled for more than 36 periods, or two classes may need the "
            + "same teacher for more periods than the week has slots.");
    }

    // ---------------------------------------------------------------- solving

    /// <summary>A unit of demand: one subject slot for one class, worth N periods a week.</summary>
    private record Demand(
        Guid ClassRoomId,
        string SubjectCode,
        int Periods,
        IReadOnlyList<CourseSlot> Courses)
    {
        public IEnumerable<Guid> TeacherIds => Courses.Where(c => c.TeacherId is not null)
            .Select(c => c.TeacherId!.Value);
    }

    private static List<Demand> BuildDemands(IReadOnlyList<CourseSlot> courses)
    {
        var demands = new List<Demand>();

        foreach (var byClass in courses.GroupBy(c => c.ClassRoomId))
        {
            // Every faith-group course of a class collapses into a single "religion" demand.
            var faithCourses = byClass.Where(c => c.FaithGroup is not null).ToList();

            if (faithCourses.Count > 0)
            {
                demands.Add(new Demand(
                    byClass.Key,
                    "religion",
                    faithCourses.Max(c => c.WeeklyPeriods),
                    faithCourses));
            }

            foreach (var course in byClass.Where(c => c.FaithGroup is null))
                demands.Add(new Demand(byClass.Key, course.SubjectCode, course.WeeklyPeriods, [course]));
        }

        return demands;
    }

    private static bool TrySolve(
        List<Demand> demands, Random rng, out List<Placement> placements)
    {
        placements = [];

        // (teacherId, day, period) → taken. This is the constraint no database index can express,
        // because it spans classes.
        var teacherBusy = new HashSet<(Guid Teacher, WeekDay Day, int Period)>();

        foreach (var byClass in demands.GroupBy(d => d.ClassRoomId))
        {
            var remaining = byClass.ToDictionary(d => d.SubjectCode, d => d.Periods);
            var perDay = new Dictionary<(WeekDay, string), int>();
            var lookup = byClass.ToDictionary(d => d.SubjectCode);

            foreach (var day in Days)
            {
                for (var period = 1; period <= PeriodSchedule.PeriodsPerDay; period++)
                {
                    // Prefer subjects with the most periods still to place, so the heavy ones do
                    // not get squeezed into the last few slots; break ties randomly so restarts
                    // actually explore a different arrangement.
                    var candidates = remaining
                        .Where(kv => kv.Value > 0)
                        .Where(kv => perDay.GetValueOrDefault((day, kv.Key)) == 0)
                        .Where(kv => lookup[kv.Key].TeacherIds
                            .All(t => !teacherBusy.Contains((t, day, period))))
                        .OrderByDescending(kv => kv.Value)
                        .ThenBy(_ => rng.Next())
                        .ToList();

                    if (candidates.Count == 0)
                        return false;

                    var chosen = candidates[0].Key;
                    var demand = lookup[chosen];

                    foreach (var course in demand.Courses)
                    {
                        placements.Add(new Placement(
                            demand.ClassRoomId, course.CourseId, day, period));
                    }

                    foreach (var teacher in demand.TeacherIds)
                        teacherBusy.Add((teacher, day, period));

                    remaining[chosen]--;
                    perDay[(day, chosen)] = 1;
                }
            }

            // Every slot filled means every demand consumed; if not, this attempt is unusable.
            if (remaining.Values.Any(v => v != 0))
                return false;
        }

        return true;
    }

    // --------------------------------------------------------------- checking

    /// <summary>
    /// Re-derives the invariants from the finished routine rather than trusting the solver:
    /// every class fully timetabled, no teacher double-booked, and each course given exactly the
    /// weekly period count NCTB specifies.
    /// </summary>
    private static void Verify(
        IReadOnlyList<Placement> placements, IReadOnlyList<CourseSlot> courses)
    {
        var byCourse = courses.ToDictionary(c => c.CourseId);

        foreach (var byClass in placements.GroupBy(p => p.ClassRoomId))
        {
            var slots = byClass
                .Select(p => (p.Day, p.PeriodIndex))
                .Distinct()
                .Count();

            if (slots != PeriodSchedule.PeriodsPerWeek)
            {
                throw new InvalidOperationException(
                    $"Class {byClass.Key} has {slots} timetabled periods, expected "
                    + $"{PeriodSchedule.PeriodsPerWeek}.");
            }

            // A slot may hold more than one course only when they are parallel faith streams.
            foreach (var slot in byClass.GroupBy(p => (p.Day, p.PeriodIndex)).Where(g => g.Count() > 1))
            {
                var faiths = slot.Select(p => byCourse[p.CourseId].FaithGroup).ToList();

                if (faiths.Any(f => f is null) || faiths.Distinct().Count() != faiths.Count)
                {
                    throw new InvalidOperationException(
                        $"Class {byClass.Key} is double-booked on {slot.Key.Day} "
                        + $"period {slot.Key.PeriodIndex}.");
                }
            }
        }

        var clash = placements
            .Select(p => (Teacher: byCourse[p.CourseId].TeacherId, p.Day, p.PeriodIndex))
            .Where(x => x.Teacher is not null)
            .GroupBy(x => x)
            .FirstOrDefault(g => g.Count() > 1);

        if (clash is not null)
        {
            throw new InvalidOperationException(
                $"Teacher {clash.Key.Teacher} is timetabled twice on {clash.Key.Day} "
                + $"period {clash.Key.PeriodIndex}.");
        }

        foreach (var course in courses)
        {
            var placed = placements.Count(p => p.CourseId == course.CourseId);
            if (placed != course.WeeklyPeriods)
            {
                throw new InvalidOperationException(
                    $"Course {course.CourseId} ({course.SubjectCode}) got {placed} periods, "
                    + $"expected {course.WeeklyPeriods}.");
            }
        }
    }
}
