using Ams.Application.Abstractions;
using Ams.Application.Dtos;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Submissions;

/// <summary>
/// Submitting and editing work: deadlines, late policy, the one-submission-per-student rule,
/// attached answer files, and the return-for-revision escape hatch.
/// </summary>
public class SubmissionWorkflowTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public SubmissionWorkflowTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    private static CreateSubmissionRequest Answer(string text = "আমার সমাধান।") => new(text);

    private static readonly IReadOnlyList<FileUpload> NoFiles = [];

    [Fact]
    public async Task A_student_can_submit_before_the_deadline()
    {
        var assignment = _world.GivenAssignment(deadline: TestContext.Now.AddDays(1));

        var result = await _world.SubmissionsAs(_world.Sadman)
            .SubmitAsync(assignment.Id, Answer(), NoFiles);

        result.Status.ShouldBe(SubmissionStatus.Submitted);
        result.SubmittedAt.ShouldBe(TestContext.Now);
        result.Marks.ShouldBeNull();
        // The roll number rides along so a teacher's marking list reads like a roll sheet.
        result.RollNumber.ShouldBe(1);
    }

    [Fact]
    public async Task Submitting_after_the_deadline_is_refused_when_late_work_is_not_allowed()
    {
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddDays(-1), allowLate: false);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Sadman).SubmitAsync(assignment.Id, Answer(), NoFiles));

        ex.Message.ShouldContain("সময়সীমা শেষ");
    }

    [Fact]
    public async Task Submitting_after_the_deadline_is_accepted_and_flagged_when_late_work_is_allowed()
    {
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddDays(-1), allowLate: true);

        var result = await _world.SubmissionsAs(_world.Sadman)
            .SubmitAsync(assignment.Id, Answer(), NoFiles);

        // Accepted, but the teacher can still see it arrived late.
        result.Status.ShouldBe(SubmissionStatus.Late);
    }

    [Fact]
    public async Task Crossing_the_deadline_flips_an_otherwise_identical_submission_to_late()
    {
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddHours(1), allowLate: true);

        // Advance the clock past the deadline — nothing else about the request changes.
        _ctx.Clock.Advance(TimeSpan.FromHours(2));

        var result = await _world.SubmissionsAs(_world.Sadman)
            .SubmitAsync(assignment.Id, Answer(), NoFiles);

        result.Status.ShouldBe(SubmissionStatus.Late);
    }

    [Fact]
    public async Task A_student_cannot_submit_the_same_assignment_twice()
    {
        var assignment = _world.GivenAssignment();
        await _world.SubmissionsAs(_world.Sadman)
            .SubmitAsync(assignment.Id, Answer("প্রথম"), NoFiles);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .SubmitAsync(assignment.Id, Answer("দ্বিতীয়"), NoFiles));

        ex.Message.ShouldContain("ইতিমধ্যে জমা");
    }

    [Fact]
    public async Task A_student_cannot_submit_to_a_draft_assignment()
    {
        var assignment = _world.GivenAssignment(status: AssignmentStatus.Draft);

        // Reported as missing rather than forbidden — a draft does not exist as far as
        // students are concerned.
        await Should.ThrowAsync<NotFoundException>(
            () => _world.SubmissionsAs(_world.Sadman).SubmitAsync(assignment.Id, Answer(), NoFiles));
    }

    [Fact]
    public async Task A_student_cannot_submit_to_another_classes_assignment()
    {
        // ইশতিয়াক is in class 7; this work belongs to class 6's maths course.
        var assignment = _world.GivenAssignment(course: _world.SixMath);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Ishtiaq)
                .SubmitAsync(assignment.Id, Answer(), NoFiles));
    }

    [Fact]
    public async Task A_student_cannot_submit_to_another_faiths_religion_course()
    {
        // অর্ণব's class does take ইসলাম ও নৈতিক শিক্ষা — but he does not.
        var assignment = _world.GivenAssignment(
            course: _world.SixIslam, teacher: _world.Mukim,
            type: AssignmentType.CreativeQuestion);

        var ex = await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Arnab).SubmitAsync(assignment.Id, Answer(), NoFiles));

        ex.Message.ShouldContain("ধর্ম শিক্ষা");
    }

    [Fact]
    public async Task A_teacher_cannot_submit_work()
    {
        var assignment = _world.GivenAssignment();

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Rejaul).SubmitAsync(assignment.Id, Answer(), NoFiles));
    }

    // ------------------------------------------------------------------ answer files

    [Fact]
    public async Task An_answer_may_be_a_file_with_no_typed_text()
    {
        // Handwritten maths is photographed or scanned far more often than it is typed.
        var assignment = _world.GivenAssignment();

        var result = await _world.SubmissionsAs(_world.Sadman).SubmitAsync(
            assignment.Id, Answer(""), [FakeFileStorage.Upload("khata.jpg", "image/jpeg")]);

        result.AnswerText.ShouldBeEmpty();
        result.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("khata.jpg");
    }

    [Fact]
    public async Task An_answer_with_neither_text_nor_a_file_is_refused()
    {
        var assignment = _world.GivenAssignment();

        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .SubmitAsync(assignment.Id, Answer("   "), NoFiles));

        ex.Message.ShouldContain("ফাইল");
    }

    [Fact]
    public async Task A_file_type_outside_the_allow_list_is_refused()
    {
        var assignment = _world.GivenAssignment();

        await Should.ThrowAsync<ValidationFailedException>(
            () => _world.SubmissionsAs(_world.Sadman).SubmitAsync(
                assignment.Id, Answer(), [FakeFileStorage.Upload("answer.exe", "application/octet-stream")]));

        // Nothing was written: the whole submission aborts rather than half-landing.
        _world.Storage.Keys.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_file_over_the_size_limit_is_refused()
    {
        var assignment = _world.GivenAssignment();

        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => _world.SubmissionsAs(_world.Sadman).SubmitAsync(
                assignment.Id, Answer(),
                [FakeFileStorage.Upload(sizeBytes: 11L * 1024 * 1024)]));

        ex.Message.ShouldContain("মেগাবাইট");
    }

    [Fact]
    public async Task More_files_than_the_school_allows_is_refused()
    {
        var assignment = _world.GivenAssignment();

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Sadman).SubmitAsync(
                assignment.Id, Answer(),
                [
                    FakeFileStorage.Upload("p1.jpg", "image/jpeg"),
                    FakeFileStorage.Upload("p2.jpg", "image/jpeg"),
                    FakeFileStorage.Upload("p3.jpg", "image/jpeg"),
                    FakeFileStorage.Upload("p4.jpg", "image/jpeg")
                ]));
    }

    [Fact]
    public async Task Removing_the_only_file_from_a_text_less_answer_is_refused()
    {
        // It would leave a submission that reads as "submitted" with nothing in it.
        var assignment = _world.GivenAssignment();
        var submission = await _world.SubmissionsAs(_world.Sadman).SubmitAsync(
            assignment.Id, Answer(""), [FakeFileStorage.Upload("khata.jpg", "image/jpeg")]);

        var attachmentId = submission.Attachments.Single().Id;

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .RemoveAttachmentAsync(submission.Id, attachmentId));
    }

    [Fact]
    public async Task A_student_cannot_open_a_classmates_answer_file()
    {
        var assignment = _world.GivenAssignment();
        var tanvirs = _world.GivenSubmission(assignment, _world.Tanvir);
        var attachment = _world.GivenSubmissionAttachment(tanvirs);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .OpenAttachmentAsync(tanvirs.Id, attachment.Id));
    }

    // ------------------------------------------------------------------ editing

    [Fact]
    public async Task A_student_can_edit_their_submission_before_the_deadline()
    {
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddDays(1), allowResubmission: true);
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        var result = await _world.SubmissionsAs(_world.Sadman)
            .UpdateAsync(submission.Id, new UpdateSubmissionRequest("সংশোধিত উত্তর।"));

        result.AnswerText.ShouldBe("সংশোধিত উত্তর।");
        result.UpdatedAt.ShouldBe(TestContext.Now);
    }

    [Fact]
    public async Task A_student_cannot_edit_their_submission_after_the_deadline()
    {
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddHours(1), allowResubmission: true);
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        _ctx.Clock.Advance(TimeSpan.FromHours(2));

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .UpdateAsync(submission.Id, new UpdateSubmissionRequest("দেরি হয়ে গেছে।")));

        ex.Message.ShouldContain("সময়সীমা শেষ");
    }

    [Fact]
    public async Task A_student_cannot_edit_when_the_assignment_forbids_resubmission()
    {
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddDays(1), allowResubmission: false);
        var submission = _world.GivenSubmission(assignment, _world.Sadman);

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .UpdateAsync(submission.Id, new UpdateSubmissionRequest("পরিবর্তন।")));
    }

    [Fact]
    public async Task A_student_cannot_edit_a_submission_that_has_been_graded()
    {
        // Even with time left and resubmission allowed, a graded answer is final.
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddDays(1), allowResubmission: true);
        var submission = _world.GivenSubmission(
            assignment, _world.Sadman, SubmissionStatus.Graded, marks: 70);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .UpdateAsync(submission.Id, new UpdateSubmissionRequest("গোপন সম্পাদনা।")));

        ex.Message.ShouldContain("মূল্যায়ন হয়ে গেছে");
    }

    [Fact]
    public async Task A_student_cannot_edit_a_classmates_submission()
    {
        var assignment = _world.GivenAssignment();
        var tanvirs = _world.GivenSubmission(assignment, _world.Tanvir);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Sadman)
                .UpdateAsync(tanvirs.Id, new UpdateSubmissionRequest("অন্যের কাজ।")));
    }

    [Fact]
    public async Task Work_returned_for_revision_can_be_edited_even_after_the_deadline()
    {
        // The teacher deliberately re-opened it, so the deadline no longer bars the student.
        var assignment = _world.GivenAssignment(
            deadline: TestContext.Now.AddHours(-5), allowResubmission: false);
        var submission = _world.GivenSubmission(
            assignment, _world.Sadman, SubmissionStatus.ReturnedForRevision);

        var result = await _world.SubmissionsAs(_world.Sadman)
            .UpdateAsync(submission.Id, new UpdateSubmissionRequest("বিস্তারিত উত্তর।"));

        result.AnswerText.ShouldBe("বিস্তারিত উত্তর।");
        // Re-submitting past the deadline puts it back in the queue flagged as late.
        result.Status.ShouldBe(SubmissionStatus.Late);
    }

    [Fact]
    public async Task Resubmitting_returned_work_before_the_deadline_goes_back_to_submitted()
    {
        var assignment = _world.GivenAssignment(deadline: TestContext.Now.AddDays(2));
        var submission = _world.GivenSubmission(
            assignment, _world.Sadman, SubmissionStatus.ReturnedForRevision);

        var result = await _world.SubmissionsAs(_world.Sadman)
            .UpdateAsync(submission.Id, new UpdateSubmissionRequest("উন্নত উত্তর।"));

        result.Status.ShouldBe(SubmissionStatus.Submitted);
    }

    [Fact]
    public async Task A_student_only_ever_lists_their_own_submissions()
    {
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman);
        _world.GivenSubmission(assignment, _world.Tanvir);

        var result = await _world.SubmissionsAs(_world.Sadman)
            .ListMineAsync(new SubmissionListQuery());

        result.Items.ShouldHaveSingleItem().StudentId.ShouldBe(_world.Sadman.Id);
    }

    [Fact]
    public async Task A_student_cannot_read_a_classmates_submission_by_id()
    {
        var assignment = _world.GivenAssignment();
        var tanvirs = _world.GivenSubmission(assignment, _world.Tanvir);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.SubmissionsAs(_world.Sadman).GetByIdAsync(tanvirs.Id));
    }
}
