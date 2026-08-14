using Ams.Application.Abstractions;
using Ams.Domain.Exceptions;

namespace Ams.Api.Http;

/// <summary>
/// Bridges ASP.NET's <see cref="IFormFile"/> to the application layer's <see cref="FileUpload"/>,
/// so no service has to know about HTTP to accept a file.
/// </summary>
public static class FileUploads
{
    /// <summary>
    /// Hard request ceiling, well above the tunable per-file limit in application settings.
    /// The setting is the policy; this is the backstop that stops a huge body being buffered
    /// before the policy can be applied.
    /// </summary>
    public const long RequestSizeLimitBytes = 32L * 1024 * 1024;

    public static Scope From(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new ValidationFailedException("একটি ফাইল নির্বাচন করুন।");

        var stream = file.OpenReadStream();

        return new Scope(
            new FileUpload(
                file.FileName,
                string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                file.Length,
                stream),
            stream);
    }

    public static IReadOnlyList<Scope> From(IEnumerable<IFormFile>? files)
        => files?.Where(f => f.Length > 0).Select(f => From(f)).ToList() ?? [];

    /// <summary>Keeps the request stream open for exactly as long as the upload is being read.</summary>
    public sealed class Scope(FileUpload value, Stream stream) : IAsyncDisposable
    {
        public FileUpload Value { get; } = value;

        public async ValueTask DisposeAsync() => await stream.DisposeAsync();
    }
}
