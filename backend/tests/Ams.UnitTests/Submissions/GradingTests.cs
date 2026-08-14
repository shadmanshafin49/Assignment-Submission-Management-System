using Ams.Application.Dtos;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Submissions;

/// <summary>Marking and feedback: who may grade, and the bounds on the marks awarded.</summary>
public class GradingTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public GradingTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task The_owning_teacher_can_grade_a_submission()
    {
        var assignment = _world.GivenAssignment(maxMarks: 100);
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        var result = await _world.SubmissionsAs(_world.Rejaul)
            .GradeAsync(submission.Id, new GradeSubmissionRequest(85, "সুন্দর হয়েছে।"));

        result.Status.ShouldBe(SubmissionStatus.Graded);
        result.Marks.ShouldBe(85);
        result.Feedback.ShouldBe("সুন্দর হয়েছে।");
        result.GradedAt.ShouldBe(TestContext.Now);
    }

    [Theory]
    [InlineData(0)]    // lower bound
    [InlineData(50)]   // middle
    [InlineData(100)]  // upper bound
    public async Task Marks_within_zero_to_max_are_accepted(int marks)
    {
        var assignment = _world.GivenAssignment(maxMarks: 100);
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        var result = await _world.SubmissionsAs(_world.Rejaul)
            .GradeAsync(submission.Id, new GradeSubmissionRequest(marks, null));

        result.Marks.ShouldBe(marks);
    }

    [Theory]
    [InlineData(101)]  // one over the ceiling
    [InlineData(-1)]   // below zero
    [InlineData(1000)]
    public async Task Marks_outside_zero_to_max_are_refused(int marks)
    {
        var assignment = _world.GivenAssignment(maxMarks: 100);
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => _world.SubmissionsAs(_world.Rejaul)
                .GradeAsync(submission.Id, new GradeSubmissionRequest(marks, null)));

        ex.Message.ShouldContain("100");
    }

    [Fact]
    public async Task A_teacher_cannot_grade_another_teachers_assignment()
    {
        var assignment = _world.GivenAssignment(teacher: _world.Rejaul);
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        var ex = await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Afsar)
                .GradeAsync(submission.Id, new GradeSubmissionRequest(50, null)));

        ex.Message.ShouldContain("নিজের অ্যাসাইনমেন্টের");
    }

    [Fact]
    public async Task An_admin_cannot_grade()
    {
        // Admins have oversight of everything but deliberately do not award marks.
        var assignment = _world.GivenAssignment();
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Admin)
                .GradeAsync(submission.Id, new GradeSubmissionRequest(50, null)));
    }

    [Fact]
    public async Task A_student_cannot_grade_their_own_work()
    {
        var assignment = _world.GivenAssignment();
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .GradeAsync(submission.Id, new GradeSubmissionRequest(100, "নিজেকেই পূর্ণ নম্বর।")));
    }

    [Fact]
    public async Task Grading_records_which_teacher_awarded_the_marks()
    {
        var assignment = _world.GivenAssignment();
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        await _world.SubmissionsAs(_world.Rejaul)
            .GradeAsync(submission.Id, new GradeSubmissionRequest(70, null));

        _ctx.ClearTracking();
        var stored = _ctx.Db.Submissions.Single(s => s.Id == submission.Id);
        stored.GradedByTeacherId.ShouldBe(_world.Rejaul.Id);
    }

    [Fact]
    public async Task Regrading_overwrites_the_previous_marks_and_feedback()
    {
        var assignment = _world.GivenAssignment();
        var submission = _world.GivenSubmission(
            assignment, _world.Sadman, SubmissionStatus.Graded, marks: 40);

        var result = await _world.SubmissionsAs(_world.Rejaul)
            .GradeAsync(submission.Id, new GradeSubmissionRequest(75, "পুনর্মূল্যায়নের পর।"));

        result.Marks.ShouldBe(75);
        result.Feedback.ShouldBe("পুনর্মূল্যায়নের পর।");
    }

    // --------------------------------------------------------------- status changes

    [Fact]
    public async Task Returning_work_for_revision_clears_the_previous_grade()
    {
        // Otherwise the student would see marks for an answer they are being asked to replace.
        var assignment = _world.GivenAssignment();
        var submission = _world.GivenSubmission(
            assignment, _world.Sadman, SubmissionStatus.Graded, marks: 30);

        var result = await _world.SubmissionsAs(_world.Rejaul)
            .UpdateStatusAsync(
                submission.Id,
                new UpdateSubmissionStatusRequest(SubmissionStatus.ReturnedForRevision));

        result.Status.ShouldBe(SubmissionStatus.ReturnedForRevision);
        result.Marks.ShouldBeNull();
        result.GradedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Status_cannot_be_set_to_graded_without_going_through_grading()
    {
        // Marking something Graded with no marks would be a meaningless state.
        var assignment = _world.GivenAssignment();
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Rejaul)
                .UpdateStatusAsync(
                    submission.Id, new UpdateSubmissionStatusRequest(SubmissionStatus.Graded)));

        ex.Message.ShouldContain("এন্ডপয়েন্ট");
    }

    [Fact]
    public async Task A_student_cannot_change_a_submission_status()
    {
        var assignment = _world.GivenAssignment();
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .UpdateStatusAsync(
                    submission.Id,
                    new UpdateSubmissionStatusRequest(SubmissionStatus.ReturnedForRevision)));
    }

    // ------------------------------------------------------------------ review lists

    [Fact]
    public async Task A_teacher_reviewing_an_assignment_sees_every_students_submission()
    {
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman);
        _world.GivenSubmission(assignment, _world.Tanvir);

        var result = await _world.SubmissionsAs(_world.Rejaul)
            .ListForAssignmentAsync(assignment.Id, new SubmissionListQuery());

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task The_marking_list_comes_back_in_roll_order()
    {
        // Teachers mark down the roll sheet, so submission time is the wrong order to present.
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Arnab);   // roll 3, submitted first
        _world.GivenSubmission(assignment, _world.Sadman);  // roll 1
        _world.GivenSubmission(assignment, _world.Tanvir);  // roll 2

        var result = await _world.SubmissionsAs(_world.Rejaul)
            .ListForAssignmentAsync(assignment.Id, new SubmissionListQuery());

        result.Items.Select(s => s.RollNumber).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task A_teacher_cannot_review_another_teachers_assignment()
    {
        var assignment = _world.GivenAssignment(teacher: _world.Rejaul);
        _world.GivenSubmission(assignment, _world.Sadman);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Afsar)
                .ListForAssignmentAsync(assignment.Id, new SubmissionListQuery()));
    }

    [Fact]
    public async Task An_admin_can_review_any_assignments_submissions()
    {
        var assignment = _world.GivenAssignment(teacher: _world.Rejaul);
        _world.GivenSubmission(assignment, _world.Sadman);

        var result = await _world.SubmissionsAs(_world.Admin)
            .ListForAssignmentAsync(assignment.Id, new SubmissionListQuery());

        result.TotalCount.ShouldBe(1);
    }
}
