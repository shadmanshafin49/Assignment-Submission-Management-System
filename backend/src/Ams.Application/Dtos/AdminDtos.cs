using Ams.Application.Common;
using Ams.Domain.Enums;

namespace Ams.Application.Dtos;

// ---- Users ----
public record CreateUserRequest(
    string FullName,
    string FullNameEn,
    string Email,
    string Password,
    UserRole Role,
    string? Designation,
    FaithGroup? Faith);

public record UpdateUserRequest(
    string FullName,
    string FullNameEn,
    UserRole Role,
    string? Designation,
    FaithGroup? Faith,
    bool IsActive);

public record UserDto(
    Guid Id,
    string FullName,
    string FullNameEn,
    string Email,
    UserRole Role,
    string? Designation,
    FaithGroup? Faith,
    bool IsActive,
    DateTimeOffset CreatedAt);

public class UserListQuery : PagedQuery
{
    public UserRole? Role { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

// ---- Classes ----
public record CreateClassRoomRequest(
    string Name, string NameEn, string Code, int Level, string? Section, string AcademicYear);

public record UpdateClassRoomRequest(
    string Name, string NameEn, string Code, int Level, string? Section,
    string AcademicYear, bool IsActive);

public record ClassRoomDto(
    Guid Id,
    string Name,
    string NameEn,
    string Code,
    int Level,
    string? Section,
    string AcademicYear,
    bool IsActive,
    int StudentCount,
    int CourseCount);

// ---- Subjects ----
public record CreateSubjectRequest(
    string Name,
    string NameEn,
    string Code,
    string? TextbookName,
    int FullMarks,
    int WeeklyPeriods,
    FaithGroup? FaithGroup,
    bool IsOptionalGroup,
    int DisplayOrder,
    IReadOnlyList<AssignmentType> AllowedAssignmentTypes);

public record UpdateSubjectRequest(
    string Name,
    string NameEn,
    string Code,
    string? TextbookName,
    int FullMarks,
    int WeeklyPeriods,
    FaithGroup? FaithGroup,
    bool IsOptionalGroup,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<AssignmentType> AllowedAssignmentTypes);

public record SubjectDto(
    Guid Id,
    string Name,
    string NameEn,
    string Code,
    string? TextbookName,
    int FullMarks,
    int WeeklyPeriods,
    FaithGroup? FaithGroup,
    bool IsOptionalGroup,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<AssignmentType> AllowedAssignmentTypes);

// ---- Courses (class × subject × teacher) ----
public record CreateCourseRequest(Guid ClassRoomId, Guid SubjectId, Guid? TeacherId);

/// <summary>Staff or re-staff a course. A null teacher leaves the course unstaffed.</summary>
public record AssignCourseTeacherRequest(Guid? TeacherId);

public record CourseDto(
    Guid Id,
    string Code,
    Guid ClassRoomId,
    string ClassRoomName,
    int ClassLevel,
    Guid SubjectId,
    string SubjectName,
    string SubjectCode,
    string? TextbookName,
    int FullMarks,
    int WeeklyPeriods,
    FaithGroup? FaithGroup,
    bool IsOptionalGroup,
    Guid? TeacherId,
    string? TeacherName,
    string? TeacherDesignation,
    string AcademicYear,
    bool IsActive,
    int StudentCount,
    int AssignmentCount,
    int PublishedAssignmentCount,
    IReadOnlyList<AssignmentType> AllowedAssignmentTypes);

public class CourseListQuery : PagedQuery
{
    public Guid? ClassRoomId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TeacherId { get; set; }

    /// <summary>When true, only courses with no teacher assigned.</summary>
    public bool? Unstaffed { get; set; }

    public string? Search { get; set; }
}

// ---- Enrollments ----
public record CreateEnrollmentRequest(Guid StudentId, Guid ClassRoomId, int RollNumber);

public record EnrollmentDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNameEn,
    string StudentEmail,
    FaithGroup? Faith,
    Guid ClassRoomId,
    string ClassRoomName,
    int RollNumber);

public class EnrollmentListQuery : PagedQuery
{
    public Guid? ClassRoomId { get; set; }
    public Guid? StudentId { get; set; }
}

// ---- Routine ----
public record SetRoutinePeriodRequest(Guid ClassRoomId, WeekDay Day, int PeriodIndex, Guid CourseId);

public record RoutineEntryDto(
    Guid CourseId,
    string CourseCode,
    string SubjectName,
    string? TeacherName,
    FaithGroup? FaithGroup);

/// <summary>
/// One period of one day. <c>Entries</c> normally holds a single course; the ধর্ম ও নৈতিক শিক্ষা
/// period holds one per faith group, taught in parallel.
/// </summary>
public record RoutineSlotDto(
    int PeriodIndex,
    string StartTime,
    string EndTime,
    IReadOnlyList<RoutineEntryDto> Entries);

public record RoutineDayDto(WeekDay Day, string DayName, IReadOnlyList<RoutineSlotDto> Periods);

public record ClassRoutineDto(
    Guid ClassRoomId,
    string ClassRoomName,
    string AcademicYear,
    string AssemblyStart,
    string AssemblyEnd,
    int BreakAfterPeriod,
    string BreakStart,
    string BreakEnd,
    IReadOnlyList<RoutineDayDto> Days);

/// <summary>A teacher's own week: only the periods they personally take.</summary>
public record TeacherRoutineSlotDto(
    WeekDay Day,
    int PeriodIndex,
    string StartTime,
    string EndTime,
    Guid CourseId,
    string CourseCode,
    string ClassRoomName,
    string SubjectName);

// ---- Settings ----
public record AppSettingDto(
    string Key,
    string Value,
    string Description,
    string ValueType,
    string Category,
    bool IsEditable,
    int DisplayOrder);

public record UpdateAppSettingsRequest(Dictionary<string, string> Settings);
