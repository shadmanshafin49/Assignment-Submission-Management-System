using Ams.Application.Common;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Controllers;

public record EnumOptionDto(string Value, string Label);

public record PeriodSlotDto(int PeriodIndex, string StartTime, string EndTime);

public record ReferenceDataDto(
    IReadOnlyList<EnumOptionDto> AssignmentTypes,
    IReadOnlyList<EnumOptionDto> AssignmentStatuses,
    IReadOnlyList<EnumOptionDto> SubmissionStatuses,
    IReadOnlyList<EnumOptionDto> Roles,
    IReadOnlyList<EnumOptionDto> FaithGroups,
    IReadOnlyList<EnumOptionDto> WeekDays,
    IReadOnlyList<PeriodSlotDto> Periods,
    int PeriodsPerDay,
    int TeachingDaysPerWeek,
    int PeriodsPerWeek,
    string AssemblyStart,
    string AssemblyEnd,
    int BreakAfterPeriod,
    string BreakStart,
    string BreakEnd);

/// <summary>
/// Bangla labels and school-day constants, served once so the frontend does not keep a second,
/// drifting copy of the domain vocabulary. "সৃজনশীল প্রশ্ন" is the name of a specific NCTB
/// question type, not interface copy — it belongs with the enum that defines it.
/// </summary>
[ApiController]
[Route("api/reference")]
[Authorize]
[Produces("application/json")]
public class ReferenceController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ReferenceDataDto), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public ActionResult<ReferenceDataDto> Get() => Ok(new ReferenceDataDto(
        Options<AssignmentType>(BanglaNames.AssignmentType),
        Options<AssignmentStatus>(BanglaNames.AssignmentStatus),
        Options<SubmissionStatus>(BanglaNames.SubmissionStatus),
        Options<UserRole>(BanglaNames.Role),
        Options<FaithGroup>(BanglaNames.Faith),
        Options<WeekDay>(BanglaNames.Day),
        Enumerable.Range(1, PeriodSchedule.PeriodsPerDay)
            .Select(i =>
            {
                var (start, end) = PeriodSchedule.SlotFor(i);
                return new PeriodSlotDto(i, start.ToString("HH:mm"), end.ToString("HH:mm"));
            })
            .ToList(),
        PeriodSchedule.PeriodsPerDay,
        PeriodSchedule.TeachingDaysPerWeek,
        PeriodSchedule.PeriodsPerWeek,
        PeriodSchedule.AssemblyStart.ToString("HH:mm"),
        PeriodSchedule.AssemblyEnd.ToString("HH:mm"),
        PeriodSchedule.BreakAfterPeriod,
        PeriodSchedule.BreakStart.ToString("HH:mm"),
        PeriodSchedule.BreakEnd.ToString("HH:mm")));

    private static List<EnumOptionDto> Options<T>(Func<T, string> label) where T : struct, Enum
        => Enum.GetValues<T>().Select(v => new EnumOptionDto(v.ToString(), label(v))).ToList();
}
