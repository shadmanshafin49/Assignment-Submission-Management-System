using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Admin;

/// <summary>
/// The weekly class routine: six days × six periods. Most of the rules here exist because a
/// timetable is a scheduling problem, not a list — a slot can be double-booked, a teacher can be
/// put in two rooms at once, and the ধর্ম ও নৈতিক শিক্ষা period is a genuine exception to both.
/// </summary>
public class RoutineTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public RoutineTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    private Task<ClassRoutineDto> Set(Guid classRoomId, WeekDay day, int period, Guid courseId) =>
        _world.AcademicAs(_world.Admin)
            .SetRoutinePeriodAsync(new SetRoutinePeriodRequest(classRoomId, day, period, courseId));

    private static RoutineSlotDto SlotOf(ClassRoutineDto routine, WeekDay day, int period) =>
        routine.Days.Single(d => d.Day == day).Periods.Single(p => p.PeriodIndex == period);

    [Fact]
    public async Task The_routine_covers_the_six_teaching_days_with_six_periods_each()
    {
        // শুক্রবার is the weekly holiday, so it is not a day the routine can even express.
        var routine = await _world.AcademicAs(_world.Admin)
            .GetClassRoutineAsync(_world.ClassSix.Id);

        routine.Days.Count.ShouldBe(6);
        routine.Days.ShouldAllBe(d => d.Periods.Count == 6);
        routine.Days.Select(d => d.Day).ShouldNotContain((WeekDay)7);
    }

    [Fact]
    public async Task Bell_times_follow_the_schools_one_shift_day()
    {
        var routine = await _world.AcademicAs(_world.Admin)
            .GetClassRoutineAsync(_world.ClassSix.Id);

        var monday = routine.Days.Single(d => d.Day == WeekDay.Monday);

        // First period runs 60 minutes for roll call; the rest are 50.
        monday.Periods[0].StartTime.ShouldBe("08:00");
        monday.Periods[0].EndTime.ShouldBe("09:00");
        monday.Periods[1].EndTime.ShouldBe("09:50");

        // Three periods, tiffin, three periods. Period 4 starts after the break, not at 10:40,
        // and the last one lands on the 14:00 close.
        routine.BreakAfterPeriod.ShouldBe(3);
        monday.Periods[2].EndTime.ShouldBe("10:40");
        monday.Periods[3].StartTime.ShouldBe("11:30");
        monday.Periods[5].EndTime.ShouldBe("14:00");
    }

    [Fact]
    public async Task No_period_runs_through_the_tiffin_break()
    {
        var routine = await _world.AcademicAs(_world.Admin)
            .GetClassRoutineAsync(_world.ClassSix.Id);

        // The grid draws tiffin as a gap between two columns, which is only honest if no
        // teaching slot overlaps it.
        foreach (var slot in routine.Days.SelectMany(d => d.Periods))
        {
            var overlapsBreak =
                string.CompareOrdinal(slot.StartTime, routine.BreakEnd) < 0 &&
                string.CompareOrdinal(slot.EndTime, routine.BreakStart) > 0;

            overlapsBreak.ShouldBeFalse(
                $"{slot.PeriodIndex}ম পিরিয়ড ({slot.StartTime}–{slot.EndTime}) overlaps tiffin");
        }
    }

    [Fact]
    public async Task Setting_a_period_places_the_course_in_that_slot()
    {
        var routine = await Set(_world.ClassSix.Id, WeekDay.Sunday, 3, _world.SixMath.Id);

        var entry = SlotOf(routine, WeekDay.Sunday, 3).Entries.ShouldHaveSingleItem();
        entry.CourseCode.ShouldBe("C06-109");
        entry.TeacherName.ShouldBe("মোঃ রেজাউল করিম");
    }

    [Fact]
    public async Task A_course_belonging_to_another_class_cannot_be_placed_in_the_slot()
    {
        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => Set(_world.ClassSix.Id, WeekDay.Sunday, 1, _world.SevenMath.Id));

        ex.Message.ShouldContain("এই শ্রেণির নয়");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task A_period_outside_the_school_day_is_refused(int period)
    {
        await Should.ThrowAsync<ValidationFailedException>(
            () => Set(_world.ClassSix.Id, WeekDay.Sunday, period, _world.SixMath.Id));
    }

    [Fact]
    public async Task Setting_a_different_course_replaces_what_was_in_the_slot()
    {
        // "Set this period to বাংলা" means exactly that — not "add বাংলা alongside গণিত".
        await Set(_world.ClassSix.Id, WeekDay.Monday, 2, _world.SixMath.Id);

        var routine = await Set(_world.ClassSix.Id, WeekDay.Monday, 2, _world.SixBangla.Id);

        SlotOf(routine, WeekDay.Monday, 2).Entries
            .ShouldHaveSingleItem().CourseCode.ShouldBe("C06-101");
    }

    [Fact]
    public async Task Setting_the_same_course_twice_is_a_no_op_rather_than_a_duplicate()
    {
        await Set(_world.ClassSix.Id, WeekDay.Monday, 2, _world.SixMath.Id);

        var routine = await Set(_world.ClassSix.Id, WeekDay.Monday, 2, _world.SixMath.Id);

        SlotOf(routine, WeekDay.Monday, 2).Entries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task The_religion_period_holds_both_faith_groups_at_once()
    {
        // The one real exception: the Muslim and Hindu halves of a class are taught ধর্ম ও নৈতিক
        // শিক্ষা in the same period, in different rooms, by different teachers.
        await Set(_world.ClassSix.Id, WeekDay.Tuesday, 4, _world.SixIslam.Id);

        var routine = await Set(_world.ClassSix.Id, WeekDay.Tuesday, 4, _world.SixHindu.Id);

        var entries = SlotOf(routine, WeekDay.Tuesday, 4).Entries;
        entries.Count.ShouldBe(2);
        entries.Select(e => e.FaithGroup).ShouldBe([FaithGroup.Islam, FaithGroup.Hindu], ignoreOrder: true);
    }

    [Fact]
    public async Task A_non_religion_course_still_replaces_a_parallel_religion_period()
    {
        await Set(_world.ClassSix.Id, WeekDay.Tuesday, 4, _world.SixIslam.Id);
        await Set(_world.ClassSix.Id, WeekDay.Tuesday, 4, _world.SixHindu.Id);

        var routine = await Set(_world.ClassSix.Id, WeekDay.Tuesday, 4, _world.SixMath.Id);

        SlotOf(routine, WeekDay.Tuesday, 4).Entries
            .ShouldHaveSingleItem().CourseCode.ShouldBe("C06-109");
    }

    [Fact]
    public async Task A_teacher_cannot_be_booked_into_two_classes_in_the_same_period()
    {
        // রেজাউল takes গণিত in both classes. No unique index can express this rule — it spans
        // two different classrooms' rows.
        await Set(_world.ClassSix.Id, WeekDay.Wednesday, 5, _world.SixMath.Id);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => Set(_world.ClassSeven.Id, WeekDay.Wednesday, 5, _world.SevenMath.Id));

        ex.Message.ShouldContain("অন্য শ্রেণিতে ক্লাস রয়েছে");
    }

    [Fact]
    public async Task The_same_teacher_may_take_the_two_classes_in_different_periods()
    {
        await Set(_world.ClassSix.Id, WeekDay.Wednesday, 5, _world.SixMath.Id);

        var routine = await Set(_world.ClassSeven.Id, WeekDay.Wednesday, 6, _world.SevenMath.Id);

        SlotOf(routine, WeekDay.Wednesday, 6).Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Clearing_a_slot_empties_it_including_both_halves_of_a_religion_period()
    {
        await Set(_world.ClassSix.Id, WeekDay.Thursday, 1, _world.SixIslam.Id);
        await Set(_world.ClassSix.Id, WeekDay.Thursday, 1, _world.SixHindu.Id);

        var routine = await _world.AcademicAs(_world.Admin)
            .ClearRoutinePeriodAsync(_world.ClassSix.Id, WeekDay.Thursday, 1);

        SlotOf(routine, WeekDay.Thursday, 1).Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_teacher_cannot_edit_the_routine()
    {
        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AcademicAs(_world.Rejaul).SetRoutinePeriodAsync(
                new SetRoutinePeriodRequest(
                    _world.ClassSix.Id, WeekDay.Saturday, 1, _world.SixMath.Id)));
    }

    [Fact]
    public async Task A_teachers_own_routine_lists_only_the_periods_they_take()
    {
        await Set(_world.ClassSix.Id, WeekDay.Saturday, 1, _world.SixMath.Id);
        await Set(_world.ClassSeven.Id, WeekDay.Saturday, 2, _world.SevenMath.Id);
        await Set(_world.ClassSix.Id, WeekDay.Saturday, 3, _world.SixBangla.Id);

        var mine = await _world.AcademicAs(_world.Rejaul).GetMyTeachingRoutineAsync();

        mine.Count.ShouldBe(2);
        mine.ShouldAllBe(s => s.SubjectName == "গণিত");
        mine[0].StartTime.ShouldBe("08:00");
        mine[1].ClassRoomName.ShouldBe("সপ্তম শ্রেণি");
    }

    [Fact]
    public async Task A_student_can_read_their_own_classes_routine()
    {
        await Set(_world.ClassSix.Id, WeekDay.Sunday, 1, _world.SixMath.Id);

        var routine = await _world.AcademicAs(_world.Sadman)
            .GetClassRoutineAsync(_world.ClassSix.Id);

        SlotOf(routine, WeekDay.Sunday, 1).Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_student_cannot_read_another_classes_routine()
    {
        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AcademicAs(_world.Sadman).GetClassRoutineAsync(_world.ClassSeven.Id));
    }

    [Fact]
    public async Task A_full_week_is_thirty_six_periods()
    {
        // The figure the whole timetable is built around: 6 days × 6 periods.
        PeriodSchedule.PeriodsPerWeek.ShouldBe(36);
    }
}
