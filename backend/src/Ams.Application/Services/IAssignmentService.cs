using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;

namespace Ams.Application.Services;

public interface IAssignmentService
{
    // Teacher / admin
    Task<PagedResult<AssignmentDto>> ListAsync(AssignmentListQuery query, CancellationToken ct = default);
    Task<AssignmentDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AssignmentDto> CreateAsync(CreateAssignmentRequest request, CancellationToken ct = default);
    Task<AssignmentDto> UpdateAsync(Guid id, UpdateAssignmentRequest request, CancellationToken ct = default);
    Task<AssignmentDto> PublishAsync(Guid id, CancellationToken ct = default);
    Task<AssignmentDto> UnpublishAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Attachments
    Task<AttachmentDto> AddAttachmentAsync(Guid id, FileUpload file, CancellationToken ct = default);
    Task RemoveAttachmentAsync(Guid id, Guid attachmentId, CancellationToken ct = default);

    /// <summary>Opens an attachment for download, after checking the caller may see the assignment.</summary>
    Task<StoredFile> OpenAttachmentAsync(Guid id, Guid attachmentId, CancellationToken ct = default);

    // Class comment thread
    Task<IReadOnlyList<AssignmentCommentDto>> ListCommentsAsync(Guid id, CancellationToken ct = default);
    Task<AssignmentCommentDto> AddCommentAsync(
        Guid id, CreateCommentRequest request, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid id, Guid commentId, CancellationToken ct = default);

    // Student
    Task<PagedResult<StudentAssignmentDto>> ListForStudentAsync(
        StudentAssignmentListQuery query, CancellationToken ct = default);
    Task<StudentAssignmentDto> GetForStudentAsync(Guid id, CancellationToken ct = default);
}
