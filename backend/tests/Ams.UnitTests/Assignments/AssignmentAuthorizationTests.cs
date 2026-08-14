using Ams.Application.Dtos;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Assignments;

/// <summary>
/// Who may create and manage assignments. These cover the brief's requirement that role-based
/// access is enforced by the API rather than only hidden in the UI.
/// </summary>
public class AssignmentAuthorizationTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public AssignmentAuthorizationTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    private CreateAssignmentRequest ValidRequest(
        Guid? courseId = null, AssignmentType type = AssignmentType.MathProblem) =>
        new(
            Title: "অনুশীলনী ৪.১ সমাধান",
            Description: "নির্ধারিত ১০টি সমস্যার সমাধান করো।",
            CourseId: courseId ?? _world.SixMath.Id,
            Type: type,
            ChapterOrLesson: "চতুর্থ অধ্যায়: বীজগণিতীয় রাশি",
            Deadline: TestContext.Now.AddDays(7),
            MaxMarks: 20,
            AllowLateSubmission: false,
            AllowResubmission: true,
            AllowComments: true);

    [Fact]
    public async Task The_courses_teacher_can_create_an_assignment()
    {
        var service = _world.AssignmentsAs(_world.Rejaul);

        var created = await service.CreateAsync(ValidRequest());

        created.Title.ShouldBe("অনুশীলনী ৪.১ সমাধান");
        created.CreatedByTeacherId.ShouldBe(_world.Rejaul.Id);
        created.CourseCode.ShouldBe("C06-109");
        // New work always starts as a draft — publishing is a separate, deliberate step.
        created.Status.ShouldBe(AssignmentStatus.Draft);
    }

    [Fact]
    public async Task A_teacher_who_does_not_take_the_course_is_refused()
    {
        // আফসার teaches বাংলা to class 6, not গণিত.
        var service = _world.AssignmentsAs(_world.Afsar);

        var ex = await Should.ThrowAsync<ForbiddenException>(
            () => service.CreateAsync(ValidRequest(courseId: _world.SixMath.Id)));

        ex.Message.ShouldContain("শিক্ষক নন");
    }

    [Fact]
    public async Task A_teacher_cannot_set_a_type_the_subject_does_not_use()
    {
        // ভাবসম্প্রসারণ is a বাংলা ২য় পত্র task; NCTB does not set it in গণিত, and the subject's
        // allowed-type list is what makes that a rule rather than a convention.
        var service = _world.AssignmentsAs(_world.Rejaul);

        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => service.CreateAsync(ValidRequest(type: AssignmentType.ThemeExpansion)));

        ex.Message.ShouldContain("গণিত");
    }

    [Fact]
    public async Task A_teacher_can_set_every_type_the_subject_does_use()
    {
        var service = _world.AssignmentsAs(_world.Rejaul);

        foreach (var type in new[]
                 {
                     AssignmentType.MathProblem, AssignmentType.CreativeQuestion,
                     AssignmentType.ShortAnswer, AssignmentType.MultipleChoice
                 })
        {
            var created = await service.CreateAsync(ValidRequest(type: type));
            created.Type.ShouldBe(type);
        }
    }

    [Fact]
    public async Task A_student_cannot_create_an_assignment()
    {
        var service = _world.AssignmentsAs(_world.Sadman);

        await Should.ThrowAsync<ForbiddenException>(() => service.CreateAsync(ValidRequest()));
    }

    [Fact]
    public async Task An_admin_cannot_create_an_assignment()
    {
        // Admins run the school and observe everything, but teaching is the teacher's job.
        var service = _world.AssignmentsAs(_world.Admin);

        await Should.ThrowAsync<ForbiddenException>(() => service.CreateAsync(ValidRequest()));
    }

    [Fact]
    public async Task A_teacher_cannot_publish_another_teachers_assignment()
    {
        var assignment = _world.GivenAssignment(
            status: AssignmentStatus.Draft, teacher: _world.Rejaul);

        var service = _world.AssignmentsAs(_world.Afsar);

        await Should.ThrowAsync<ForbiddenException>(() => service.PublishAsync(assignment.Id));
    }

    [Fact]
    public async Task A_teacher_cannot_delete_another_teachers_assignment()
    {
        var assignment = _world.GivenAssignment(teacher: _world.Rejaul);
        var service = _world.AssignmentsAs(_world.Afsar);

        await Should.ThrowAsync<ForbiddenException>(() => service.DeleteAsync(assignment.Id));
    }

    [Fact]
    public async Task A_teacher_listing_assignments_sees_only_work_on_their_own_courses()
    {
        _world.GivenAssignment(course: _world.SixMath, teacher: _world.Rejaul);
        _world.GivenAssignment(
            course: _world.SixBangla, teacher: _world.Afsar,
            type: AssignmentType.CreativeQuestion);

        var result = await _world.AssignmentsAs(_world.Rejaul)
            .ListAsync(new AssignmentListQuery());

        result.Items.ShouldAllBe(a => a.CreatedByTeacherId == _world.Rejaul.Id);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_admin_listing_assignments_sees_every_teachers_work()
    {
        _world.GivenAssignment(course: _world.SixMath, teacher: _world.Rejaul);
        _world.GivenAssignment(
            course: _world.SixBangla, teacher: _world.Afsar,
            type: AssignmentType.CreativeQuestion);

        var result = await _world.AssignmentsAs(_world.Admin)
            .ListAsync(new AssignmentListQuery());

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task The_expected_submission_count_excludes_students_of_another_faith()
    {
        // ইসলাম ও নৈতিক শিক্ষা is set for the two Muslim students in class 6, not all three.
        var assignment = _world.GivenAssignment(
            course: _world.SixIslam, teacher: _world.Mukim,
            type: AssignmentType.CreativeQuestion);

        var result = await _world.AssignmentsAs(_world.Mukim).GetByIdAsync(assignment.Id);

        result.ExpectedSubmissionCount.ShouldBe(2);
    }
}
