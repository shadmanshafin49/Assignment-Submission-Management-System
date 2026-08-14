using Ams.Domain.Enums;

namespace Ams.Application.Common;

/// <summary>
/// Bangla labels for the enums the API exposes.
/// <para>
/// These live server-side rather than in the frontend because they are domain vocabulary, not
/// interface copy: "সৃজনশীল প্রশ্ন" is the name of a specific NCTB question type, and the same
/// string has to appear identically in the UI, in Swagger, and anywhere the API is consumed
/// from. The frontend reads them from <c>GET /api/reference</c>.
/// </para>
/// </summary>
public static class BanglaNames
{
    public static string Day(WeekDay day) => day switch
    {
        WeekDay.Saturday => "শনিবার",
        WeekDay.Sunday => "রবিবার",
        WeekDay.Monday => "সোমবার",
        WeekDay.Tuesday => "মঙ্গলবার",
        WeekDay.Wednesday => "বুধবার",
        WeekDay.Thursday => "বৃহস্পতিবার",
        _ => day.ToString()
    };

    public static string AssignmentType(AssignmentType type) => type switch
    {
        Domain.Enums.AssignmentType.CreativeQuestion => "সৃজনশীল প্রশ্ন",
        Domain.Enums.AssignmentType.MultipleChoice => "বহুনির্বাচনি প্রশ্ন",
        Domain.Enums.AssignmentType.ShortAnswer => "সংক্ষিপ্ত-উত্তর প্রশ্ন",
        Domain.Enums.AssignmentType.DescriptiveQuestion => "বর্ণনামূলক প্রশ্ন",
        Domain.Enums.AssignmentType.ThemeExpansion => "ভাবসম্প্রসারণ",
        Domain.Enums.AssignmentType.Precis => "সারাংশ / সারমর্ম",
        Domain.Enums.AssignmentType.Letter => "পত্র / দরখাস্ত",
        Domain.Enums.AssignmentType.Paragraph => "অনুচ্ছেদ রচনা",
        Domain.Enums.AssignmentType.Composition => "প্রবন্ধ রচনা",
        Domain.Enums.AssignmentType.Grammar => "ব্যাকরণ অনুশীলন",
        Domain.Enums.AssignmentType.ReadingTest => "রিডিং টেস্ট",
        Domain.Enums.AssignmentType.WritingTest => "রাইটিং টেস্ট",
        Domain.Enums.AssignmentType.MathProblem => "গাণিতিক সমস্যা সমাধান",
        Domain.Enums.AssignmentType.PracticalWork => "ব্যবহারিক কাজ",
        Domain.Enums.AssignmentType.Project => "প্রজেক্ট / অনুসন্ধানমূলক কাজ",
        Domain.Enums.AssignmentType.Report => "প্রতিবেদন প্রণয়ন",
        Domain.Enums.AssignmentType.Drawing => "চিত্র অঙ্কন",
        Domain.Enums.AssignmentType.ClassWork => "শ্রেণির কাজ",
        _ => type.ToString()
    };

    public static string SubmissionStatus(SubmissionStatus status) => status switch
    {
        Domain.Enums.SubmissionStatus.Submitted => "জমা হয়েছে",
        Domain.Enums.SubmissionStatus.Late => "বিলম্বে জমা",
        Domain.Enums.SubmissionStatus.Graded => "মূল্যায়িত",
        Domain.Enums.SubmissionStatus.ReturnedForRevision => "সংশোধনের জন্য ফেরত",
        _ => status.ToString()
    };

    public static string AssignmentStatus(AssignmentStatus status) => status switch
    {
        Domain.Enums.AssignmentStatus.Draft => "খসড়া",
        Domain.Enums.AssignmentStatus.Published => "প্রকাশিত",
        _ => status.ToString()
    };

    public static string Role(UserRole role) => role switch
    {
        UserRole.Admin => "প্রশাসক",
        UserRole.Teacher => "শিক্ষক",
        UserRole.Student => "শিক্ষার্থী",
        _ => role.ToString()
    };

    public static string Faith(FaithGroup faith) => faith switch
    {
        FaithGroup.Islam => "ইসলাম",
        FaithGroup.Hindu => "হিন্দু",
        FaithGroup.Christian => "খ্রিষ্টান",
        FaithGroup.Buddhist => "বৌদ্ধ",
        _ => faith.ToString()
    };

    private const string BanglaDigits = "০১২৩৪৫৬৭৮৯";

    /// <summary>Renders an integer using Bangla digits, e.g. 36 → "৩৬".</summary>
    public static string Digits(int value)
    {
        var text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Concat(text.Select(c => char.IsAsciiDigit(c) ? BanglaDigits[c - '0'] : c));
    }
}
