namespace Ams.Domain.Entities;

/// <summary>
/// Metadata for a file a teacher attached to an assignment — a question paper, a worksheet, a
/// scanned page of the textbook.
/// <para>
/// The bytes live on disk under the configured storage root; only the metadata is in the
/// database. <see cref="StorageKey"/> is a server-generated name, never the uploader's filename,
/// so a hostile name like <c>../../appsettings.json</c> cannot escape the storage directory.
/// The original name is kept separately, purely for display and download.
/// </para>
/// </summary>
public class AssignmentAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    /// <summary>The name the uploader's file had. Display only — never used to build a path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Server-generated relative path inside the storage root.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public Guid UploadedById { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

/// <summary>
/// Metadata for a file a student attached to their submission — a photo of handwritten work, a
/// PDF, a text file. Same storage rules as <see cref="AssignmentAttachment"/>.
/// </summary>
public class SubmissionAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public Guid UploadedById { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
