namespace Ams.Domain.Entities;

/// <summary>
/// A comment on an assignment post — the class thread, as in Google Classroom. Students ask
/// "স্যার, কোন অনুশীলনী?" and the teacher answers once for everybody.
/// <para>
/// Everyone on the course sees the whole thread, so this is deliberately <b>not</b> a private
/// channel: a student's submission and its feedback stay private, a comment never is. The two
/// are separate types for that reason, rather than one "message" table with a visibility flag
/// that a future query could forget to filter on.
/// </para>
/// </summary>
public class AssignmentComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete. A removed comment leaves a tombstone rather than vanishing, so a thread that
    /// several people replied to does not silently lose the message they were replying to.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public bool IsDeleted => DeletedAt is not null;
}
