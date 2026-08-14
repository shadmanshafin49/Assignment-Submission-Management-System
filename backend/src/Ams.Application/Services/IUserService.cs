using Ams.Application.Common;
using Ams.Application.Dtos;

namespace Ams.Application.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
