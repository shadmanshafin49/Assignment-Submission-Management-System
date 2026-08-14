namespace Ams.Domain.Enums;

/// <summary>
/// Teaching days. The school runs a six-day week, শনিবার–বৃহস্পতিবার, with শুক্রবার as the
/// weekly holiday — six 50-minute periods a day, 36 periods a week.
/// <para>
/// Deliberately not <see cref="System.DayOfWeek"/>: that type starts the week on Sunday and
/// includes Friday, which would let a routine row be written for a day the school is closed.
/// </para>
/// </summary>
public enum WeekDay
{
    Saturday = 1,
    Sunday = 2,
    Monday = 3,
    Tuesday = 4,
    Wednesday = 5,
    Thursday = 6
}
