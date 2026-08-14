using Ams.Api.Http;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Controllers;

/// <summary>
/// Everything a student can do. Kept on its own route prefix rather than sharing the teacher
/// endpoints, because the projections differ: nothing here can return a draft, another
/// student's answer, or a course the caller does not take.
/// </summary>
[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
[Produces("application/json")]
public class StudentController(
    IAssignmentService assignments,
    ISubmissionService submissions) : ControllerBase
{
    /// <summary>Published work on the caller's own courses, soonest deadline first.</summary>
    [HttpGet("assignments")]
    [ProducesResponseType(typeof(PagedResult<StudentAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentAssignmentDto>>> ListAssignments(
        [FromQuery] StudentAssignmentListQuery query, CancellationToken ct)
        => Ok(await assignments.ListForStudentAsync(query, ct));

    [HttpGet("assignments/{id:guid}")]
    [ProducesResponseType(typeof(StudentAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentAssignmentDto>> GetAssignment(Guid id, CancellationToken ct)
        => Ok(await assignments.GetForStudentAsync(id, ct));

    /// <summary>
    /// Submits an answer. Text and files travel together in one multipart request, so a
    /// submission is never persisted without an answer in it.
    /// </summary>
    [HttpPost("assignments/{id:guid}/submission")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileUploads.RequestSizeLimitBytes)]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionDto>> Submit(
        Guid id,
        [FromForm] string? answerText,
        [FromForm] IFormFileCollection? files,
        CancellationToken ct)
    {
        var scopes = FileUploads.From(files);

        try
        {
            var created = await submissions.SubmitAsync(
                id,
                new CreateSubmissionRequest(answerText ?? string.Empty),
                scopes.Select(s => s.Value).ToList(),
                ct);

            return Created($"/api/student/submissions/{created.Id}", created);
        }
        finally
        {
            foreach (var scope in scopes)
                await scope.DisposeAsync();
        }
    }

    [HttpPut("submissions/{id:guid}")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionDto>> UpdateSubmission(
        Guid id, [FromBody] UpdateSubmissionRequest request, CancellationToken ct)
        => Ok(await submissions.UpdateAsync(id, request, ct));

    /// <summary>Adds another file to an answer that is still open for editing.</summary>
    [HttpPost("submissions/{id:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileUploads.RequestSizeLimitBytes)]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttachmentDto>> AddSubmissionAttachment(
        Guid id, IFormFile file, CancellationToken ct)
    {
        await using var upload = FileUploads.From(file);
        var created = await submissions.AddAttachmentAsync(id, upload.Value, ct);
        return Created($"/api/student/submissions/{id}/attachments/{created.Id}", created);
    }

    /// <summary>Downloads one of the caller's own answer files.</summary>
    [HttpGet("submissions/{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadSubmissionAttachment(
        Guid id, Guid attachmentId, CancellationToken ct)
    {
        var file = await submissions.OpenAttachmentAsync(id, attachmentId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpDelete("submissions/{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveSubmissionAttachment(
        Guid id, Guid attachmentId, CancellationToken ct)
    {
        await submissions.RemoveAttachmentAsync(id, attachmentId, ct);
        return NoContent();
    }

    /// <summary>The caller's own submissions, with marks and feedback once graded.</summary>
    [HttpGet("submissions")]
    [ProducesResponseType(typeof(PagedResult<SubmissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> ListMySubmissions(
        [FromQuery] SubmissionListQuery query, CancellationToken ct)
        => Ok(await submissions.ListMineAsync(query, ct));
}
