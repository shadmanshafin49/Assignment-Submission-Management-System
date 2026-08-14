using Ams.Application.Dtos;
using Ams.Application.Services;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Admin;

/// <summary>
/// Classes, subjects, courses and enrollments — the structure the assignment rules are enforced
/// against. A course is one subject taught to one class by one teacher, and it is what a teacher's
/// permission to set work actually hangs on.
/// </summary>
public class AcademicStructureTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public AcademicStructureTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    /// <summary>A student account that is not on any roster yet.</summary>
    private async Task<UserDto> NewStudentAsync(FaithGroup? faith = FaithGroup.Islam) =>
        await _world.UsersAs(_world.Admin).CreateAsync(new CreateUserRequest(
            FullName: "মোঃ নতুন শিক্ষার্থী",
            FullNameEn: "Md Notun Shikkharthi",
            Email: $"c6r{Guid.NewGuid():N}@student.gcbhs.edu.bd",
            Password: "Password@123",
            Role: UserRole.Student,
            Designation: null,
            Faith: faith));

    // ------------------------------------------------------------- enrollments

    [Fact]
    public async Task An_admin_can_enroll_a_student_in_a_class()
    {
        var student = await NewStudentAsync();

        var result = await _world.AcademicAs(_world.Admin)
            .CreateEnrollmentAsync(new CreateEnrollmentRequest(student.Id, _world.ClassSix.Id, 4));

        result.StudentId.ShouldBe(student.Id);
        result.ClassRoomId.ShouldBe(_world.ClassSix.Id);
        result.RollNumber.ShouldBe(4);
    }

    [Fact]
    public async Task A_teacher_account_cannot_be_enrolled_as_a_student()
    {
        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => _world.AcademicAs(_world.Admin).CreateEnrollmentAsync(
                new CreateEnrollmentRequest(_world.Afsar.Id, _world.ClassSix.Id, 4)));

        ex.Message.ShouldContain("কেবল শিক্ষার্থীকে");
    }

    [Fact]
    public async Task A_student_with_no_faith_group_cannot_be_enrolled()
    {
        // ধর্ম ও নৈতিক শিক্ষা is compulsory, and nothing can decide for them which one they take.
        var student = await NewStudentAsync(faith: null);

        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => _world.AcademicAs(_world.Admin).CreateEnrollmentAsync(
                new CreateEnrollmentRequest(student.Id, _world.ClassSix.Id, 4)));

        ex.Message.ShouldContain("ধর্ম");
    }

    [Fact]
    public async Task A_student_cannot_be_enrolled_in_the_same_class_twice()
    {
        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).CreateEnrollmentAsync(
                new CreateEnrollmentRequest(_world.Sadman.Id, _world.ClassSix.Id, 40)));
    }

    [Fact]
    public async Task A_roll_number_cannot_be_reused_within_a_class()
    {
        // সাদমান already holds roll 1 in class 6.
        var student = await NewStudentAsync();

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).CreateEnrollmentAsync(
                new CreateEnrollmentRequest(student.Id, _world.ClassSix.Id, 1)));

        ex.Message.ShouldContain("1");
    }

    [Fact]
    public async Task The_same_roll_number_is_free_in_a_different_class()
    {
        // Roll 1 exists in both class 6 and class 7 — it is unique per class, not school-wide.
        var student = await NewStudentAsync();

        var result = await _world.AcademicAs(_world.Admin).CreateEnrollmentAsync(
            new CreateEnrollmentRequest(student.Id, _world.ClassSeven.Id, 2));

        result.RollNumber.ShouldBe(2);
    }

    [Fact]
    public async Task A_teacher_cannot_manage_enrollments()
    {
        var student = await NewStudentAsync();

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AcademicAs(_world.Rejaul).CreateEnrollmentAsync(
                new CreateEnrollmentRequest(student.Id, _world.ClassSix.Id, 4)));
    }

    [Fact]
    public async Task A_student_with_submissions_cannot_be_un_enrolled()
    {
        // They would lose access to their own graded history.
        var assignment = _world.GivenAssignment(course: _world.SixMath);
        _world.GivenSubmission(assignment, _world.Sadman);

        _ctx.ClearTracking();
        var enrollment = _ctx.Db.Enrollments.First(
            e => e.StudentId == _world.Sadman.Id && e.ClassRoomId == _world.ClassSix.Id);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).DeleteEnrollmentAsync(enrollment.Id));

        ex.Message.ShouldContain("ভর্তি বাতিল করা যাবে না");
    }

    [Fact]
    public async Task The_class_roster_comes_back_in_roll_order()
    {
        var roster = await _world.AcademicAs(_world.Rejaul)
            .GetClassRosterAsync(_world.ClassSix.Id);

        roster.Select(e => e.RollNumber).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task A_teacher_cannot_read_the_roster_of_a_class_they_do_not_teach()
    {
        // আফসার teaches class 6 only.
        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AcademicAs(_world.Afsar).GetClassRosterAsync(_world.ClassSeven.Id));
    }

    // ----------------------------------------------------------------- courses

    [Fact]
    public async Task Creating_a_course_derives_its_code_from_the_class_and_subject()
    {
        // C07-101 = class 7, বাংলা ১ম পত্র — the board's own numbering, not an invented one.
        var result = await _world.AcademicAs(_world.Admin).CreateCourseAsync(
            new CreateCourseRequest(_world.ClassSeven.Id, _world.Bangla.Id, _world.Afsar.Id));

        result.Code.ShouldBe("C07-101");
        result.TeacherName.ShouldBe("গাজী মোঃ আফসার উদ্দিন");
    }

    [Fact]
    public async Task A_class_cannot_take_the_same_subject_twice()
    {
        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).CreateCourseAsync(
                new CreateCourseRequest(_world.ClassSix.Id, _world.Math.Id, _world.Rejaul.Id)));

        ex.Message.ShouldContain("ইতিমধ্যে");
    }

    [Fact]
    public async Task Staffing_a_course_immediately_unlocks_assignment_creation_for_that_teacher()
    {
        // The end-to-end effect of the admin's decision: আফসার cannot set বাংলা work for class 7
        // before he is put on the course, and can immediately afterwards.
        var course = await _world.AcademicAs(_world.Admin).CreateCourseAsync(
            new CreateCourseRequest(_world.ClassSeven.Id, _world.Bangla.Id, null));

        var request = new CreateAssignmentRequest(
            "সপ্তবর্ণা — কাবুলিওয়ালা", "সৃজনশীল প্রশ্নের উত্তর লেখো।", course.Id,
            AssignmentType.CreativeQuestion, "কাবুলিওয়ালা",
            TestContext.Now.AddDays(5), 10, false, true, true);

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AssignmentsAs(_world.Afsar).CreateAsync(request));

        await _world.AcademicAs(_world.Admin)
            .AssignCourseTeacherAsync(course.Id, new AssignCourseTeacherRequest(_world.Afsar.Id));
        _ctx.ClearTracking();

        var created = await _world.AssignmentsAs(_world.Afsar).CreateAsync(request);
        created.CreatedByTeacherId.ShouldBe(_world.Afsar.Id);
    }

    [Fact]
    public async Task A_student_account_cannot_be_put_on_a_course()
    {
        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => _world.AcademicAs(_world.Admin).CreateCourseAsync(
                new CreateCourseRequest(_world.ClassSeven.Id, _world.Bangla.Id, _world.Sadman.Id)));

        ex.Message.ShouldContain("কেবল শিক্ষক");
    }

    [Fact]
    public async Task A_course_cannot_change_hands_while_the_outgoing_teacher_has_work_on_it()
    {
        // Their assignments would be left unmanageable: they could no longer grade them, and the
        // incoming teacher does not own them either.
        _world.GivenAssignment(course: _world.SixMath, teacher: _world.Rejaul);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).AssignCourseTeacherAsync(
                _world.SixMath.Id, new AssignCourseTeacherRequest(_world.Afsar.Id)));

        ex.Message.ShouldContain("শিক্ষক পরিবর্তন করা যাবে না");
    }

    [Fact]
    public async Task A_course_with_assignments_cannot_be_deleted()
    {
        _world.GivenAssignment(course: _world.SixMath);

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).DeleteCourseAsync(_world.SixMath.Id));
    }

    [Fact]
    public async Task A_teacher_reads_only_their_own_courses()
    {
        // রেজাউল takes গণিত in both classes; he takes nothing else.
        var result = await _world.AcademicAs(_world.Rejaul).GetMyCoursesAsync();

        result.Select(c => c.Code).ShouldBe(["C06-109", "C07-109"], ignoreOrder: true);
    }

    [Fact]
    public async Task A_students_course_list_excludes_another_faiths_religion_course()
    {
        // Class 6 runs four courses; অর্ণব takes three of them.
        var result = await _world.AcademicAs(_world.Arnab).GetMyEnrolledCoursesAsync();

        result.Select(c => c.Code).ShouldBe(["C06-101", "C06-109", "C06-112"], ignoreOrder: true);
    }

    [Fact]
    public async Task A_students_course_list_shows_published_work_only()
    {
        // A draft must not even leak as a number on the student's course card.
        _world.GivenAssignment(status: AssignmentStatus.Draft, course: _world.SixMath);
        _world.GivenAssignment(status: AssignmentStatus.Published, course: _world.SixMath);

        var result = await _world.AcademicAs(_world.Sadman).GetMyEnrolledCoursesAsync();

        result.Single(c => c.Code == "C06-109").AssignmentCount.ShouldBe(1);
    }

    // ---------------------------------------------------------- classes & subjects

    [Fact]
    public async Task A_class_with_courses_cannot_be_deleted()
    {
        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).DeleteClassRoomAsync(_world.ClassSix.Id));
    }

    [Fact]
    public async Task A_teacher_cannot_create_a_class()
    {
        await Should.ThrowAsync<ForbiddenException>(
            () => _world.AcademicAs(_world.Rejaul).CreateClassRoomAsync(
                new CreateClassRoomRequest("অষ্টম শ্রেণি", "Class 8", "C08", 8, null, "2026")));
    }

    [Fact]
    public async Task Any_authenticated_user_can_read_the_class_list_for_dropdowns()
    {
        var result = await _world.AcademicAs(_world.Rejaul)
            .ListClassRoomsAsync(new PagedQueryRequest());

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task An_allowed_assignment_type_cannot_be_withdrawn_while_work_of_that_type_exists()
    {
        // Otherwise published work would be left with a type its own subject no longer permits.
        _world.GivenAssignment(course: _world.SixMath, type: AssignmentType.MathProblem);

        var request = new UpdateSubjectRequest(
            Name: "গণিত", NameEn: "Mathematics", Code: "109", TextbookName: null,
            FullMarks: 100, WeeklyPeriods: 5, FaithGroup: null, IsOptionalGroup: false,
            DisplayOrder: 0, IsActive: true,
            AllowedAssignmentTypes: [AssignmentType.MultipleChoice]);

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.AcademicAs(_world.Admin).UpdateSubjectAsync(_world.Math.Id, request));
    }
}
