using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Controllers;

/// <summary>Teacher and admin review surface for submissions.</summary>
[ApiController]
[Route("api/submissions")]
[Authorize(Roles = "Teacher,Admin")]
[Produces("application/json")]
public class SubmissionsController(ISubmissionService submissions) : ControllerBase
{
    /// <summary>
    /// Lists submissions. Teachers see work on their own assignments; admins see everything.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SubmissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> List(
        [FromQuery] SubmissionListQuery query, CancellationToken ct)
        => Ok(await submissions.ListAllAsync(query, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SubmissionDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await submissions.GetByIdAsync(id, ct));

    /// <summary>
    /// Awards marks and feedback. Restricted to the teacher who owns the assignment — admins
    /// have oversight but deliberately cannot grade.
    /// </summary>
    [HttpPut("{id:guid}/grade")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SubmissionDto>> Grade(
        Guid id, [FromBody] GradeSubmissionRequest request, CancellationToken ct)
        => Ok(await submissions.GradeAsync(id, request, ct));

    /// <summary>
    /// Changes a submission's status — chiefly to return work for revision, which re-opens
    /// editing for the student and clears any previous grade.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionDto>> UpdateStatus(
        Guid id, [FromBody] UpdateSubmissionStatusRequest request, CancellationToken ct)
        => Ok(await submissions.UpdateStatusAsync(id, request, ct));

    /// <summary>
    /// Downloads a file a student attached to their answer — the marking teacher's route.
    /// Students reach their own files through <c>/api/student/submissions/…</c>; both land on
    /// the same service check, so neither route can widen access on its own.
    /// </summary>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(
        Guid id, Guid attachmentId, CancellationToken ct)
    {
        var file = await submissions.OpenAttachmentAsync(id, attachmentId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
