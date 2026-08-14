using Ams.Domain.Enums;

namespace Ams.Domain.Entities;

/// <summary>
/// An NCTB subject taught across classes 6–8, e.g. বাংলা ১ম পত্র.
/// <para>
/// <see cref="Code"/> is the real board subject code (101 = বাংলা ১ম পত্র, 109 = গণিত,
/// 154 = তথ্য ও যোগাযোগ প্রযুক্তি …), not an invented slug, because course codes are built from
/// it: <c>C06-101</c>. See <c>docs/RESEARCH.md</c> §3.
/// </para>
/// </summary>
public class Subject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Bangla name as the board writes it, e.g. "বাংলা ১ম পত্র".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>English name, e.g. "Bangla 1st Paper".</summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>Board subject code, e.g. "101". Unique.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>NCTB textbook title for this subject, e.g. "চারুপাঠ" — shown on the course card.</summary>
    public string? TextbookName { get; set; }

    /// <summary>
    /// Full marks per NCTB's subject structure: 100 or 50. Not a cap on assignment marks —
    /// it is what the subject is worth in the annual exam, shown for context.
    /// </summary>
    public int FullMarks { get; set; }

    /// <summary>Weekly period count. The routine builder allocates exactly this many slots.</summary>
    public int WeeklyPeriods { get; set; }

    /// <summary>
    /// Set for the religion subjects. A student only takes the course whose faith group matches
    /// their own; compulsory subjects leave this null and are taken by everyone.
    /// </summary>
    public FaithGroup? FaithGroup { get; set; }

    /// <summary>
    /// True for NCTB item 10 — the "যেকোনো একটি" group (কৃষিশিক্ষা, শারীরিক শিক্ষা ও স্বাস্থ্য,
    /// কর্ম ও জীবনমুখী শিক্ষা, চারু ও কারুকলা …), assessed by ধারাবাহিক মূল্যায়ন rather than a
    /// written annual exam. This school teaches four of them; see <c>docs/RESEARCH.md</c> §4.
    /// </summary>
    public bool IsOptionalGroup { get; set; }

    /// <summary>Display order in the routine and course lists — follows the NCTB table order.</summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public ICollection<Course> Courses { get; set; } = [];

    /// <summary>The assignment types a teacher of this subject is allowed to set.</summary>
    public ICollection<SubjectAssignmentType> AllowedAssignmentTypes { get; set; } = [];
}

/// <summary>
/// Join row saying "a teacher of this subject may set this kind of work".
/// <para>
/// Modelled as a table rather than a list on <see cref="Subject"/> so the rule is queryable and
/// an admin can change it without a deployment. It is enforced when an assignment is created:
/// setting a ভাবসম্প্রসারণ for গণিত is refused.
/// </para>
/// </summary>
public class SubjectAssignmentType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public AssignmentType Type { get; set; }
}
