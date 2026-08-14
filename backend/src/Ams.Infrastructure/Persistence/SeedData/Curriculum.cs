using Ams.Domain.Enums;

namespace Ams.Infrastructure.Persistence.SeedData;

/// <summary>
/// The NCTB curriculum for classes 6–8 as it stands for the 2026 school year.
/// <para>
/// Every value here was read off a primary source and is cited in <c>docs/RESEARCH.md</c>:
/// subject codes from the board's subject/code table, weekly period counts and full marks from
/// NCTB's <i>বিষয় কাঠামো, নম্বর, সময় বণ্টন এবং মূল্যায়ন পদ্ধতি</i> (signed 09-02-2025), the
/// permitted assignment types from <i>বিষয়ভিত্তিক প্রশ্নের ধরন, মূল্যায়ন নির্দেশনা ও নম্বর
/// বিভাজন</i>, and the chapter lists from the সূচিপত্র of each 2026 textbook.
/// </para>
/// </summary>
public static class Curriculum
{
    public const string AcademicYear = "2026";

    // Subject code constants, so the assignment tables below read as the school would say them.
    public const string Bangla1 = "101";
    public const string Bangla2 = "102";
    public const string English1 = "107";
    public const string English2 = "108";
    public const string Math = "109";
    public const string Islam = "111";
    public const string Hindu = "112";
    public const string Science = "127";
    public const string Agriculture = "134";
    public const string PhysicalEducation = "147";
    public const string Arts = "148";
    public const string Bgs = "150";
    public const string Ict = "154";
    public const string WorkAndLife = "155";

    public record SubjectSeed(
        string Code,
        string Name,
        string NameEn,
        int FullMarks,
        int WeeklyPeriods,
        FaithGroup? FaithGroup,
        bool IsOptionalGroup,
        int DisplayOrder,
        AssignmentType[] AllowedTypes,
        IReadOnlyDictionary<int, string> TextbookByClass);

