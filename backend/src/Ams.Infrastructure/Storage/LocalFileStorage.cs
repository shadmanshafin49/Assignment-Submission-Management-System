using Ams.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ams.Infrastructure.Storage;

public class FileStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Directory the files live in. A Docker volume is mounted here.</summary>
    public string Root { get; set; } = "uploads";
}

/// <summary>
/// Disk-backed attachment storage.
/// <para>
/// Two things matter here. First, the stored name is a fresh GUID plus a sanitised extension —
/// the uploader's filename is never used to build a path, so <c>../../appsettings.json</c> is
/// just a display string. Second, every resolved path is re-checked against the storage root
/// before any I/O, so even a key that somehow contained traversal segments cannot escape.
/// </para>
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IOptions<FileStorageOptions> options, ILogger<LocalFileStorage> logger)
    {
        _logger = logger;
        _root = Path.GetFullPath(options.Value.Root);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(string folder, FileUpload file, CancellationToken ct = default)
    {
        var safeFolder = SanitiseSegment(folder);
        var extension = SanitiseExtension(Path.GetExtension(file.FileName));
        var storageKey = $"{safeFolder}/{Guid.NewGuid():N}{extension}";

        var fullPath = ResolveWithinRoot(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var target = File.Create(fullPath);
        await file.Content.CopyToAsync(target, ct);

        _logger.LogInformation(
            "Stored attachment {StorageKey} ({SizeBytes} bytes)", storageKey, file.SizeBytes);

        return storageKey;
    }

    public Task<StoredFile?> OpenAsync(
        string storageKey, string fileName, string contentType, CancellationToken ct = default)
    {
        var fullPath = ResolveWithinRoot(storageKey);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Attachment {StorageKey} is missing from disk", storageKey);
            return Task.FromResult<StoredFile?>(null);
        }

        Stream content = File.OpenRead(fullPath);
        return Task.FromResult<StoredFile?>(new StoredFile(content, contentType, fileName));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolveWithinRoot(storageKey);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Joins the key onto the root and refuses anything that lands outside it. This is belt and
    /// braces — keys are generated, not user-supplied — but it is the check that makes that
    /// property enforced rather than merely intended.
    /// </summary>
    private string ResolveWithinRoot(string storageKey)
    {
        var candidate = Path.GetFullPath(Path.Combine(_root, storageKey));

        if (!candidate.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && candidate != _root)
        {
            throw new InvalidOperationException($"Storage key '{storageKey}' escapes the storage root.");
        }

        return candidate;
    }

    private static string SanitiseSegment(string segment)
    {
        var cleaned = new string(segment
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .ToArray());

        return string.IsNullOrEmpty(cleaned) ? "misc" : cleaned;
    }

    private static string SanitiseExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var cleaned = new string(extension.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
        return cleaned.Length is > 1 and <= 12 ? cleaned.ToLowerInvariant() : string.Empty;
    }
}
