using Ams.Application.Common;
using Ams.Application.Dtos;

namespace Ams.Application.Services;

/// <summary>
/// Admin management of the academic structure: classes, subjects, the courses that pair them,
/// who is enrolled where, and the weekly routine. Grouped into one service because these
/// entities are only ever meaningful in relation to each other.
/// </summary>
public interface IAcademicService
{
    // Classes
    Task<PagedResult<ClassRoomDto>> ListClassRoomsAsync(PagedQueryRequest query, CancellationToken ct = default);
    Task<ClassRoomDto> CreateClassRoomAsync(CreateClassRoomRequest request, CancellationToken ct = default);
    Task<ClassRoomDto> UpdateClassRoomAsync(Guid id, UpdateClassRoomRequest request, CancellationToken ct = default);
    Task DeleteClassRoomAsync(Guid id, CancellationToken ct = default);

    // Subjects
    Task<PagedResult<SubjectDto>> ListSubjectsAsync(PagedQueryRequest query, CancellationToken ct = default);
    Task<SubjectDto> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default);
    Task<SubjectDto> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request, CancellationToken ct = default);
    Task DeleteSubjectAsync(Guid id, CancellationToken ct = default);

    // Courses
    Task<PagedResult<CourseDto>> ListCoursesAsync(CourseListQuery query, CancellationToken ct = default);
    Task<CourseDto> GetCourseAsync(Guid id, CancellationToken ct = default);
    Task<CourseDto> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default);
    Task<CourseDto> AssignCourseTeacherAsync(
        Guid id, AssignCourseTeacherRequest request, CancellationToken ct = default);
    Task DeleteCourseAsync(Guid id, CancellationToken ct = default);

    /// <summary>The courses the calling teacher takes — the only ones they may set work for.</summary>
    Task<IReadOnlyList<CourseDto>> GetMyCoursesAsync(CancellationToken ct = default);

    /// <summary>
    /// The courses the calling student takes: every course of their class, minus religion
    /// courses that do not match their own faith group.
    /// </summary>
    Task<IReadOnlyList<CourseDto>> GetMyEnrolledCoursesAsync(CancellationToken ct = default);

    // Enrollments
    Task<PagedResult<EnrollmentDto>> ListEnrollmentsAsync(EnrollmentListQuery query, CancellationToken ct = default);
    Task<EnrollmentDto> CreateEnrollmentAsync(CreateEnrollmentRequest request, CancellationToken ct = default);
    Task DeleteEnrollmentAsync(Guid id, CancellationToken ct = default);

    /// <summary>The class roster, in roll order — what a teacher sees when marking.</summary>
    Task<IReadOnlyList<EnrollmentDto>> GetClassRosterAsync(Guid classRoomId, CancellationToken ct = default);

    // Routine
    Task<ClassRoutineDto> GetClassRoutineAsync(Guid classRoomId, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherRoutineSlotDto>> GetMyTeachingRoutineAsync(CancellationToken ct = default);
    Task<ClassRoutineDto> SetRoutinePeriodAsync(SetRoutinePeriodRequest request, CancellationToken ct = default);
    Task<ClassRoutineDto> ClearRoutinePeriodAsync(
        Guid classRoomId, Domain.Enums.WeekDay day, int periodIndex, CancellationToken ct = default);
}

public class PagedQueryRequest : PagedQuery
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
