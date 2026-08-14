using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;

namespace Ams.Application.Common;

/// <summary>
/// Hand-written entity → DTO projections. Kept explicit (rather than reflection-based mapping)
/// so it is obvious at a glance which fields cross the API boundary — notably that student-facing
/// DTOs never carry draft state or another student's answer.
/// </summary>
public static class MappingExtensions
{
    public static UserDto ToDto(this User u)
        => new(u.Id, u.FullName, u.FullNameEn, u.Email, u.Role, u.Designation, u.Faith,
            u.IsActive, u.CreatedAt);

    public static SubjectDto ToDto(this Subject s)
        => new(
            s.Id, s.Name, s.NameEn, s.Code, s.TextbookName, s.FullMarks, s.WeeklyPeriods,
            s.FaithGroup, s.IsOptionalGroup, s.DisplayOrder, s.IsActive,
            s.AllowedAssignmentTypes.Select(t => t.Type).OrderBy(t => t).ToList());

    public static ClassRoomDto ToDto(this ClassRoom c, int studentCount, int courseCount)
        => new(c.Id, c.Name, c.NameEn, c.Code, c.Level, c.Section, c.AcademicYear, c.IsActive,
            studentCount, courseCount);

    public static EnrollmentDto ToDto(this Enrollment e)
        => new(e.Id, e.StudentId, e.Student.FullName, e.Student.FullNameEn, e.Student.Email,
            e.Student.Faith, e.ClassRoomId, e.ClassRoom.Name, e.RollNumber);

    public static CourseDto ToDto(
        this Course c, int studentCount, int assignmentCount, int publishedAssignmentCount)
        => new(
            c.Id,
            c.Code,
            c.ClassRoomId,
            c.ClassRoom?.Name ?? string.Empty,
            c.ClassRoom?.Level ?? 0,
            c.SubjectId,
            c.Subject?.Name ?? string.Empty,
            c.Subject?.Code ?? string.Empty,
            c.Subject?.TextbookName,
            c.Subject?.FullMarks ?? 0,
            c.Subject?.WeeklyPeriods ?? 0,
            c.Subject?.FaithGroup,
            c.Subject?.IsOptionalGroup ?? false,
            c.TeacherId,
            c.Teacher?.FullName,
            c.Teacher?.Designation,
            c.AcademicYear,
            c.IsActive,
            studentCount,
            assignmentCount,
            publishedAssignmentCount,
            c.Subject?.AllowedAssignmentTypes.Select(t => t.Type).OrderBy(t => t).ToList() ?? []);

    public static AppSettingDto ToDto(this AppSetting s)
        => new(s.Key, s.Value, s.Description, s.ValueType, s.Category, s.IsEditable, s.DisplayOrder);

    public static AttachmentDto ToDto(this AssignmentAttachment a)
        => new(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedAt);

    public static AttachmentDto ToDto(this SubmissionAttachment a)
        => new(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedAt);

    /// <summary>
    /// A deleted comment keeps its slot in the thread but loses its text — replies to it would
    /// otherwise dangle. <paramref name="canDelete"/> is decided by the caller, which knows who
    /// is asking.
    /// </summary>
    public static AssignmentCommentDto ToDto(this AssignmentComment c, bool canDelete)
        => new(
            c.Id,
            c.AssignmentId,
            c.AuthorId,
            c.Author?.FullName ?? string.Empty,
            c.Author?.Role ?? UserRole.Student,
            c.IsDeleted ? "মন্তব্যটি মুছে ফেলা হয়েছে" : c.Body,
            c.CreatedAt,
            c.UpdatedAt,
            c.IsDeleted,
            canDelete && !c.IsDeleted);

    public static AssignmentDto ToDto(
        this Assignment a,
        int submissionCount,
        int gradedCount,
        int expectedSubmissionCount,
        int commentCount)
        => new(
            a.Id,
            a.Title,
            a.Description,
            a.CourseId,
            a.Course?.Code ?? string.Empty,
            a.Course?.ClassRoomId ?? Guid.Empty,
            a.Course?.ClassRoom?.Name ?? string.Empty,
            a.Course?.SubjectId ?? Guid.Empty,
            a.Course?.Subject?.Name ?? string.Empty,
            a.Type,
            a.ChapterOrLesson,
            a.CreatedByTeacherId,
            a.CreatedByTeacher?.FullName ?? string.Empty,
            a.WeekNumber,
            a.AssignedOn,
            a.Deadline,
            a.MaxMarks,
            a.Status,
            a.AllowLateSubmission,
            a.AllowResubmission,
            a.AllowComments,
            a.PublishedAt,
            a.CreatedAt,
            submissionCount,
            gradedCount,
            expectedSubmissionCount,
            a.Attachments.OrderBy(x => x.UploadedAt).Select(x => x.ToDto()).ToList(),
            commentCount);

    public static SubmissionDto ToDto(
        this Submission s, Assignment assignment, DateTimeOffset now, int? rollNumber = null)
        => new(
            s.Id,
            s.AssignmentId,
            assignment.Title,
            s.StudentId,
            s.Student?.FullName ?? string.Empty,
            s.Student?.Email ?? string.Empty,
            rollNumber,
            s.AnswerText,
            s.Status,
            s.SubmittedAt,
            s.UpdatedAt,
            s.Marks,
            assignment.MaxMarks,
            s.Feedback,
            s.GradedByTeacher?.FullName,
            s.GradedAt,
            s.CanBeEditedByStudent(assignment, now),
            s.Attachments.OrderBy(x => x.UploadedAt).Select(x => x.ToDto()).ToList());

    public static StudentAssignmentDto ToStudentDto(
        this Assignment a, Submission? mySubmission, DateTimeOffset now, int commentCount)
        => new(
            a.Id,
            a.Title,
            a.Description,
            a.CourseId,
            a.Course?.Code ?? string.Empty,
            a.Course?.ClassRoom?.Name ?? string.Empty,
            a.Course?.Subject?.Name ?? string.Empty,
            a.Type,
            a.ChapterOrLesson,
            a.CreatedByTeacher?.FullName ?? string.Empty,
            a.WeekNumber,
            a.AssignedOn,
            a.Deadline,
            a.MaxMarks,
            a.AllowLateSubmission,
            a.AllowResubmission,
            a.AllowComments,
            a.IsPastDeadline(now),
            // A student can act when the window is open and they have not already submitted,
            // or when they still hold an editable submission.
            mySubmission is null
                ? a.CanAcceptSubmission(now)
                : mySubmission.CanBeEditedByStudent(a, now),
            a.Attachments.OrderBy(x => x.UploadedAt).Select(x => x.ToDto()).ToList(),
            commentCount,
            mySubmission?.ToDto(a, now));
}
