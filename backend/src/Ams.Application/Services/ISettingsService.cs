using Ams.Application.Dtos;

namespace Ams.Application.Services;

public interface ISettingsService
{
    Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppSettingDto>> UpdateAsync(
        UpdateAppSettingsRequest request, CancellationToken ct = default);

    /// <summary>Reads a boolean setting, falling back to <paramref name="fallback"/> when unset or unparseable.</summary>
    Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken ct = default);

    /// <summary>Reads an integer setting, falling back to <paramref name="fallback"/> when unset or unparseable.</summary>
    Task<int> GetIntAsync(string key, int fallback, CancellationToken ct = default);

    /// <summary>Reads a string setting, falling back to <paramref name="fallback"/> when unset or blank.</summary>
    Task<string> GetStringAsync(string key, string fallback, CancellationToken ct = default);
}
