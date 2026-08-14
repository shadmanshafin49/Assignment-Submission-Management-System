using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ams.Application.Services;

public class SettingsService(
    IAppDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<SettingsService> logger) : ISettingsService
{
    public async Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default)
    {
        var settings = await db.AppSettings
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Key)
            .ToListAsync(ct);

        return settings.Select(s => s.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<AppSettingDto>> UpdateAsync(
        UpdateAppSettingsRequest request, CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("কেবল প্রশাসক অ্যাপ্লিকেশন সেটিংস পরিবর্তন করতে পারেন।");

        var now = timeProvider.GetUtcNow();
        var keys = request.Settings.Keys.ToList();
        var existing = await db.AppSettings.Where(s => keys.Contains(s.Key)).ToListAsync(ct);

        // Only known keys are writable — an unrecognised key is a client bug, not a new setting.
        var unknown = keys.Except(existing.Select(s => s.Key)).ToList();
        if (unknown.Count > 0)
            throw new ValidationFailedException($"অজানা সেটিং কী: {string.Join(", ", unknown)}।");

        // Some rows describe the institution rather than configure behaviour. They are readable
        // by the app but not writable through this endpoint.
        var locked = existing.Where(s => !s.IsEditable).Select(s => s.Key).ToList();
        if (locked.Count > 0)
            throw new ForbiddenException(
                $"এই সেটিংগুলো পরিবর্তনযোগ্য নয়: {string.Join(", ", locked)}।");

        foreach (var setting in existing)
        {
            var value = request.Settings[setting.Key].Trim();
            ValidateValue(setting, value);

            setting.Value = value;
            setting.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin {UserId} updated settings: {Keys}",
            currentUser.UserId, string.Join(", ", keys));

        return await GetAllAsync(ct);
    }

    public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken ct = default)
    {
        var raw = await ReadRawAsync(key, ct);
        return bool.TryParse(raw, out var value) ? value : fallback;
    }

    public async Task<int> GetIntAsync(string key, int fallback, CancellationToken ct = default)
    {
        var raw = await ReadRawAsync(key, ct);
        return int.TryParse(raw, out var value) ? value : fallback;
    }

    public async Task<string> GetStringAsync(
        string key, string fallback, CancellationToken ct = default)
    {
        var raw = await ReadRawAsync(key, ct);
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }

    /// <summary>
    /// A setting row declares its own type, so the check lives here rather than in a validator
    /// that would have to keep a second copy of the key list in sync.
    /// </summary>
    private static void ValidateValue(AppSetting setting, string value)
    {
        switch (setting.ValueType)
        {
            case "bool" when !bool.TryParse(value, out _):
                throw new ValidationFailedException(
                    $"'{setting.Key}' এর মান true অথবা false হতে হবে।");

            case "int" when !int.TryParse(value, out _):
                throw new ValidationFailedException(
                    $"'{setting.Key}' এর মান একটি সংখ্যা হতে হবে।");

            case "string" when value.Length == 0:
                throw new ValidationFailedException($"'{setting.Key}' খালি রাখা যাবে না।");
        }
    }

    private async Task<string?> ReadRawAsync(string key, CancellationToken ct) =>
        await db.AppSettings
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
}
