namespace Ams.Application.Abstractions;

/// <summary>A file being uploaded, decoupled from ASP.NET's <c>IFormFile</c>.</summary>
public record FileUpload(string FileName, string ContentType, long SizeBytes, Stream Content);

/// <summary>A stored file being read back.</summary>
public record StoredFile(Stream Content, string ContentType, string FileName);

/// <summary>
/// Where attachment bytes live. The database stores only metadata.
/// <para>
/// Abstracted so the application layer never touches the filesystem: the tests use an in-memory
/// implementation, and swapping the disk-backed one for S3/MinIO in production is a single
/// registration change rather than an edit to the services.
/// </para>
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persists the bytes under <paramref name="folder"/> and returns the storage key.
    /// The key is server-generated — the uploader's filename never reaches the filesystem.
    /// </summary>
    Task<string> SaveAsync(string folder, FileUpload file, CancellationToken ct = default);

    Task<StoredFile?> OpenAsync(string storageKey, string fileName, string contentType,
        CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
