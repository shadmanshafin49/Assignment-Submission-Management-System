namespace Ams.Domain.Enums;

/// <summary>
/// The kind of work an assignment asks for.
/// <para>
/// These are not invented categories. They are the question and coursework types NCTB
/// prescribes for classes 6–8 in <i>ষষ্ঠ থেকে অষ্টম শ্রেণির বিষয়ভিত্তিক প্রশ্নের ধরন, মূল্যায়ন
/// নির্দেশনা ও নম্বর বিভাজন</i> (2012 curriculum, effective from the 2025 school year), reduced
/// to the ones a teacher would realistically set as homework. See <c>docs/RESEARCH.md</c>.
/// </para>
/// <para>
/// A subject only accepts the types listed for it in <see cref="Entities.SubjectAssignmentType"/>,
/// so a maths teacher cannot set a ভাবসম্প্রসারণ and a Bangla teacher cannot set a lab report.
/// </para>
/// </summary>
public enum AssignmentType
{
    /// <summary>সৃজনশীল প্রশ্ন — 10 marks each. বাংলা ১ম, গণিত, বিজ্ঞান, বাংলাদেশ ও বিশ্বপরিচয়, ধর্ম.</summary>
    CreativeQuestion = 1,

    /// <summary>বহুনির্বাচনি প্রশ্ন (MCQ) — 1 mark each.</summary>
    MultipleChoice = 2,

    /// <summary>সংক্ষিপ্ত-উত্তর প্রশ্ন — 2 marks each.</summary>
    ShortAnswer = 3,

    /// <summary>বর্ণনামূলক প্রশ্ন — বাংলা ১ম পত্র, set on আনন্দপাঠ.</summary>
    DescriptiveQuestion = 4,

    /// <summary>ভাবসম্প্রসারণ — বাংলা ২য় পত্র নির্মিতি.</summary>
    ThemeExpansion = 5,

    /// <summary>সারাংশ / সারমর্ম — বাংলা ২য় পত্র নির্মিতি.</summary>
    Precis = 6,

    /// <summary>পত্র / দরখাস্ত / আবেদনপত্র — বাংলা ২য় পত্র, and Letter/E-mail in English 2nd paper.</summary>
    Letter = 7,

    /// <summary>অনুচ্ছেদ রচনা — বাংলা ২য় পত্র; also "writing a paragraph" in English 1st paper.</summary>
    Paragraph = 8,

    /// <summary>প্রবন্ধ রচনা / composition — বাংলা ২য় পত্র (15 marks) and English 2nd paper.</summary>
    Composition = 9,

    /// <summary>ব্যাকরণ / grammar exercise — বাংলা ২য় পত্র ব্যাকরণ, English 2nd paper Part-A.</summary>
    Grammar = 10,

    /// <summary>Reading test — English 1st paper Part-A (seen and unseen passages).</summary>
    ReadingTest = 11,

    /// <summary>Writing test — English 1st paper Part-B (story completion, dialogue, paragraph).</summary>
    WritingTest = 12,

    /// <summary>গাণিতিক সমস্যা সমাধান — worked problems from an অনুশীলনী.</summary>
    MathProblem = 13,

    /// <summary>ব্যবহারিক কাজ — ICT practical, কৃষিশিক্ষা field work.</summary>
    PracticalWork = 14,

    /// <summary>প্রজেক্ট / অনুসন্ধানমূলক কাজ — the "assignment" component of ধারাবাহিক মূল্যায়ন.</summary>
    Project = 15,

    /// <summary>প্রতিবেদন প্রণয়ন — a written report on practical or investigative work.</summary>
    Report = 16,

    /// <summary>চিত্র অঙ্কন — চারু ও কারুকলা drawing work.</summary>
    Drawing = 17,

    /// <summary>শ্রেণির কাজ — class work written up at home.</summary>
    ClassWork = 18
}
