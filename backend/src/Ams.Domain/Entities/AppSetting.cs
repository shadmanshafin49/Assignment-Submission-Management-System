namespace Ams.Domain.Entities;

/// <summary>
/// A setting an admin can change at runtime, without a deployment.
/// <para>
/// Values are stored as strings and parsed by the settings service, so adding a setting never
/// needs a migration. Each row carries its own Bangla description, which is what the admin
/// settings screen renders as the field label — the UI does not keep a second copy of the list.
/// </para>
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    /// <summary>Bangla label shown on the admin settings page.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// How the value should be rendered and validated: <c>bool</c>, <c>int</c>, <c>string</c>.
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>Groups settings into sections on the settings page.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// False for facts about the institution that the app displays but must not let anyone edit
    /// through a settings form — the EIIN, for instance.
    /// </summary>
    public bool IsEditable { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Every application-level setting the admin controls.
/// <para>
/// These fall into four groups: who the institution is, how the academic week is shaped, what
/// defaults new assignments inherit, and what students are allowed to upload. They are the
/// settings that genuinely differ between schools using the same software — everything else is
/// either a business rule (which belongs in code and tests) or per-assignment configuration
/// (which belongs on the assignment).
/// </para>
/// </summary>
public static class AppSettingKeys
{
    // ---- Institution ----
    public const string SchoolName = "school.name";
    public const string SchoolNameEn = "school.name_en";
    public const string SchoolEiin = "school.eiin";
    public const string SchoolAddress = "school.address";
    public const string AcademicYear = "school.academic_year";

    // ---- Academic week ----
    /// <summary>Teaching days per week. Six here: শনিবার–বৃহস্পতিবার.</summary>
    public const string TeachingDaysPerWeek = "academic.teaching_days_per_week";

    /// <summary>Periods per teaching day. Six, so 36 periods a week.</summary>
    public const string PeriodsPerDay = "academic.periods_per_day";

    // ---- Assignment defaults ----
    /// <summary>
    /// Days between setting an assignment and its deadline. Seven — teachers set work weekly and
    /// it is due at the next class of the same course.
    /// </summary>
    public const string DefaultDeadlineDays = "assignments.default_deadline_days";

    public const string DefaultMaxMarks = "assignments.default_max_marks";
    public const string DefaultAllowLateSubmission = "assignments.default_allow_late_submission";
    public const string DefaultAllowResubmission = "assignments.default_allow_resubmission";

    /// <summary>Whether the class comment thread is open on new assignment posts.</summary>
    public const string DefaultAllowComments = "assignments.default_allow_comments";

    /// <summary>Hard cap on attachments per assignment post, enforced server-side.</summary>
    public const string MaxAttachmentsPerAssignment = "assignments.max_attachments";

    // ---- Submissions and uploads ----
    public const string MaxAttachmentsPerSubmission = "submissions.max_attachments";
    public const string MaxUploadSizeMb = "uploads.max_size_mb";

    /// <summary>Comma-separated list of permitted file extensions, e.g. <c>pdf,txt,jpg</c>.</summary>
    public const string AllowedUploadExtensions = "uploads.allowed_extensions";
}