    /// <summary>
    /// The fourteen subjects this school teaches, in the order NCTB lists them.
    /// <para>
    /// Weekly periods for the nine compulsory subjects are NCTB's own numbers and total 29.
    /// The school runs a six-day week (36 periods) and teaches four of the "যেকোনো একটি"
    /// optional-group subjects rather than one, so the remaining seven periods are split
    /// 2/2/2/1 across কৃষিশিক্ষা, শারীরিক শিক্ষা, কর্ম ও জীবনমুখী শিক্ষা and চারু ও কারুকলা.
    /// </para>
    /// </summary>
    public static readonly SubjectSeed[] Subjects =
    [
        new(Bangla1, "বাংলা ১ম পত্র", "Bangla 1st Paper", 100, 3, null, false, 1,
            [
                AssignmentType.CreativeQuestion,
                AssignmentType.MultipleChoice,
                AssignmentType.DescriptiveQuestion,
                AssignmentType.ShortAnswer
            ],
            new Dictionary<int, string> { [6] = "চারুপাঠ", [7] = "সপ্তবর্ণা", [8] = "সাহিত্য-কণিকা" }),

        new(Bangla2, "বাংলা ২য় পত্র", "Bangla 2nd Paper", 50, 2, null, false, 2,
            [
                AssignmentType.Grammar,
                AssignmentType.MultipleChoice,
                AssignmentType.ThemeExpansion,
                AssignmentType.Precis,
                AssignmentType.Letter,
                AssignmentType.Paragraph,
                AssignmentType.Composition
            ],
            Same("বাংলা ব্যাকরণ ও নির্মিতি")),

        new(English1, "ইংরেজি ১ম পত্র", "English 1st Paper", 100, 4, null, false, 3,
            [
                AssignmentType.ReadingTest,
                AssignmentType.WritingTest,
                AssignmentType.MultipleChoice,
                AssignmentType.Paragraph,
                AssignmentType.ShortAnswer
            ],
            Same("English For Today")),

        new(English2, "ইংরেজি ২য় পত্র", "English 2nd Paper", 50, 2, null, false, 4,
            [
                AssignmentType.Grammar,
                AssignmentType.Composition,
                AssignmentType.Letter,
                AssignmentType.Paragraph,
                AssignmentType.MultipleChoice
            ],
            Same("English Grammar and Composition")),

        new(Math, "গণিত", "Mathematics", 100, 5, null, false, 5,
            [
                AssignmentType.MathProblem,
                AssignmentType.CreativeQuestion,
                AssignmentType.ShortAnswer,
                AssignmentType.MultipleChoice
            ],
            Same("গণিত")),

        new(Science, "বিজ্ঞান", "Science", 100, 5, null, false, 6,
            [
                AssignmentType.CreativeQuestion,
                AssignmentType.ShortAnswer,
                AssignmentType.MultipleChoice,
                AssignmentType.PracticalWork,
                AssignmentType.Report
            ],
            Same("বিজ্ঞান")),

        new(Bgs, "বাংলাদেশ ও বিশ্বপরিচয়", "Bangladesh and Global Studies", 100, 3, null, false, 7,
            [
                AssignmentType.CreativeQuestion,
                AssignmentType.ShortAnswer,
                AssignmentType.MultipleChoice,
                AssignmentType.Project
            ],
            Same("বাংলাদেশ ও বিশ্বপরিচয়")),

        new(Ict, "তথ্য ও যোগাযোগ প্রযুক্তি", "Information and Communication Technology",
            50, 2, null, false, 8,
            [
                AssignmentType.MultipleChoice,
                AssignmentType.ShortAnswer,
                AssignmentType.PracticalWork,
                AssignmentType.Report
            ],
            Same("তথ্য ও যোগাযোগ প্রযুক্তি")),

        new(Islam, "ইসলাম ও নৈতিক শিক্ষা", "Islam and Moral Education",
            100, 3, FaithGroup.Islam, false, 9,
            [
                AssignmentType.CreativeQuestion,
                AssignmentType.ShortAnswer,
                AssignmentType.MultipleChoice
            ],
            Same("ইসলাম শিক্ষা")),

        new(Hindu, "হিন্দুধর্ম ও নৈতিক শিক্ষা", "Hindu Religion and Moral Education",
            100, 3, FaithGroup.Hindu, false, 10,
            [
                AssignmentType.CreativeQuestion,
                AssignmentType.ShortAnswer,
                AssignmentType.MultipleChoice
            ],
            Same("হিন্দুধর্ম শিক্ষা")),

        // ---- NCTB item 10: the "যেকোনো একটি" group. This school teaches four of them, and
        // they are assessed by ধারাবাহিক মূল্যায়ন — class work, practical work and assignments —
        // rather than by a written annual paper.
        new(Agriculture, "কৃষিশিক্ষা", "Agriculture Studies", 50, 2, null, true, 11,
            [
                AssignmentType.PracticalWork,
                AssignmentType.Project,
                AssignmentType.Report,
                AssignmentType.ShortAnswer,
                AssignmentType.MultipleChoice
            ],
            Same("কৃষিশিক্ষা")),

        new(PhysicalEducation, "শারীরিক শিক্ষা ও স্বাস্থ্য", "Physical Education and Health",
            50, 2, null, true, 12,
            [
                AssignmentType.MultipleChoice,
                AssignmentType.ShortAnswer,
                AssignmentType.ClassWork,
                AssignmentType.Project
            ],
            Same("শারীরিক শিক্ষা ও স্বাস্থ্য")),

        new(WorkAndLife, "কর্ম ও জীবনমুখী শিক্ষা", "Work and Life Oriented Education",
            50, 2, null, true, 13,
            [
                AssignmentType.MultipleChoice,
                AssignmentType.ShortAnswer,
                AssignmentType.Project,
                AssignmentType.ClassWork
            ],
            Same("কর্ম ও জীবনমুখী শিক্ষা")),

        new(Arts, "চারু ও কারুকলা", "Arts and Crafts", 50, 1, null, true, 14,
            [
                AssignmentType.Drawing,
                AssignmentType.ShortAnswer,
                AssignmentType.PracticalWork,
                AssignmentType.ClassWork
            ],
            Same("চারু ও কারুকলা"))
    ];

