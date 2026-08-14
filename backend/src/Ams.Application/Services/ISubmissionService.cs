using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;

namespace Ams.Application.Services;

public interface ISubmissionService
{
    // Student surface

    /// <summary>
    /// Creates the student's answer. Text and files arrive together so the "an answer must
    /// contain something" rule can be enforced in one transaction rather than leaving an empty
    /// submission behind if the follow-up upload never happens.
    /// </summary>
    Task<SubmissionDto> SubmitAsync(
        Guid assignmentId,
        CreateSubmissionRequest request,
        IReadOnlyList<FileUpload> files,
        CancellationToken ct = default);
    Task<SubmissionDto> UpdateAsync(
        Guid submissionId, UpdateSubmissionRequest request, CancellationToken ct = default);
    Task<PagedResult<SubmissionDto>> ListMineAsync(
        SubmissionListQuery query, CancellationToken ct = default);

    // Attachments on the student's own answer
    Task<AttachmentDto> AddAttachmentAsync(
        Guid submissionId, FileUpload file, CancellationToken ct = default);
    Task RemoveAttachmentAsync(
        Guid submissionId, Guid attachmentId, CancellationToken ct = default);

    /// <summary>Opens an answer file — readable by its author, the marking teacher and an admin.</summary>
    Task<StoredFile> OpenAttachmentAsync(
        Guid submissionId, Guid attachmentId, CancellationToken ct = default);

    // Teacher / admin surface
    Task<PagedResult<SubmissionDto>> ListForAssignmentAsync(
        Guid assignmentId, SubmissionListQuery query, CancellationToken ct = default);
    Task<PagedResult<SubmissionDto>> ListAllAsync(
        SubmissionListQuery query, CancellationToken ct = default);
    Task<SubmissionDto> GetByIdAsync(Guid submissionId, CancellationToken ct = default);
    Task<SubmissionDto> GradeAsync(
        Guid submissionId, GradeSubmissionRequest request, CancellationToken ct = default);
    Task<SubmissionDto> UpdateStatusAsync(
        Guid submissionId, UpdateSubmissionStatusRequest request, CancellationToken ct = default);
}
