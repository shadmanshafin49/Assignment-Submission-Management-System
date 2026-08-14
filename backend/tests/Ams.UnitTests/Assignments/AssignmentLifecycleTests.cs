using Ams.Application.Dtos;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Assignments;

/// <summary>Draft → published → locked lifecycle, and the guards protecting student work.</summary>
public class AssignmentLifecycleTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public AssignmentLifecycleTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    private UpdateAssignmentRequest Edit(
        string title = "অনুশীলনী সমাধান",
        string description = "নির্ধারিত সমস্যাগুলোর সমাধান করো।",
        Guid? courseId = null,
        AssignmentType type = AssignmentType.MathProblem,
        int maxMarks = 100,
        DateTimeOffset? deadline = null) =>
        new(
            Title: title,
            Description: description,
            CourseId: courseId ?? _world.SixMath.Id,
            Type: type,
            ChapterOrLesson: "চতুর্থ অধ্যায়",
            Deadline: deadline ?? TestContext.Now.AddDays(7),
            MaxMarks: maxMarks,
            AllowLateSubmission: false,
            AllowResubmission: true,
            AllowComments: true);

    [Fact]
    public async Task Publishing_a_draft_marks_it_published_and_stamps_the_time()
    {
        var assignment = _world.GivenAssignment(status: AssignmentStatus.Draft);

        var result = await _world.AssignmentsAs(_world.Rejaul).PublishAsync(assignment.Id);

        result.Status.ShouldBe(AssignmentStatus.Published);
        result.PublishedAt.ShouldBe(TestContext.Now);
    }

    [Fact]
    public async Task Publishing_an_already_published_assignment_is_refused()
    {
        var assignment = _world.GivenAssignment(status: AssignmentStatus.Published);

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AssignmentsAs(_world.Rejaul).PublishAsync(assignment.Id));
    }

    [Fact]
    public async Task Publishing_with_a_deadline_in_the_past_is_refused()
    {
        // Students could never submit to it, so publishing is a configuration error.
        var assignment = _world.GivenAssignment(
            status: AssignmentStatus.Draft, deadline: TestContext.Now.AddDays(-1));

        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => _world.AssignmentsAs(_world.Rejaul).PublishAsync(assignment.Id));

        ex.Message.ShouldContain("ভবিষ্যতে");
    }

    [Fact]
    public async Task A_draft_may_be_parked_with_a_past_deadline()
    {
        // Only publishing needs a live deadline; a teacher drafting next week's work should not
        // be blocked because they have not set the date yet.
        var request = new CreateAssignmentRequest(
            "খসড়া", "পরে ঠিক করা হবে।", _world.SixMath.Id, AssignmentType.MathProblem,
            null, TestContext.Now.AddHours(-1), 20, false, true, true);

        var created = await _world.AssignmentsAs(_world.Rejaul).CreateAsync(request);

        created.Status.ShouldBe(AssignmentStatus.Draft);
    }

    [Fact]
    public async Task Omitting_the_deadline_applies_the_schools_default_window()
    {
        // Teachers set work weekly, so the default is the school's one-week window rather than
        // a value every teacher has to retype.
        var request = new CreateAssignmentRequest(
            "সাপ্তাহিক কাজ", "অনুশীলনী ৪.১", _world.SixMath.Id, AssignmentType.MathProblem,
            null, null, 20, false, true, true);

        var created = await _world.AssignmentsAs(_world.Rejaul).CreateAsync(request);

        created.Deadline.ShouldBe(TestContext.Now.AddDays(7));
    }

    [Fact]
    public async Task Creating_an_assignment_with_zero_max_marks_is_refused()
    {
        var request = new CreateAssignmentRequest(
            "শূন্য নম্বর", "নির্দেশনা", _world.SixMath.Id, AssignmentType.MathProblem,
            null, TestContext.Now.AddDays(3), 0, false, true, true);

        await Should.ThrowAsync<ValidationFailedException>(
            () => _world.AssignmentsAs(_world.Rejaul).CreateAsync(request));
    }

    [Fact]
    public async Task An_assignment_without_submissions_can_be_deleted()
    {
        var assignment = _world.GivenAssignment();

        await _world.AssignmentsAs(_world.Rejaul).DeleteAsync(assignment.Id);

        _ctx.ClearTracking();
        _ctx.Db.Assignments.Any(a => a.Id == assignment.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task An_assignment_with_submissions_cannot_be_deleted()
    {
        // Deleting would cascade away student work, so it is blocked outright.
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AssignmentsAs(_world.Rejaul).DeleteAsync(assignment.Id));

        ex.Message.ShouldContain("জমা");
        _ctx.Db.Assignments.Any(a => a.Id == assignment.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task Deleting_an_assignment_also_removes_its_attached_files()
    {
        // The rows cascade on their own; the stored bytes only go if the service asks.
        var assignment = _world.GivenAssignment();
        var attachment = await _world.AssignmentsAs(_world.Rejaul)
            .AddAttachmentAsync(assignment.Id, FakeFileStorage.Upload("worksheet.pdf"));

        await _world.AssignmentsAs(_world.Rejaul).DeleteAsync(assignment.Id);

        _world.Storage.Keys.ShouldBeEmpty();
        attachment.FileName.ShouldBe("worksheet.pdf");
    }

    [Fact]
    public async Task An_assignment_with_submissions_cannot_be_reverted_to_draft()
    {
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman);

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AssignmentsAs(_world.Rejaul).UnpublishAsync(assignment.Id));
    }

    [Fact]
    public async Task Withdrawing_work_nobody_has_answered_yet_puts_it_back_in_draft()
    {
        var assignment = _world.GivenAssignment(status: AssignmentStatus.Published);

        var result = await _world.AssignmentsAs(_world.Rejaul).UnpublishAsync(assignment.Id);

        result.Status.ShouldBe(AssignmentStatus.Draft);
        result.PublishedAt.ShouldBeNull();
    }

    [Fact]
    public async Task The_course_cannot_be_changed_once_students_have_submitted()
    {
        // Moving the work to another course would orphan the answers already given.
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AssignmentsAs(_world.Rejaul)
                .UpdateAsync(assignment.Id, Edit(courseId: _world.SevenMath.Id)));

        ex.Message.ShouldContain("কোর্স পরিবর্তন করা যাবে না");
    }

    [Fact]
    public async Task Work_can_be_moved_to_another_of_the_teachers_own_courses_before_anyone_answers()
    {
        var assignment = _world.GivenAssignment();

        var result = await _world.AssignmentsAs(_world.Rejaul)
            .UpdateAsync(assignment.Id, Edit(courseId: _world.SevenMath.Id));

        result.CourseCode.ShouldBe("C07-109");
    }

    [Fact]
    public async Task Work_cannot_be_moved_to_a_course_the_teacher_does_not_take()
    {
        var assignment = _world.GivenAssignment();

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AssignmentsAs(_world.Rejaul).UpdateAsync(
                assignment.Id,
                Edit(courseId: _world.SixBangla.Id, type: AssignmentType.CreativeQuestion)));
    }

    [Fact]
    public async Task Max_marks_cannot_be_lowered_below_marks_already_awarded()
    {
        var assignment = _world.GivenAssignment(maxMarks: 100);
        _world.GivenSubmission(assignment, _world.Sadman, SubmissionStatus.Graded, marks: 80);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AssignmentsAs(_world.Rejaul)
                .UpdateAsync(assignment.Id, Edit(maxMarks: 50)));

        ex.Message.ShouldContain("80");
    }

    [Fact]
    public async Task Title_and_description_can_still_be_corrected_after_submissions_exist()
    {
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman);

        var result = await _world.AssignmentsAs(_world.Rejaul).UpdateAsync(
            assignment.Id, Edit(title: "সংশোধিত শিরোনাম", description: "আরও স্পষ্ট নির্দেশনা।"));

        result.Title.ShouldBe("সংশোধিত শিরোনাম");
    }

    [Fact]
    public async Task An_edit_cannot_switch_to_a_type_the_subject_does_not_use()
    {
        var assignment = _world.GivenAssignment();

        await Should.ThrowAsync<ValidationFailedException>(
            () => _world.AssignmentsAs(_world.Rejaul)
                .UpdateAsync(assignment.Id, Edit(type: AssignmentType.ThemeExpansion)));
    }
}
