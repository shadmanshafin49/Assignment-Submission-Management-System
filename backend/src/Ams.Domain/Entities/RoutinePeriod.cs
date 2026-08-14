using Ams.Domain.Enums;

namespace Ams.Domain.Entities;

/// <summary>
/// One cell of the weekly class routine: "class 6, রবিবার, ৩য় পিরিয়ড → C06-109 (গণিত)".
/// <para>
/// Six days × six periods = 36 slots per class, and every slot is filled.
/// </para>
/// <para>
/// A slot normally holds one course, but the ধর্ম ও নৈতিক শিক্ষা period is a real exception: the
/// Muslim and Hindu groups of the same class are taught at the same time, in different rooms, by
/// different teachers. So the unique index is on (class, day, period, course), and the service
/// enforces the narrower rule — a slot may hold more than one course only when every course in it
/// belongs to a different faith group. That keeps "a class cannot be double-booked" true for
/// everything except the one case where it genuinely is not.
/// </para>
/// </summary>
public class RoutinePeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public WeekDay Day { get; set; }

    /// <summary>1–6. Bell times come from <see cref="PeriodSchedule"/>.</summary>
    public int PeriodIndex { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// The school day. Derived from NCTB's one-shift rule for classes 6–8 — 15-minute assembly,
/// 60-minute first period for roll call, 50 minutes thereafter — laid out from a 07:45 assembly
/// and closing at 14:00. See <c>docs/RESEARCH.md</c> §4.
/// <para>
/// The day splits evenly: three periods, tiffin, three periods. That is the one deliberate
/// departure from the NCTB sheet, which puts a 35-minute break after the fourth period. Three and
/// three is what the school actually rings, and holding the 14:00 close with six periods leaves
/// the break 50 minutes rather than 35.
/// </para>
/// <para>
/// Kept as code rather than a table because bell times are a property of the school day, not of
/// any one class, and every class shares them.
/// </para>
/// </summary>
public static class PeriodSchedule
{
    public const int PeriodsPerDay = 6;
    public const int TeachingDaysPerWeek = 6;
    public const int PeriodsPerWeek = PeriodsPerDay * TeachingDaysPerWeek;

    public static readonly TimeOnly AssemblyStart = new(7, 45);
    public static readonly TimeOnly AssemblyEnd = new(8, 0);

    /// <summary>Tiffin break sits between period 3 and period 4 — the middle of the day.</summary>
    public const int BreakAfterPeriod = 3;

    public static readonly TimeOnly BreakStart = new(10, 40);
    public static readonly TimeOnly BreakEnd = new(11, 30);

    private static readonly (TimeOnly Start, TimeOnly End)[] Slots =
    [
        (new TimeOnly(8, 0), new TimeOnly(9, 0)),    // 60 min — includes roll call
        (new TimeOnly(9, 0), new TimeOnly(9, 50)),
        (new TimeOnly(9, 50), new TimeOnly(10, 40)),
        // ---- টিফিন 10:40–11:30 ----
        (new TimeOnly(11, 30), new TimeOnly(12, 20)),
        (new TimeOnly(12, 20), new TimeOnly(13, 10)),
        (new TimeOnly(13, 10), new TimeOnly(14, 0))
    ];

    public static bool IsValidPeriod(int periodIndex)
        => periodIndex >= 1 && periodIndex <= PeriodsPerDay;

    public static (TimeOnly Start, TimeOnly End) SlotFor(int periodIndex)
        => IsValidPeriod(periodIndex)
            ? Slots[periodIndex - 1]
            : throw new ArgumentOutOfRangeException(nameof(periodIndex));
}
