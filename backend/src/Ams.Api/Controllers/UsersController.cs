using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> List(
        [FromQuery] UserListQuery query, CancellationToken ct)
        => Ok(await users.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await users.GetByIdAsync(id, ct));

    /// <summary>
    /// Creates an account with an explicit role. There is no self-registration endpoint by
    /// design — otherwise anyone could sign themselves up as a Teacher or Admin.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var created = await users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => Ok(await users.UpdateAsync(id, request, ct));

    /// <summary>
    /// Deactivates an account. Accounts are never hard-deleted, so assignments and submissions
    /// keep their authorship.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await users.DeactivateAsync(id, ct);
        return NoContent();
    }
}
