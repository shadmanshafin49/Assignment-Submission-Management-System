using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Application.Services;
using Ams.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Controllers;

/// <summary>
/// Academic structure: classes, subjects, the courses that pair them with a teacher, class
/// rosters and the weekly routine.
/// Reads are open to any authenticated user (dropdowns and labels need them); every write is
/// admin-only, enforced both here and again in <c>AcademicService</c>.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class AcademicController(IAcademicService academic) : ControllerBase
{
    // ---------------------------------------------------------------- classes

    [HttpGet("classes")]
    [ProducesResponseType(typeof(PagedResult<ClassRoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClassRoomDto>>> ListClasses(
        [FromQuery] PagedQueryRequest query, CancellationToken ct)
        => Ok(await academic.ListClassRoomsAsync(query, ct));

    [HttpPost("classes")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassRoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassRoomDto>> CreateClass(
        [FromBody] CreateClassRoomRequest request, CancellationToken ct)
    {
        var created = await academic.CreateClassRoomAsync(request, ct);
        return Created($"/api/classes/{created.Id}", created);
    }

    [HttpPut("classes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassRoomDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClassRoomDto>> UpdateClass(
        Guid id, [FromBody] UpdateClassRoomRequest request, CancellationToken ct)
        => Ok(await academic.UpdateClassRoomAsync(id, request, ct));

    [HttpDelete("classes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteClass(Guid id, CancellationToken ct)
    {
        await academic.DeleteClassRoomAsync(id, ct);
        return NoContent();
    }

    /// <summary>The class roster in roll order — visible to its teachers and to admins.</summary>
    [HttpGet("classes/{id:guid}/roster")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> Roster(Guid id, CancellationToken ct)
        => Ok(await academic.GetClassRosterAsync(id, ct));

    // --------------------------------------------------------------- subjects

    [HttpGet("subjects")]
    [ProducesResponseType(typeof(PagedResult<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SubjectDto>>> ListSubjects(
        [FromQuery] PagedQueryRequest query, CancellationToken ct)
        => Ok(await academic.ListSubjectsAsync(query, ct));

    [HttpPost("subjects")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubjectDto>> CreateSubject(
        [FromBody] CreateSubjectRequest request, CancellationToken ct)
    {
        var created = await academic.CreateSubjectAsync(request, ct);
        return Created($"/api/subjects/{created.Id}", created);
    }

    [HttpPut("subjects/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubjectDto>> UpdateSubject(
        Guid id, [FromBody] UpdateSubjectRequest request, CancellationToken ct)
        => Ok(await academic.UpdateSubjectAsync(id, request, ct));

    [HttpDelete("subjects/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSubject(Guid id, CancellationToken ct)
    {
        await academic.DeleteSubjectAsync(id, ct);
        return NoContent();
    }

    // ---------------------------------------------------------------- courses

    /// <summary>Lists course offerings. Teachers see their own; admins see all.</summary>
    [HttpGet("courses")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseDto>>> ListCourses(
        [FromQuery] CourseListQuery query, CancellationToken ct)
        => Ok(await academic.ListCoursesAsync(query, ct));

    [HttpGet("courses/{id:guid}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourse(Guid id, CancellationToken ct)
        => Ok(await academic.GetCourseAsync(id, ct));

    [HttpPost("courses")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDto>> CreateCourse(
        [FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        var created = await academic.CreateCourseAsync(request, ct);
        return CreatedAtAction(nameof(GetCourse), new { id = created.Id }, created);
    }

    /// <summary>Staffs a course, or clears its teacher by sending a null id.</summary>
    [HttpPut("courses/{id:guid}/teacher")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDto>> AssignCourseTeacher(
        Guid id, [FromBody] AssignCourseTeacherRequest request, CancellationToken ct)
        => Ok(await academic.AssignCourseTeacherAsync(id, request, ct));

    [HttpDelete("courses/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCourse(Guid id, CancellationToken ct)
    {
        await academic.DeleteCourseAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// The courses the calling teacher takes — used to populate the create-assignment form so a
    /// teacher is never offered a course they cannot set work for.
    /// </summary>
    [HttpGet("me/courses")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> MyCourses(CancellationToken ct)
        => Ok(await academic.GetMyCoursesAsync(ct));

    /// <summary>The calling student's own courses, filtered by their faith group.</summary>
    [HttpGet("me/enrolled-courses")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> MyEnrolledCourses(CancellationToken ct)
        => Ok(await academic.GetMyEnrolledCoursesAsync(ct));

    // ------------------------------------------------------------ enrollments

    [HttpGet("enrollments")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<EnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EnrollmentDto>>> ListEnrollments(
        [FromQuery] EnrollmentListQuery query, CancellationToken ct)
        => Ok(await academic.ListEnrollmentsAsync(query, ct));

    [HttpPost("enrollments")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentDto>> CreateEnrollment(
        [FromBody] CreateEnrollmentRequest request, CancellationToken ct)
    {
        var created = await academic.CreateEnrollmentAsync(request, ct);
        return Created($"/api/enrollments/{created.Id}", created);
    }

    [HttpDelete("enrollments/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEnrollment(Guid id, CancellationToken ct)
    {
        await academic.DeleteEnrollmentAsync(id, ct);
        return NoContent();
    }

    // ---------------------------------------------------------------- routine

    /// <summary>
    /// The full weekly routine for a class: six days × six periods, with bell times.
    /// Students may only read their own class's.
    /// </summary>
    [HttpGet("classes/{id:guid}/routine")]
    [ProducesResponseType(typeof(ClassRoutineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClassRoutineDto>> ClassRoutine(Guid id, CancellationToken ct)
        => Ok(await academic.GetClassRoutineAsync(id, ct));

    /// <summary>The calling teacher's own week — only the periods they personally take.</summary>
    [HttpGet("me/routine")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(IReadOnlyList<TeacherRoutineSlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherRoutineSlotDto>>> MyRoutine(
        CancellationToken ct)
        => Ok(await academic.GetMyTeachingRoutineAsync(ct));

    [HttpPut("routine")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassRoutineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassRoutineDto>> SetRoutinePeriod(
        [FromBody] SetRoutinePeriodRequest request, CancellationToken ct)
        => Ok(await academic.SetRoutinePeriodAsync(request, ct));

    [HttpDelete("routine")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassRoutineDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClassRoutineDto>> ClearRoutinePeriod(
        [FromQuery] Guid classRoomId,
        [FromQuery] WeekDay day,
        [FromQuery] int periodIndex,
        CancellationToken ct)
        => Ok(await academic.ClearRoutinePeriodAsync(classRoomId, day, periodIndex, ct));
}
