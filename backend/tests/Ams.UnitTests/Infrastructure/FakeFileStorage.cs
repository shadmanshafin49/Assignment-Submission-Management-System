using System.Collections.Concurrent;
using Ams.Application.Abstractions;

namespace Ams.UnitTests.Infrastructure;

/// <summary>
/// In-memory attachment storage. Lets the upload rules — size limits, extension allow-lists,
/// per-post caps, and "removing the last file empties the answer" — be tested without touching
/// the filesystem.
/// </summary>
public sealed class FakeFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public IReadOnlyCollection<string> Keys => _files.Keys.ToList();

    public async Task<string> SaveAsync(string folder, FileUpload file, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await file.Content.CopyToAsync(buffer, ct);

        var key = $"{folder}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        _files[key] = buffer.ToArray();

        return key;
    }

    public Task<StoredFile?> OpenAsync(
        string storageKey, string fileName, string contentType, CancellationToken ct = default)
        => Task.FromResult(_files.TryGetValue(storageKey, out var bytes)
            ? new StoredFile(new MemoryStream(bytes), contentType, fileName)
            : null);

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        _files.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }

    /// <summary>Builds an upload with the given size without allocating that many bytes.</summary>
    public static FileUpload Upload(
        string fileName = "answer.pdf",
        string contentType = "application/pdf",
        long sizeBytes = 1024)
        => new(fileName, contentType, sizeBytes, new MemoryStream(new byte[Math.Min(sizeBytes, 4096)]));
}
