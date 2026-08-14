using Ams.Application.Dtos;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Assignments;

/// <summary>
/// What a student is allowed to see. Drafts, another class's work and another faith's religion
/// course must never leak through any student-facing route — this is the rule most likely to be
/// got wrong by filtering in the UI instead of the API.
/// </summary>
public class StudentVisibilityTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public StudentVisibilityTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task A_student_sees_published_work_on_a_course_their_class_takes()
    {
        _world.GivenAssignment(status: AssignmentStatus.Published, course: _world.SixMath);

        var result = await _world.AssignmentsAs(_world.Sadman)
            .ListForStudentAsync(new StudentAssignmentListQuery());

        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_student_never_sees_draft_work()
    {
        _world.GivenAssignment(status: AssignmentStatus.Draft, course: _world.SixMath);

        var result = await _world.AssignmentsAs(_world.Sadman)
            .ListForStudentAsync(new StudentAssignmentListQuery());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_student_never_sees_work_set_for_another_class()
    {
        // ইশতিয়াক is in class 7; this work belongs to class 6's maths course.
        _world.GivenAssignment(status: AssignmentStatus.Published, course: _world.SixMath);

        var result = await _world.AssignmentsAs(_world.Ishtiaq)
            .ListForStudentAsync(new StudentAssignmentListQuery());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_student_never_sees_another_faiths_religion_course()
    {
        // অর্ণব is the one Hindu student in class 6, so ইসলাম ও নৈতিক শিক্ষা is not his course
        // even though it is set for his class.
        _world.GivenAssignment(
            course: _world.SixIslam, teacher: _world.Mukim,
            type: AssignmentType.CreativeQuestion);

        var result = await _world.AssignmentsAs(_world.Arnab)
            .ListForStudentAsync(new StudentAssignmentListQuery());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Each_student_sees_the_religion_course_of_their_own_faith()
    {
        _world.GivenAssignment(
            course: _world.SixIslam, teacher: _world.Mukim,
            type: AssignmentType.CreativeQuestion);
        _world.GivenAssignment(
            course: _world.SixHindu, teacher: _world.Purnima,
            type: AssignmentType.CreativeQuestion);

        var muslim = await _world.AssignmentsAs(_world.Sadman)
            .ListForStudentAsync(new StudentAssignmentListQuery());
        var hindu = await _world.AssignmentsAs(_world.Arnab)
            .ListForStudentAsync(new StudentAssignmentListQuery());

        muslim.Items.ShouldHaveSingleItem().SubjectName.ShouldBe("ইসলাম ও নৈতিক শিক্ষা");
        hindu.Items.ShouldHaveSingleItem().SubjectName.ShouldBe("হিন্দুধর্ম ও নৈতিক শিক্ষা");
    }

    [Fact]
    public async Task Fetching_a_draft_by_id_reports_not_found_rather_than_forbidden()
    {
        // Returning 403 would confirm the assignment exists; 404 gives nothing away.
        var assignment = _world.GivenAssignment(status: AssignmentStatus.Draft);

        await Should.ThrowAsync<NotFoundException>(
            () => _world.AssignmentsAs(_world.Sadman).GetForStudentAsync(assignment.Id));
    }

    [Fact]
    public async Task Fetching_another_classes_assignment_by_id_reports_not_found()
    {
        var assignment = _world.GivenAssignment(
            status: AssignmentStatus.Published, course: _world.SixMath);

        await Should.ThrowAsync<NotFoundException>(
            () => _world.AssignmentsAs(_world.Ishtiaq).GetForStudentAsync(assignment.Id));
    }

    [Fact]
    public async Task Fetching_another_faiths_religion_assignment_by_id_reports_not_found()
    {
        var assignment = _world.GivenAssignment(
            course: _world.SixIslam, teacher: _world.Mukim,
            type: AssignmentType.CreativeQuestion);

        await Should.ThrowAsync<NotFoundException>(
            () => _world.AssignmentsAs(_world.Arnab).GetForStudentAsync(assignment.Id));
    }

    [Fact]
    public async Task A_students_feed_carries_their_own_submission_and_not_a_classmates()
    {
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman, answer: "সাদমানের উত্তর");
        _world.GivenSubmission(assignment, _world.Tanvir, answer: "তানভীরের উত্তর");

        var result = await _world.AssignmentsAs(_world.Sadman)
            .ListForStudentAsync(new StudentAssignmentListQuery());

        var item = result.Items.ShouldHaveSingleItem();
        item.MySubmission.ShouldNotBeNull();
        item.MySubmission!.AnswerText.ShouldBe("সাদমানের উত্তর");
        item.MySubmission.StudentId.ShouldBe(_world.Sadman.Id);
    }

    [Fact]
    public async Task A_teacher_cannot_use_the_student_feed()
    {
        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AssignmentsAs(_world.Rejaul)
                .ListForStudentAsync(new StudentAssignmentListQuery()));
    }

    [Fact]
    public async Task PendingOnly_filters_out_work_the_student_has_already_submitted()
    {
        var answered = _world.GivenAssignment();
        _world.GivenAssignment(); // second, unanswered
        _world.GivenSubmission(answered, _world.Sadman);

        var result = await _world.AssignmentsAs(_world.Sadman)
            .ListForStudentAsync(new StudentAssignmentListQuery { PendingOnly = true });

        result.TotalCount.ShouldBe(1);
        result.Items.Single().MySubmission.ShouldBeNull();
    }

    [Fact]
    public async Task DueOnly_filters_out_work_whose_deadline_has_passed()
    {
        _world.GivenAssignment(deadline: TestContext.Now.AddDays(3));
        _world.GivenAssignment(deadline: TestContext.Now.AddDays(-1));

        var result = await _world.AssignmentsAs(_world.Sadman)
            .ListForStudentAsync(new StudentAssignmentListQuery { DueOnly = true });

        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_student_sees_work_from_every_course_their_class_takes()
    {
        _world.GivenAssignment(course: _world.SixMath, teacher: _world.Rejaul);
        _world.GivenAssignment(
            course: _world.SixBangla, teacher: _world.Afsar,
            type: AssignmentType.CreativeQuestion);

        var result = await _world.AssignmentsAs(_world.Sadman)
            .ListForStudentAsync(new StudentAssignmentListQuery());

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Filtering_the_feed_by_course_narrows_it_to_that_subject()
    {
        _world.GivenAssignment(course: _world.SixMath, teacher: _world.Rejaul);
        _world.GivenAssignment(
            course: _world.SixBangla, teacher: _world.Afsar,
            type: AssignmentType.CreativeQuestion);

        var result = await _world.AssignmentsAs(_world.Sadman).ListForStudentAsync(
            new StudentAssignmentListQuery { CourseId = _world.SixBangla.Id });

        result.Items.ShouldHaveSingleItem().CourseCode.ShouldBe("C06-101");
    }
}