    /// <summary>
    /// Chapter, lesson and unit names, read from the সূচিপত্র of each 2026 NCTB textbook.
    /// Keyed by (class level, subject code). These are what assignment titles refer to, which is
    /// the difference between "Assignment 3" and "প্রথম অধ্যায়: স্বাভাবিক সংখ্যা ও ভগ্নাংশ".
    /// </summary>
    public static readonly Dictionary<(int Level, string Subject), string[]> Chapters = new()
    {
        // ---------------------------------------------------------- বাংলা ১ম পত্র
        [(6, Bangla1)] =
        [
            "গদ্য: সততার পুরস্কার — মুহম্মদ শহীদুল্লাহ",
            "গদ্য: নীল নদ আর পিরামিডের দেশ — সৈয়দ মুজতবা আলী",
            "গদ্য: আমাদের লোকশিল্প — কামরুল হাসান",
            "কবিতা: জন্মভূমি — রবীন্দ্রনাথ ঠাকুর",
            "কবিতা: ঝিঙে ফুল — কাজী নজরুল ইসলাম",
            "কবিতা: আসমানি — জসীমউদ্‌দীন"
        ],
        [(7, Bangla1)] =
        [
            "গদ্য: কাবুলিওয়ালা — রবীন্দ্রনাথ ঠাকুর",
            "গদ্য: লখার একুশে — আবুবকর সিদ্দিক",
            "গদ্য: ছবির রং — হাশেম খান",
            "কবিতা: কুলি-মজুর — কাজী নজরুল ইসলাম",
            "কবিতা: আমার বাড়ি — জসীমউদ্‌দীন",
            "কবিতা: সাম্য — সুফিয়া কামাল"
        ],
        [(8, Bangla1)] =
        [
            "গদ্য: অতিথির স্মৃতি — শরৎচন্দ্র চট্টোপাধ্যায়",
            "গদ্য: পড়ে পাওয়া — বিভূতিভূষণ বন্দ্যোপাধ্যায়",
            "গদ্য: সুখী মানুষ — মমতাজউদদীন আহমদ",
            "কবিতা: দুই বিঘা জমি — রবীন্দ্রনাথ ঠাকুর",
            "কবিতা: পাছে লোকে কিছু বলে — কামিনী রায়",
            "কবিতা: একুশের গান — আবদুল গাফ্‌ফার চৌধুরী"
        ],

        // ---------------------------------------------------------- বাংলা ২য় পত্র
        [(6, Bangla2)] =
        [
            "ব্যাকরণ: ভাষা ও বাংলা ভাষা",
            "ব্যাকরণ: ধ্বনিতত্ত্ব",
            "ব্যাকরণ: রূপতত্ত্ব",
            "ব্যাকরণ: বিরামচিহ্ন",
            "নির্মিতি: সারাংশ ও সারমর্ম রচনা",
            "নির্মিতি: ভাবসম্প্রসারণ",
            "নির্মিতি: পত্র রচনা",
            "নির্মিতি: অনুচ্ছেদ রচনা",
            "নির্মিতি: প্রবন্ধ রচনা"
        ],
        [(7, Bangla2)] =
        [
            "ব্যাকরণ: ধ্বনি ও বর্ণ",
            "ব্যাকরণ: শব্দ ও পদ",
            "ব্যাকরণ: বাক্য",
            "নির্মিতি: সারাংশ ও সারমর্ম",
            "নির্মিতি: ভাবসম্প্রসারণ",
            "নির্মিতি: পত্র রচনা",
            "নির্মিতি: অনুচ্ছেদ রচনা",
            "নির্মিতি: প্রবন্ধ রচনা"
        ],
        [(8, Bangla2)] =
        [
            "ব্যাকরণ: সাধু ও চলিত রীতির পার্থক্য",
            "ব্যাকরণ: সন্ধি",
            "ব্যাকরণ: শব্দগঠন",
            "ব্যাকরণ: বাগ্‌ধারা",
            "নির্মিতি: সারাংশ ও সারমর্ম",
            "নির্মিতি: ভাব-সম্প্রসারণ",
            "নির্মিতি: পত্র রচনা — আবেদন পত্র",
            "নির্মিতি: প্রবন্ধ রচনা — শ্রমের মর্যাদা"
        ],

        // -------------------------------------------------------- ইংরেজি ১ম পত্র
        [(6, English1)] =
        [
            "Lesson 1: Going to a New School",
            "Lesson 9: Health is Wealth",
            "Lesson 13: Our Pride",
            "Lesson 20: Hason Raja: The Mystic Bard of Bangladesh",
            "Lesson 21–22: Wonders of the World",
            "Lesson 31: Too Much or Too Little Water"
        ],
        [(7, English1)] =
        [
            "Unit 1: Attention, Please",
            "Unit 3: What are Friends for?",
            "Unit 4: People Who Make a Difference",
            "Unit 6: Leisure",
            "Unit 7: Games and Sports",
            "Unit 9: Climate Change"
        ],
        [(8, English1)] =
        [
            "Unit 1: A Glimpse of Our Culture",
            "Unit 2: Food and Nutrition",
            "Unit 3: Health and Hygiene",
            "Unit 5: Humans and Environment",
            "Unit 8: News! News! News!",
            "Unit 10: Fables"
        ],

        // -------------------------------------------------------- ইংরেজি ২য় পত্র
        [(6, English2)] =
        [
            "Unit 1: Parts of Speech",
            "Unit 2: The Tenses",
            "Unit 3: Articles: a, an, the",
            "Unit 8: Punctuation and Capitalisation",
            "Unit 9: Letters and E-mails",
            "Unit 10: Writing Paragraphs"
        ],
        [(7, English2)] =
        [
            "Unit 3: The Tense",
            "Unit 4: Forms of Verbs",
            "Unit 7: More about Prepositions",
            "Unit 16: Direct Speech and Indirect Speech",
            "Unit 17: Voice",
            "Unit 19: Letter Writing"
        ],
        [(8, English2)] =
        [
            "Unit 4: Degrees of Adjectives",
            "Unit 5: Tenses",
            "Unit 8: Voice",
            "Unit 9: Direct and Indirect Speech",
            "Unit 10: Suffixes and Prefixes",
            "Composition: Letter writing"
        ],

        // ------------------------------------------------------------------ গণিত
        [(6, Math)] =
        [
            "প্রথম অধ্যায়: স্বাভাবিক সংখ্যা ও ভগ্নাংশ",
            "দ্বিতীয় অধ্যায়: অনুপাত ও শতকরা",
            "তৃতীয় অধ্যায়: পূর্ণসংখ্যা",
            "চতুর্থ অধ্যায়: বীজগণিতীয় রাশি",
            "পঞ্চম অধ্যায়: সরল সমীকরণ",
            "ষষ্ঠ অধ্যায়: জ্যামিতির মৌলিক ধারণা",
            "অষ্টম অধ্যায়: তথ্য ও উপাত্ত"
        ],
        [(7, Math)] =
        [
            "প্রথম অধ্যায়: মূলদ ও অমূলদ সংখ্যা",
            "দ্বিতীয় অধ্যায়: সমানুপাত ও লাভ-ক্ষতি",
            "তৃতীয় অধ্যায়: পরিমাপ",
            "চতুর্থ অধ্যায়: বীজগণিতীয় রাশির গুণ ও ভাগ",
            "সপ্তম অধ্যায়: সরল সমীকরণ",
            "নবম অধ্যায়: ত্রিভুজ",
            "একাদশ অধ্যায়: তথ্য ও উপাত্ত"
        ],
        [(8, Math)] =
        [
            "প্রথম অধ্যায়: প্যাটার্ন",
            "দ্বিতীয় অধ্যায়: মুনাফা",
            "তৃতীয় অধ্যায়: পরিমাপ",
            "চতুর্থ অধ্যায়: বীজগণিতীয় সূত্রাবলি ও প্রয়োগ",
            "ষষ্ঠ অধ্যায়: সরল সহসমীকরণ",
            "নবম অধ্যায়: পিথাগোরাসের উপপাদ্য",
            "দশম অধ্যায়: বৃত্ত"
        ],

        // ---------------------------------------------------------------- বিজ্ঞান
        [(6, Science)] =
        [
            "প্রথম অধ্যায়: বৈজ্ঞানিক প্রক্রিয়া ও পরিমাপ",
            "দ্বিতীয় অধ্যায়: জীবজগৎ",
            "পঞ্চম অধ্যায়: সালোকসংশ্লেষণ",
            "অষ্টম অধ্যায়: মিশ্রণ",
            "দশম অধ্যায়: গতি",
            "ত্রয়োদশ অধ্যায়: খাদ্য ও পুষ্টি"
        ],
        [(7, Science)] =
        [
            "প্রথম অধ্যায়: নিম্নশ্রেণির জীব",
            "চতুর্থ অধ্যায়: শ্বসন",
            "পঞ্চম অধ্যায়: পরিপাকতন্ত্র এবং রক্ত সংবহনতন্ত্র",
            "অষ্টম অধ্যায়: শব্দের কথা",
            "নবম অধ্যায়: তাপ ও তাপমাত্রা",
            "চতুর্দশ অধ্যায়: জলবায়ু পরিবর্তন"
        ],
        [(8, Science)] =
        [
            "প্রথম অধ্যায়: প্রাণিজগতের শ্রেণিবিন্যাস",
            "দ্বিতীয় অধ্যায়: জীবের বৃদ্ধি ও বংশগতি",
            "ষষ্ঠ অধ্যায়: পরমাণুর গঠন",
            "অষ্টম অধ্যায়: রাসায়নিক বিক্রিয়া",
            "একাদশ অধ্যায়: আলো",
            "চতুর্দশ অধ্যায়: পরিবেশ এবং বাস্তুতন্ত্র"
        ],

        // ------------------------------------------------- বাংলাদেশ ও বিশ্বপরিচয়
        [(6, Bgs)] =
        [
            "প্রথম অধ্যায়: সমাজ বিবর্তনের ইতিহাস",
            "দ্বিতীয় অধ্যায়: বাংলাদেশের ইতিহাস",
            "তৃতীয় অধ্যায়: বাংলাদেশের সংস্কৃতি ও সমাজ",
            "পঞ্চম অধ্যায়: বাংলাদেশ ও বাংলাদেশের নাগরিক",
            "ষষ্ঠ অধ্যায়: বাংলাদেশের পরিবেশ"
        ],
        [(7, Bgs)] =
        [
            "প্রথম অধ্যায়: বাংলাদেশের মুক্তিসংগ্রাম ও গণআন্দোলন",
            "দ্বিতীয় অধ্যায়: বাংলাদেশের সংস্কৃতি ও সাংস্কৃতিক বৈচিত্র্য",
            "ষষ্ঠ অধ্যায়: বাংলাদেশের জলবায়ু",
            "অষ্টম অধ্যায়: বাংলাদেশের সামাজিক সমস্যা",
            "একাদশ অধ্যায়: বাংলাদেশ ও আন্তর্জাতিক সহযোগিতা"
        ],
        [(8, Bgs)] =
        [
            "প্রথম অধ্যায়: ঔপনিবেশিক যুগ ও বাংলার স্বাধীনতা সংগ্রাম",
            "তৃতীয় অধ্যায়: বাংলাদেশের মুক্তিযুদ্ধ ও গণতান্ত্রিক সংগ্রাম",
            "পঞ্চম অধ্যায়: বাংলাদেশ: রাষ্ট্র ও সরকার ব্যবস্থা",
            "অষ্টম অধ্যায়: বাংলাদেশের বিভিন্ন নৃগোষ্ঠী",
            "একাদশ অধ্যায়: বাংলাদেশে জলবায়ু ও দুর্যোগ মোকাবিলা"
        ],

        // --------------------------------------------- তথ্য ও যোগাযোগ প্রযুক্তি
        [(6, Ict)] =
        [
            "প্রথম অধ্যায়: তথ্য ও যোগাযোগ প্রযুক্তি পরিচিতি",
            "দ্বিতীয় অধ্যায়: তথ্য ও যোগাযোগ প্রযুক্তি সংশ্লিষ্ট যন্ত্রপাতি",
            "তৃতীয় অধ্যায়: তথ্য ও যোগাযোগ প্রযুক্তির নিরাপদ ব্যবহার",
            "চতুর্থ অধ্যায়: ওয়ার্ড প্রসেসিং",
            "পঞ্চম অধ্যায়: ইন্টারনেট পরিচিতি"
        ],
        [(7, Ict)] =
        [
            "প্রথম অধ্যায়: প্রাত্যহিক জীবনে তথ্য ও যোগাযোগ প্রযুক্তি",
            "দ্বিতীয় অধ্যায়: কম্পিউটার-সংশ্লিষ্ট যন্ত্রপাতি",
            "তৃতীয় অধ্যায়: নিরাপদ ও নৈতিক ব্যবহার",
            "চতুর্থ অধ্যায়: ওয়ার্ড প্রসেসিং",
            "পঞ্চম অধ্যায়: শিক্ষায় ইন্টারনেটের ব্যবহার"
        ],
        [(8, Ict)] =
        [
            "প্রথম অধ্যায়: তথ্য ও যোগাযোগ প্রযুক্তির গুরুত্ব",
            "দ্বিতীয় অধ্যায়: কম্পিউটার নেটওয়ার্ক",
            "তৃতীয় অধ্যায়: নিরাপদ ও নৈতিক ব্যবহার",
            "চতুর্থ অধ্যায়: স্প্রেডশিটের ব্যবহার",
            "পঞ্চম অধ্যায়: শিক্ষা ও দৈনন্দিন জীবনে ইন্টারনেটের ব্যবহার"
        ],

        // ------------------------------------------------- ইসলাম ও নৈতিক শিক্ষা
        [(6, Islam)] =
        [
            "প্রথম অধ্যায়: আকাইদ",
            "দ্বিতীয় অধ্যায়: ইবাদত",
            "তৃতীয় অধ্যায়: কুরআন ও হাদিস শিক্ষা",
            "চতুর্থ অধ্যায়: আখলাক",
            "পঞ্চম অধ্যায়: আদর্শ জীবনচরিত"
        ],
        [(7, Islam)] =
        [
            "প্রথম অধ্যায়: আকাইদ",
            "দ্বিতীয় অধ্যায়: ইবাদত",
            "তৃতীয় অধ্যায়: কুরআন ও হাদিস শিক্ষা",
            "চতুর্থ অধ্যায়: আখলাক",
            "পঞ্চম অধ্যায়: আদর্শ জীবনচরিত"
        ],
        [(8, Islam)] =
        [
            "প্রথম অধ্যায়: আকাইদ — ইমান ও নৈতিকতা",
            "দ্বিতীয় অধ্যায়: ইবাদত — যাকাত ও হজ",
            "তৃতীয় অধ্যায়: কুরআন ও হাদিস শিক্ষা",
            "চতুর্থ অধ্যায়: আখলাক — নারীর মর্যাদা ও সমাজসেবা",
            "পঞ্চম অধ্যায়: আদর্শ জীবনচরিত"
        ],

        // ----------------------------------------------- হিন্দুধর্ম ও নৈতিক শিক্ষা
        [(6, Hindu)] =
        [
            "প্রথম অধ্যায়: স্রষ্টা ও সৃষ্টি",
            "দ্বিতীয় অধ্যায়: ধর্মগ্রন্থ",
            "চতুর্থ অধ্যায়: নিত্যকর্ম ও যোগাসন",
            "ষষ্ঠ অধ্যায়: ধর্মীয় উপাখ্যানে নৈতিক শিক্ষা",
            "অষ্টম অধ্যায়: হিন্দুধর্ম ও নৈতিক মূল্যবোধ"
        ],
        [(7, Hindu)] =
        [
            "প্রথম অধ্যায়: ঈশ্বরের স্বরূপ",
            "তৃতীয় অধ্যায়: ধর্মগ্রন্থ",
            "পঞ্চম অধ্যায়: পূজা-পার্বণ",
            "সপ্তম অধ্যায়: অবতার ও আদর্শ জীবনচরিত",
            "অষ্টম অধ্যায়: হিন্দুধর্ম ও নৈতিক মূল্যবোধ"
        ],
        [(8, Hindu)] =
        [
            "প্রথম অধ্যায়: ঈশ্বরের স্বরূপ",
            "দ্বিতীয় অধ্যায়: ধর্মগ্রন্থ",
            "চতুর্থ অধ্যায়: নিত্যকর্ম ও যোগাসন",
            "ষষ্ঠ অধ্যায়: ধর্মীয় উপাখ্যানে নৈতিক শিক্ষা",
            "সপ্তম অধ্যায়: অবতার ও আদর্শ জীবনচরিত"
        ],

        // -------------------------------------------------------------- কৃষিশিক্ষা
        [(6, Agriculture)] =
        [
            "প্রথম অধ্যায়: আমাদের জীবনে কৃষি",
            "দ্বিতীয় অধ্যায়: কৃষি প্রযুক্তি ও যন্ত্রপাতি",
            "চতুর্থ অধ্যায়: কৃষি ও জলবায়ু",
            "পঞ্চম অধ্যায়: কৃষিজ উৎপাদন",
            "ষষ্ঠ অধ্যায়: বনায়ন"
        ],
        [(7, Agriculture)] =
        [
            "প্রথম অধ্যায়: কৃষি এবং আমাদের সংস্কৃতি",
            "দ্বিতীয় অধ্যায়: কৃষি প্রযুক্তি",
            "তৃতীয় অধ্যায়: কৃষি উপকরণ",
            "পঞ্চম অধ্যায়: কৃষিজ উৎপাদন",
            "ষষ্ঠ অধ্যায়: বনায়ন"
        ],
        [(8, Agriculture)] =
        [
            "প্রথম অধ্যায়: বাংলাদেশের কৃষি ও আন্তর্জাতিক প্রেক্ষাপট",
            "দ্বিতীয় অধ্যায়: কৃষিপ্রযুক্তি",
            "চতুর্থ অধ্যায়: কৃষি ও জলবায়ু",
            "পঞ্চম অধ্যায়: কৃষিজ উৎপাদন",
            "ষষ্ঠ অধ্যায়: বনায়ন"
        ],

        // ------------------------------------------------- শারীরিক শিক্ষা ও স্বাস্থ্য
        [(6, PhysicalEducation)] =
        [
            "প্রথম অধ্যায়: শরীরচর্চা ও সুস্থ জীবন",
            "দ্বিতীয় অধ্যায়: স্কাউটিং ও গার্ল গাইডিং",
            "তৃতীয় অধ্যায়: স্বাস্থ্যবিজ্ঞান পরিচিতি ও স্বাস্থ্যসেবা",
            "চতুর্থ অধ্যায়: আমাদের জীবনে বয়ঃসন্ধিকাল",
            "পঞ্চম অধ্যায়: জীবনের জন্য খেলাধুলা"
        ],
        [(7, PhysicalEducation)] =
        [
            "প্রথম অধ্যায়: শরীরচর্চা ও সুস্থজীবন",
            "দ্বিতীয় অধ্যায়: স্কাউটিং ও গার্ল গাইডিং",
            "তৃতীয় অধ্যায়: স্বাস্থ্যবিজ্ঞান পরিচিতি ও স্বাস্থ্যসেবা",
            "চতুর্থ অধ্যায়: বয়ঃসন্ধিকালে ব্যক্তিগত নিরাপত্তা",
            "পঞ্চম অধ্যায়: জীবনের জন্য খেলাধুলা"
        ],
        [(8, PhysicalEducation)] =
        [
            "প্রথম অধ্যায়: শরীরচর্চা ও সুস্থজীবন",
            "দ্বিতীয় অধ্যায়: স্কাউটিং, গার্ল গাইডিং ও বাংলাদেশ রেড ক্রিসেন্ট সোসাইটি",
            "তৃতীয় অধ্যায়: আমাদের জীবনে প্রজনন স্বাস্থ্য",
            "চতুর্থ অধ্যায়: জীবনের জন্য খেলাধুলা"
        ],

        // ---------------------------------------------------- কর্ম ও জীবনমুখী শিক্ষা
        [(6, WorkAndLife)] =
        [
            "প্রথম অধ্যায়: কর্মেই আনন্দ",
            "দ্বিতীয় অধ্যায়: আমাদের প্রয়োজনীয় কাজ",
            "তৃতীয় অধ্যায়: শিক্ষায় সাফল্য"
        ],
        [(7, WorkAndLife)] =
        [
            "প্রথম অধ্যায়: কর্ম ও মানবিকতা",
            "দ্বিতীয় অধ্যায়: পারিবারিক কাজ ও পেশা",
            "তৃতীয় অধ্যায়: শিক্ষা পরিকল্পনা ও কর্মক্ষেত্রে সফলতা"
        ],
        [(8, WorkAndLife)] =
        [
            "প্রথম অধ্যায়: মেধা, কায়িকশ্রম ও আত্ম-অনুসন্ধান",
            "দ্বিতীয় অধ্যায়: আমাদের কাজ — যেগুলো অন্যেরা করে",
            "তৃতীয় অধ্যায়: আমাদের শিক্ষা ও কর্ম"
        ],

        // -------------------------------------------------------- চারু ও কারুকলা
        [(6, Arts)] =
        [
            "তৃতীয় অধ্যায়: বাংলাদেশের লোকশিল্প ও কারুশিল্প",
            "চতুর্থ অধ্যায়: ছবি আঁকার সাধারণ নিয়ম, উপকরণ ও মাধ্যম",
            "পঞ্চম অধ্যায়: ছবি আঁকার অনুশীলন",
            "ষষ্ঠ অধ্যায়: কাগজ ও ফেলনা জিনিস দিয়ে শিল্পকর্ম",
            "সপ্তম অধ্যায়: রং ও রঙের ব্যবহার"
        ],
        [(7, Arts)] =
        [
            "দ্বিতীয় অধ্যায়: চিত্রকলা সর্বকালে সব মানুষের ভাষা",
            "তৃতীয় অধ্যায়: বাংলাদেশের লোকশিল্প ও কারুশিল্প",
            "পঞ্চম অধ্যায়: ছবি আঁকার নানারকম আনন্দদায়ক অনুশীলন",
            "ষষ্ঠ অধ্যায়: বিভিন্ন প্রকার শিল্পকর্ম",
            "সপ্তম অধ্যায়: রঙ ও রঙের ব্যবহার"
        ],
        [(8, Arts)] =
        [
            "প্রথম অধ্যায়: বাংলাদেশের প্রাচীন শিল্পকলা ও ঐতিহ্যের পরিচয়",
            "দ্বিতীয় অধ্যায়: বাংলাদেশের অভ্যুদয়ে চারুশিল্প ও শিল্পীরা",
            "ষষ্ঠ অধ্যায়: বিষয়ভিত্তিক ছবি ও নকশা অঙ্কন",
            "সপ্তম অধ্যায়: বিভিন্ন মাধ্যমের শিল্পকর্ম"
        ]
    };

    private static Dictionary<int, string> Same(string textbook)
        => new() { [6] = textbook, [7] = textbook, [8] = textbook };
}
