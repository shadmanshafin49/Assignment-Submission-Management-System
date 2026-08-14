using Ams.Api.Http;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Controllers;

/// <summary>
/// Teacher and admin view of assignments. Students use <see cref="StudentController"/>, which
/// exposes a deliberately narrower projection — the two exceptions are the class comment thread
/// and attachment downloads, which are shared surfaces where the service decides visibility.
/// <para>
/// Roles are declared per action rather than on the class for exactly that reason: a single
/// class-level role gate would silently lock students out of their own comment thread.
/// </para>
/// </summary>
[ApiController]
[Route("api/assignments")]
[Authorize]
[Produces("application/json")]
public class AssignmentsController(
    IAssignmentService assignments,
    ISubmissionService submissions) : ControllerBase
{
    /// <summary>Lists assignments. Teachers see only work on their own courses; admins see all.</summary>
    [HttpGet]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> List(
        [FromQuery] AssignmentListQuery query, CancellationToken ct)
        => Ok(await assignments.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await assignments.GetByIdAsync(id, ct));

    /// <summary>
    /// Creates a draft assignment on one of the caller's own courses. The type must be one the
    /// course's subject permits, and the deadline defaults to the school's standard week.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssignmentDto>> Create(
        [FromBody] CreateAssignmentRequest request, CancellationToken ct)
    {
        var created = await assignments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentDto>> Update(
        Guid id, [FromBody] UpdateAssignmentRequest request, CancellationToken ct)
        => Ok(await assignments.UpdateAsync(id, request, ct));

    /// <summary>Publishes a draft, making it visible to the students on that course.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentDto>> Publish(Guid id, CancellationToken ct)
        => Ok(await assignments.PublishAsync(id, ct));

    /// <summary>Reverts a published assignment to draft. Refused once submissions exist.</summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentDto>> Unpublish(Guid id, CancellationToken ct)
        => Ok(await assignments.UnpublishAsync(id, ct));

    /// <summary>Deletes an assignment and its attachments. Refused once submissions exist.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await assignments.DeleteAsync(id, ct);
        return NoContent();
    }

    // ------------------------------------------------------------ attachments

    /// <summary>Attaches a question paper or worksheet to the assignment post.</summary>
    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileUploads.RequestSizeLimitBytes)]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttachmentDto>> AddAttachment(
        Guid id, IFormFile file, CancellationToken ct)
    {
        await using var upload = FileUploads.From(file);
        var created = await assignments.AddAttachmentAsync(id, upload.Value, ct);
        return Created($"/api/assignments/{id}/attachments/{created.Id}", created);
    }

    /// <summary>
    /// Downloads an attachment. Access mirrors the assignment itself, so a link to a draft's
    /// worksheet is as invisible to students as the draft is.
    /// </summary>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(
        Guid id, Guid attachmentId, CancellationToken ct)
    {
        var file = await assignments.OpenAttachmentAsync(id, attachmentId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveAttachment(
        Guid id, Guid attachmentId, CancellationToken ct)
    {
        await assignments.RemoveAttachmentAsync(id, attachmentId, ct);
        return NoContent();
    }

    // --------------------------------------------------------------- comments

    /// <summary>The class comment thread. Everyone on the course sees the same thread.</summary>
    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentCommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssignmentCommentDto>>> ListComments(
        Guid id, CancellationToken ct)
        => Ok(await assignments.ListCommentsAsync(id, ct));

    [HttpPost("{id:guid}/comments")]
    [Authorize(Roles = "Teacher,Student")]
    [ProducesResponseType(typeof(AssignmentCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentCommentDto>> AddComment(
        Guid id, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var created = await assignments.AddCommentAsync(id, request, ct);
        return Created($"/api/assignments/{id}/comments/{created.Id}", created);
    }

    [HttpDelete("{id:guid}/comments/{commentId:guid}")]
    [Authorize(Roles = "Teacher,Student")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(
        Guid id, Guid commentId, CancellationToken ct)
    {
        await assignments.DeleteCommentAsync(id, commentId, ct);
        return NoContent();
    }

    // ------------------------------------------------------------ submissions

    /// <summary>Lists every submission made against this assignment, in roll order.</summary>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(PagedResult<SubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> ListSubmissions(
        Guid id, [FromQuery] SubmissionListQuery query, CancellationToken ct)
        => Ok(await submissions.ListForAssignmentAsync(id, query, ct));
}
