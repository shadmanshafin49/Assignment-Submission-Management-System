using Ams.Application.Dtos;
using Ams.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Controllers;

/// <summary>
/// Application-level settings an admin can tune at runtime. These supply the defaults applied
/// to newly created assignments.
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize]
[Produces("application/json")]
public class SettingsController(ISettingsService settings) : ControllerBase
{
    /// <summary>Readable by any authenticated user so the UI can reflect current defaults.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppSettingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppSettingDto>>> GetAll(CancellationToken ct)
        => Ok(await settings.GetAllAsync(ct));

    [HttpPut]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<AppSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AppSettingDto>>> Update(
        [FromBody] UpdateAppSettingsRequest request, CancellationToken ct)
        => Ok(await settings.UpdateAsync(request, ct));
}
