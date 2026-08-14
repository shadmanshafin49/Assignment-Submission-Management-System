using Ams.Domain.Enums;

namespace Ams.Domain.Entities;

/// <summary>
/// A piece of work set on one <see cref="Course"/>. Teachers set work weekly, so an assignment
/// carries the week it belongs to and a deadline that defaults to seven days after it is set.
/// </summary>
public class Assignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    /// <summary>The instructions students see. Plain text, rendered with line breaks preserved.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The course this work belongs to — which fixes both the class and the subject.</summary>
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    /// <summary>
    /// What kind of work this is. Must be one of the types allowed for the course's subject,
    /// which is how "বাংলা ১ম পত্র gets সৃজনশীল and MCQ, গণিত gets problems" is enforced rather
    /// than merely documented.
    /// </summary>
    public AssignmentType Type { get; set; }

    /// <summary>
    /// The chapter, lesson or unit the work covers, e.g. "প্রথম অধ্যায়: স্বাভাবিক সংখ্যা ও ভগ্নাংশ"
    /// or "Unit 3: What are Friends for?". Free text — teachers phrase this their own way.
    /// </summary>
    public string? ChapterOrLesson { get; set; }

    /// <summary>The teacher who set it. Only they (or an admin) may modify it.</summary>
    public Guid CreatedByTeacherId { get; set; }
    public User CreatedByTeacher { get; set; } = null!;

    /// <summary>
    /// Which week of the academic year this belongs to (ISO week number). Lets the teacher and
    /// the student both see "this week's work" without date arithmetic in the UI.
    /// </summary>
    public int WeekNumber { get; set; }

    /// <summary>The day the work was set. Drives the default deadline.</summary>
    public DateTimeOffset AssignedOn { get; set; }

    public DateTimeOffset Deadline { get; set; }
    public int MaxMarks { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

    /// <summary>When false, submissions after <see cref="Deadline"/> are rejected outright.</summary>
    public bool AllowLateSubmission { get; set; }

    /// <summary>When false, a student cannot edit a submission once it has been created.</summary>
    public bool AllowResubmission { get; set; } = true;

    /// <summary>When false, the class comment thread on this post is closed.</summary>
    public bool AllowComments { get; set; } = true;

    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Submission> Submissions { get; set; } = [];
    public ICollection<AssignmentAttachment> Attachments { get; set; } = [];
    public ICollection<AssignmentComment> Comments { get; set; } = [];

    // ---- Domain rules -------------------------------------------------------
    // Predicates take `now` explicitly rather than reading the clock, so every
    // deadline-sensitive rule is deterministic under test.

    public bool IsPublished => Status == AssignmentStatus.Published;

    public bool IsPastDeadline(DateTimeOffset now) => now > Deadline;

    /// <summary>Students may only ever see published work.</summary>
    public bool IsVisibleToStudents => IsPublished;

    /// <summary>
    /// A submission may be created when the assignment is published and either the deadline
    /// has not passed or the assignment explicitly permits late work.
    /// </summary>
    public bool CanAcceptSubmission(DateTimeOffset now)
        => IsPublished && (!IsPastDeadline(now) || AllowLateSubmission);

    /// <summary>Marks must fall within 0..MaxMarks inclusive.</summary>
    public bool IsValidMark(int marks) => marks >= 0 && marks <= MaxMarks;
}
