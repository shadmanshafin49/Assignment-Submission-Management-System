using Ams.Domain.Enums;

namespace Ams.Infrastructure.Persistence.SeedData;

/// <summary>
/// The work each subject actually sets, phrased the way a Bangladeshi secondary teacher phrases
/// it.
/// <para>
/// The types and mark values are not invented: they follow NCTB's <i>বিষয়ভিত্তিক প্রশ্নের ধরন,
/// মূল্যায়ন নির্দেশনা ও নম্বর বিভাজন</i> for classes 6–8 — সৃজনশীল প্রশ্ন are worth 10 each,
/// সংক্ষিপ্ত-উত্তর 2 each, বহুনির্বাচনি 1 each, ভাবসম্প্রসারণ and পত্র 5, প্রবন্ধ রচনা 15 — and
/// the optional-group subjects are given the coursework tasks (ব্যবহারিক কাজ, প্রজেক্ট,
/// প্রতিবেদন) that their ধারাবাহিক মূল্যায়ন is made of. See <c>docs/RESEARCH.md</c> §5.
/// </para>
/// <para>
/// <c>{chapter}</c> is replaced with a real chapter, lesson or unit name from the 2026 textbook.
/// </para>
/// </summary>
public static class AssignmentTemplates
{
    public record Template(AssignmentType Type, int MaxMarks, string Title, string Description);

    public static IReadOnlyList<Template> For(string subjectCode, int level) => subjectCode switch
    {
        Curriculum.Bangla1 => Bangla1,
        Curriculum.Bangla2 => Bangla2,
        Curriculum.English1 => English1(level),
        Curriculum.English2 => English2(level),
        Curriculum.Math => Math,
        Curriculum.Science => Science,
        Curriculum.Bgs => Bgs,
        Curriculum.Ict => Ict,
        Curriculum.Islam => Religion,
        Curriculum.Hindu => Religion,
        Curriculum.Agriculture => Agriculture,
        Curriculum.PhysicalEducation => PhysicalEducation,
        Curriculum.WorkAndLife => WorkAndLife,
        Curriculum.Arts => Arts,
        _ => []
    };

    // ------------------------------------------------------------ বাংলা ১ম পত্র

    private static readonly Template[] Bangla1 =
    [
        new(AssignmentType.CreativeQuestion, 10,
            "সৃজনশীল প্রশ্ন — {chapter}",
            "{chapter} — পাঠটি ভালোভাবে পড়ে নিচের উদ্দীপকটির আলোকে সৃজনশীল প্রশ্নের চারটি অংশেরই "
            + "উত্তর লেখো।\n\nউদ্দীপক: শ্রেণিকক্ষে আলোচিত ঘটনাটি স্মরণ করো।\n\n"
            + "ক) জ্ঞানমূলক প্রশ্ন — ১ নম্বর\n"
            + "খ) অনুধাবনমূলক প্রশ্ন — ২ নম্বর\n"
            + "গ) প্রয়োগমূলক প্রশ্ন — ৩ নম্বর\n"
            + "ঘ) উচ্চতর দক্ষতামূলক প্রশ্ন — ৪ নম্বর\n\n"
            + "খাতায় হাতে লিখে ছবি তুলে আপলোড করতে পারো, অথবা সরাসরি টাইপ করে জমা দিতে পারো।"),

        new(AssignmentType.MultipleChoice, 15,
            "বহুনির্বাচনি প্রশ্ন — {chapter}",
            "{chapter} পাঠ থেকে ১৫টি বহুনির্বাচনি প্রশ্ন দেওয়া হলো (সংযুক্ত ফাইলে)। "
            + "প্রতিটি প্রশ্নের সঠিক উত্তরের ক্রমিক নম্বর ও অক্ষর (যেমন ১-খ) লিখে জমা দাও। "
            + "প্রতিটি প্রশ্নের মান ১। সবগুলো প্রশ্নের উত্তর দিতে হবে।"),

        new(AssignmentType.ShortAnswer, 10,
            "সংক্ষিপ্ত প্রশ্নোত্তর — {chapter}",
            "{chapter} পাঠের শেষে দেওয়া কর্ম-অনুশীলন থেকে ৫টি সংক্ষিপ্ত প্রশ্নের উত্তর লেখো। "
            + "প্রতিটি উত্তর ৪-৫ বাক্যের মধ্যে সীমাবদ্ধ রাখবে। প্রতিটি প্রশ্নের মান ২।"),

        new(AssignmentType.DescriptiveQuestion, 10,
            "আনন্দপাঠ — বর্ণনামূলক প্রশ্ন",
            "আনন্দপাঠ বইয়ের নির্ধারিত অংশ পড়ে বর্ণনামূলক প্রশ্নের উত্তর দাও।\n\n"
            + "ক) কাহিনির সংক্ষিপ্ত পরিচয় দাও — ৩ নম্বর\n"
            + "খ) চরিত্রটির বৈশিষ্ট্য নিজের ভাষায় বিশ্লেষণ করো — ৭ নম্বর")
    ];

    // ------------------------------------------------------------ বাংলা ২য় পত্র

    private static readonly Template[] Bangla2 =
    [
        new(AssignmentType.ThemeExpansion, 5,
            "ভাবসম্প্রসারণ — ‘পরিশ্রম সৌভাগ্যের প্রসূতি’",
            "নিচের ভাবটি সম্প্রসারিত করে লেখো:\n\n‘পরিশ্রম সৌভাগ্যের প্রসূতি’\n\n"
            + "মূলভাব, সম্প্রসারিত ভাব ও উপসংহার — এই তিনটি অংশ যেন স্পষ্ট থাকে। "
            + "আনুমানিক ১৫০-২০০ শব্দ। নম্বর ৫।"),

        new(AssignmentType.Precis, 5,
            "সারাংশ লিখন",
            "সংযুক্ত অনুচ্ছেদটি মনোযোগ দিয়ে পড়ে তার সারাংশ লেখো। "
            + "মূল অনুচ্ছেদের এক-তৃতীয়াংশের বেশি লেখা যাবে না, নিজের ভাষায় লিখতে হবে এবং "
            + "মূল বক্তব্যের কোনো গুরুত্বপূর্ণ দিক বাদ দেওয়া যাবে না। নম্বর ৫।"),

        new(AssignmentType.Letter, 5,
            "পত্র রচনা — বিদ্যালয়ে অনুপস্থিতির জন্য ছুটির দরখাস্ত",
            "অসুস্থতার কারণে তিন দিন বিদ্যালয়ে অনুপস্থিত থাকার অনুমতি চেয়ে প্রধান শিক্ষক বরাবর "
            + "একটি দরখাস্ত লেখো।\n\nদরখাস্তের কাঠামো ঠিক রাখবে — তারিখ, প্রাপক, বিষয়, "
            + "সম্বোধন, মূল বক্তব্য ও নিবেদক অংশ। নম্বর ৫।"),

        new(AssignmentType.Paragraph, 5,
            "অনুচ্ছেদ রচনা — ‘আমার প্রিয় ঋতু’",
            "‘আমার প্রিয় ঋতু’ শিরোনামে একটি অনুচ্ছেদ লেখো। "
            + "অনুচ্ছেদটি এক প্যারাগ্রাফে লিখতে হবে, কোনো উপশিরোনাম দেওয়া যাবে না। "
            + "আনুমানিক ১৫০ শব্দ। নম্বর ৫।"),

        new(AssignmentType.Composition, 15,
            "প্রবন্ধ রচনা — ‘শ্রমের মর্যাদা’",
            "‘শ্রমের মর্যাদা’ শিরোনামে একটি প্রবন্ধ রচনা করো।\n\n"
            + "সংকেত: ভূমিকা; শ্রমের প্রকারভেদ; শ্রমের মর্যাদা সম্পর্কে ধর্মীয় ও সামাজিক দৃষ্টিভঙ্গি; "
            + "শ্রমবিমুখতার কুফল; উপসংহার।\n\nআনুমানিক ৪০০-৫০০ শব্দ। নম্বর ১৫।"),

        new(AssignmentType.Grammar, 15,
            "ব্যাকরণ অনুশীলন — {chapter}",
            "{chapter} — এই অধ্যায় থেকে ১৫টি বহুনির্বাচনি প্রশ্ন সংযুক্ত ফাইলে দেওয়া হলো। "
            + "প্রতিটির সঠিক উত্তর লিখে জমা দাও। প্রতিটি প্রশ্নের মান ১।")
    ];

    // ---------------------------------------------------------- ইংরেজি ১ম পত্র

    private static Template[] English1(int level) =>
    [
        new(AssignmentType.ReadingTest, 20,
            "Reading Test — {chapter}",
            "Read the seen passage from {chapter} in English For Today and answer the following:\n\n"
            + "1. Multiple choice questions (7 × 1 = 7)\n"
            + "2. Answer the questions in your own words (5 × 2 = 10)\n"
            + "3. Fill in the gaps with suitable words (5 × ... included above)\n\n"
            + "Write your answers in full sentences. Total 20 marks."),

        new(AssignmentType.WritingTest, 20,
            "Writing Test — Paragraph and Dialogue",
            $"Complete both writing tasks based on {(level == 6 ? "Lesson" : "Unit")} work done in class.\n\n"
            + $"1. Write a paragraph in about {(level == 6 ? "120" : "150")} words on the topic given "
            + "in the attached sheet. (10 marks)\n"
            + "2. Write a dialogue between two friends on the same topic. (10 marks)\n\n"
            + "Hand-written answers may be photographed and uploaded."),

        new(AssignmentType.ShortAnswer, 10,
            "Answering questions from the poem",
            "Read the poem in English For Today and answer any 5 of the 8 questions on the attached "
            + "sheet. Each answer carries 2 marks. Write in complete sentences."),

        new(AssignmentType.MultipleChoice, 7,
            "MCQ — {chapter}",
            "Answer the 7 multiple choice questions on the attached sheet, based on the seen "
            + "passage from {chapter}. Write the question number and the correct option "
            + "(for example, 1-b). Each question carries 1 mark.")
    ];

    // ---------------------------------------------------------- ইংরেজি ২য় পত্র

    private static Template[] English2(int level) =>
    [
        new(AssignmentType.Grammar, 15,
            "Grammar — {chapter}",
            "Complete the grammar exercises from {chapter} in the attached worksheet:\n\n"
            + "1. Gap filling with clues (10 × 0.5 = 5)\n"
            + "2. Right form of verbs (5 × 1 = 5)\n"
            + $"3. {(level >= 7 ? "Narration — change into indirect speech" : "Substitution table")} (5 × 1 = 5)\n\n"
            + "Total 15 marks."),

        new(AssignmentType.Letter, 8,
            "Letter / E-mail writing",
            "Write an informal e-mail to your friend describing how you spent your last vacation. "
            + "Use the correct format — subject line, greeting, body and closing. 8 marks."),

        new(AssignmentType.Composition, 12,
            "Short composition",
            $"Write a short composition in about {(level == 6 ? "200" : "250")} words on the topic "
            + "given in class. Plan your writing first: an introduction, two or three body "
            + "paragraphs and a conclusion. 12 marks."),

        new(AssignmentType.Paragraph, 10,
            "Writing a paragraph",
            "Write a paragraph using the clues given in the attached sheet. Remember that a "
            + "paragraph is written as a single block of text with a topic sentence, supporting "
            + "sentences and a concluding sentence. 10 marks.")
    ];

    // ------------------------------------------------------------------- গণিত

    private static readonly Template[] Math =
    [
        new(AssignmentType.MathProblem, 20,
            "অনুশীলনী সমাধান — {chapter}",
            "{chapter} — এই অধ্যায়ের অনুশীলনী থেকে নির্ধারিত ১০টি সমস্যার সমাধান করো।\n\n"
            + "প্রতিটি সমাধানে ধাপগুলো স্পষ্টভাবে দেখাতে হবে; কেবল উত্তর লিখলে নম্বর দেওয়া হবে না। "
            + "খাতায় সমাধান করে ছবি তুলে আপলোড করো। প্রতিটি সমস্যার মান ২।"),

        new(AssignmentType.CreativeQuestion, 10,
            "সৃজনশীল প্রশ্ন — {chapter}",
            "{chapter} — নিচের সৃজনশীল প্রশ্নটির ক, খ ও গ — তিনটি অংশের সমাধান করো।\n\n"
            + "ক) ৩ নম্বর\nখ) ৩ নম্বর\nগ) ৪ নম্বর\n\n"
            + "প্রয়োজনীয় সূত্র উল্লেখ করে ধাপে ধাপে সমাধান করবে।"),

        new(AssignmentType.ShortAnswer, 20,
            "সংক্ষিপ্ত-উত্তর প্রশ্ন — {chapter}",
            "{chapter} অধ্যায় থেকে ১৫টি সংক্ষিপ্ত-উত্তর প্রশ্ন সংযুক্ত ফাইলে দেওয়া হলো। "
            + "যেকোনো ১০টির উত্তর দাও। প্রতিটির মান ২।"),

        new(AssignmentType.MultipleChoice, 30,
            "বহুনির্বাচনি অনুশীলন",
            "পাটিগণিত, বীজগণিত, জ্যামিতি এবং তথ্য ও উপাত্ত — চারটি বিভাগ থেকে মোট ৩০টি "
            + "বহুনির্বাচনি প্রশ্ন দেওয়া হলো। সবগুলোর উত্তর দিতে হবে। প্রতিটির মান ১।")
    ];

    // ---------------------------------------------------------------- বিজ্ঞান

    private static readonly Template[] Science =
    [
        new(AssignmentType.CreativeQuestion, 10,
            "সৃজনশীল প্রশ্ন — {chapter}",
            "{chapter} — উদ্দীপকের আলোকে সৃজনশীল প্রশ্নের চারটি অংশের উত্তর দাও।\n\n"
            + "ক) জ্ঞানমূলক — ১ নম্বর\nখ) অনুধাবনমূলক — ২ নম্বর\n"
            + "গ) প্রয়োগমূলক — ৩ নম্বর\nঘ) উচ্চতর দক্ষতামূলক — ৪ নম্বর\n\n"
            + "প্রয়োজনে চিত্র এঁকে ব্যাখ্যা করবে।"),

        new(AssignmentType.ShortAnswer, 20,
            "সংক্ষিপ্ত-উত্তর প্রশ্ন — {chapter}",
            "{chapter} অধ্যায় থেকে ১০টি সংক্ষিপ্ত-উত্তর প্রশ্নের উত্তর দাও। "
            + "প্রতিটি উত্তর ৩-৪ বাক্যে সীমাবদ্ধ রাখবে। প্রতিটির মান ২।"),

        new(AssignmentType.PracticalWork, 15,
            "ব্যবহারিক কাজ — {chapter}",
            "{chapter} — শ্রেণিকক্ষে দেখানো পরীক্ষণটি বাড়িতে নিরাপদে সম্পন্ন করো এবং "
            + "নিচের ছকে ফলাফল লিপিবদ্ধ করো:\n\n"
            + "১. পরীক্ষণের নাম\n২. প্রয়োজনীয় উপকরণ\n৩. কার্যপদ্ধতি\n৪. পর্যবেক্ষণ\n৫. সিদ্ধান্ত\n\n"
            + "কাজের ছবি বা হাতে আঁকা চিত্র সংযুক্ত করলে ভালো হয়।"),

        new(AssignmentType.MultipleChoice, 30,
            "বহুনির্বাচনি অনুশীলন — {chapter}",
            "{chapter} সহ পঠিত অধ্যায়গুলো থেকে ৩০টি বহুনির্বাচনি প্রশ্ন দেওয়া হলো। "
            + "প্রতিটি অধ্যায় থেকে অন্তত ২টি প্রশ্ন রয়েছে। সবগুলোর উত্তর দিতে হবে। প্রতিটির মান ১।")
    ];

    // ------------------------------------------------- বাংলাদেশ ও বিশ্বপরিচয়

    private static readonly Template[] Bgs =
    [
        new(AssignmentType.CreativeQuestion, 10,
            "সৃজনশীল প্রশ্ন — {chapter}",
            "{chapter} — উদ্দীপকটি পড়ে সৃজনশীল প্রশ্নের ক, খ, গ ও ঘ অংশের উত্তর লেখো।\n\n"
            + "ক) ১ নম্বর\nখ) ২ নম্বর\nগ) ৩ নম্বর\nঘ) ৪ নম্বর\n\n"
            + "ঘ অংশে নিজের মতামত যুক্তিসহ উপস্থাপন করবে।"),

        new(AssignmentType.ShortAnswer, 20,
            "সংক্ষিপ্ত-উত্তর প্রশ্ন — {chapter}",
            "{chapter} অধ্যায় থেকে ১০টি সংক্ষিপ্ত-উত্তর প্রশ্নের উত্তর দাও। প্রতিটির মান ২।"),

        new(AssignmentType.Project, 10,
            "অনুসন্ধানমূলক কাজ — {chapter}",
            "{chapter} — এই অধ্যায়ের সাথে সম্পর্কিত একটি বিষয়ে নিজের এলাকায় ছোট পরিসরে "
            + "অনুসন্ধান করো এবং একটি প্রতিবেদন তৈরি করো।\n\n"
            + "প্রতিবেদনে থাকবে: শিরোনাম, উদ্দেশ্য, তথ্য সংগ্রহের পদ্ধতি, প্রাপ্ত তথ্য "
            + "(সারণি বা লেখচিত্রসহ) এবং সিদ্ধান্ত। ৩০০-৪০০ শব্দ।"),

        new(AssignmentType.MultipleChoice, 30,
            "বহুনির্বাচনি অনুশীলন",
            "পঠিত অধ্যায়গুলো থেকে ৩০টি বহুনির্বাচনি প্রশ্ন দেওয়া হলো। "
            + "সবগুলোর উত্তর দিতে হবে। প্রতিটির মান ১।")
    ];

    // --------------------------------------------- তথ্য ও যোগাযোগ প্রযুক্তি

    private static readonly Template[] Ict =
    [
        new(AssignmentType.PracticalWork, 15,
            "ব্যবহারিক কাজ — {chapter}",
            "{chapter} — কম্পিউটার ল্যাবে দেখানো কাজটি নিজে করে দেখাও এবং প্রতিটি ধাপের "
            + "স্ক্রিনশট বা ছবি সংযুক্ত করো।\n\n"
            + "মূল্যায়ন: যন্ত্র/উপকরণ সঠিকভাবে ব্যবহার — ৫, ধাপ অনুসরণ — ৫, "
            + "ফলাফল উপস্থাপন — ৫।"),

        new(AssignmentType.ShortAnswer, 10,
            "সংক্ষিপ্ত প্রশ্নোত্তর — {chapter}",
            "{chapter} অধ্যায় থেকে ৮টি সংক্ষিপ্ত প্রশ্ন দেওয়া হলো; যেকোনো ৫টির উত্তর দাও। "
            + "প্রতিটির মান ২।"),

        new(AssignmentType.Report, 5,
            "প্রতিবেদন প্রণয়ন — {chapter}",
            "{chapter} — সম্পন্ন করা ব্যবহারিক কাজের উপর একটি সংক্ষিপ্ত প্রতিবেদন লেখো। "
            + "কাজের উদ্দেশ্য, ব্যবহৃত সফটওয়্যার বা যন্ত্র, ধাপসমূহ এবং শেখা বিষয়গুলো উল্লেখ করবে।"),

        new(AssignmentType.MultipleChoice, 15,
            "বহুনির্বাচনি প্রশ্ন — {chapter}",
            "{chapter} অধ্যায় থেকে ১৫টি বহুনির্বাচনি প্রশ্নের উত্তর দাও। "
            + "সবগুলোর উত্তর দিতে হবে। প্রতিটির মান ১।")
    ];

    // ------------------------------------------------------ ধর্ম ও নৈতিক শিক্ষা

    private static readonly Template[] Religion =
    [
        new(AssignmentType.CreativeQuestion, 10,
            "সৃজনশীল প্রশ্ন — {chapter}",
            "{chapter} — উদ্দীপকের আলোকে সৃজনশীল প্রশ্নের চারটি অংশের উত্তর দাও।\n\n"
            + "ক) ১ নম্বর\nখ) ২ নম্বর\nগ) ৩ নম্বর\nঘ) ৪ নম্বর\n\n"
            + "ঘ অংশে শিক্ষণীয় দিকটি নিজের ভাষায় ব্যাখ্যা করবে।"),

        new(AssignmentType.ShortAnswer, 20,
            "সংক্ষিপ্ত-উত্তর প্রশ্ন — {chapter}",
            "{chapter} অধ্যায় থেকে ১৫টি সংক্ষিপ্ত-উত্তর প্রশ্ন দেওয়া হলো; ১০টির উত্তর দাও। "
            + "প্রতিটির মান ২।"),

        new(AssignmentType.MultipleChoice, 30,
            "বহুনির্বাচনি অনুশীলন — {chapter}",
            "{chapter} সহ পঠিত অধ্যায়গুলো থেকে ৩০টি বহুনির্বাচনি প্রশ্ন দেওয়া হলো। "
            + "সবগুলোর উত্তর দিতে হবে। প্রতিটির মান ১।")
    ];

    // -------------------------------------------------------------- কৃষিশিক্ষা

    private static readonly Template[] Agriculture =
    [
        new(AssignmentType.PracticalWork, 10,
            "ব্যবহারিক কাজ — {chapter}",
            "{chapter} — বাড়িতে বা বিদ্যালয়ের বাগানে একটি ছোট প্লটে কাজটি সম্পন্ন করো।\n\n"
            + "কাজের প্রতিটি ধাপের ছবি তুলে রাখবে এবং কোন উপকরণ কীভাবে ব্যবহার করেছ তা "
            + "লিখে জমা দেবে। কাজটি নিরাপদে ও বড়দের তত্ত্বাবধানে করবে।"),

        new(AssignmentType.Project, 10,
            "প্রজেক্ট — {chapter}",
            "{chapter} — নিজের এলাকার একজন কৃষকের সাথে কথা বলে তথ্য সংগ্রহ করো এবং "
            + "একটি প্রতিবেদন তৈরি করো।\n\n"
            + "প্রতিবেদনে থাকবে: কৃষকের পরিচয়, চাষকৃত ফসল, ব্যবহৃত প্রযুক্তি ও উপকরণ, "
            + "সমস্যা এবং তোমার সুপারিশ।"),

        new(AssignmentType.ShortAnswer, 10,
            "সংক্ষিপ্ত প্রশ্নোত্তর — {chapter}",
            "{chapter} অধ্যায় থেকে ৫টি সংক্ষিপ্ত প্রশ্নের উত্তর দাও। প্রতিটির মান ২।"),

        new(AssignmentType.Report, 10,
            "প্রতিবেদন — {chapter}",
            "{chapter} — শ্রেণিতে আলোচিত বিষয়ের উপর একটি সংক্ষিপ্ত প্রতিবেদন লেখো। "
            + "৩০০ শব্দের মধ্যে, প্রয়োজনে ছক বা চিত্র ব্যবহার করো।")
    ];

    // -------------------------------------------------- শারীরিক শিক্ষা ও স্বাস্থ্য

    private static readonly Template[] PhysicalEducation =
    [
        new(AssignmentType.MultipleChoice, 15,
            "বহুনির্বাচনি প্রশ্ন — {chapter}",
            "{chapter} অধ্যায় থেকে ১৫টি বহুনির্বাচনি প্রশ্নের উত্তর দাও। প্রতিটির মান ১।"),

        new(AssignmentType.ShortAnswer, 10,
            "সংক্ষিপ্ত প্রশ্নোত্তর — {chapter}",
            "{chapter} অধ্যায় থেকে ৫টি সংক্ষিপ্ত প্রশ্নের উত্তর দাও। প্রতিটির মান ২।"),

        new(AssignmentType.ClassWork, 10,
            "শ্রেণির কাজ — {chapter}",
            "{chapter} — সাত দিনের একটি ব্যক্তিগত শরীরচর্চার রুটিন তৈরি করো এবং প্রতিদিন "
            + "কী কী করেছ তা ছকে লিখে জমা দাও। ছকে থাকবে: দিন, ব্যায়ামের নাম, সময় এবং মন্তব্য।"),

        new(AssignmentType.Project, 10,
            "প্রজেক্ট — স্বাস্থ্যবিধি পোস্টার",
            "শ্রেণিকক্ষে টাঙানোর জন্য স্বাস্থ্যবিধি সম্পর্কে একটি পোস্টার তৈরি করো। "
            + "কমপক্ষে পাঁচটি নিয়ম ছবি ও লেখার মাধ্যমে উপস্থাপন করবে। পোস্টারের ছবি আপলোড করো।")
    ];

    // ---------------------------------------------------- কর্ম ও জীবনমুখী শিক্ষা

    private static readonly Template[] WorkAndLife =
    [
        new(AssignmentType.MultipleChoice, 15,
            "বহুনির্বাচনি প্রশ্ন — {chapter}",
            "{chapter} অধ্যায় থেকে ১৫টি বহুনির্বাচনি প্রশ্নের উত্তর দাও। প্রতিটির মান ১।"),

        new(AssignmentType.Project, 10,
            "প্রজেক্ট — {chapter}",
            "{chapter} — নিজের পরিবারে বা এলাকায় দেখা একটি পেশা বেছে নিয়ে তার উপর "
            + "একটি সংক্ষিপ্ত প্রতিবেদন তৈরি করো: পেশার নাম, কাজের ধরন, প্রয়োজনীয় দক্ষতা "
            + "এবং সমাজে এর গুরুত্ব।"),

        new(AssignmentType.ClassWork, 10,
            "শ্রেণির কাজ — {chapter}",
            "{chapter} — শ্রেণিকক্ষে আলোচিত বিষয়ের উপর নিজের অভিজ্ঞতা লিখে জমা দাও। "
            + "কমপক্ষে ১৫০ শব্দ।"),

        new(AssignmentType.ShortAnswer, 10,
            "সংক্ষিপ্ত প্রশ্নোত্তর — {chapter}",
            "{chapter} অধ্যায় থেকে ৫টি সংক্ষিপ্ত প্রশ্নের উত্তর দাও। প্রতিটির মান ২।")
    ];

    // -------------------------------------------------------- চারু ও কারুকলা

    private static readonly Template[] Arts =
    [
        new(AssignmentType.Drawing, 10,
            "চিত্র অঙ্কন — {chapter}",
            "{chapter} — শ্রেণিতে আলোচিত পদ্ধতি অনুসরণ করে একটি ছবি আঁকো।\n\n"
            + "কাগজের আকার: এ-৪। রং পেন্সিল বা জলরং — যেকোনোটি ব্যবহার করতে পারো। "
            + "আঁকা শেষ হলে ছবিটির স্পষ্ট ছবি তুলে আপলোড করো।\n\n"
            + "মূল্যায়ন: বিষয়বস্তু — ৪, গঠন ও অনুপাত — ৩, রঙের ব্যবহার — ৩।"),

        new(AssignmentType.PracticalWork, 10,
            "শিল্পকর্ম তৈরি — {chapter}",
            "{chapter} — কাগজ, রঙিন কাগজ বা ফেলনা জিনিস দিয়ে একটি শিল্পকর্ম তৈরি করো। "
            + "তৈরি করা জিনিসটির ছবি এবং কী কী উপকরণ ব্যবহার করেছ তার তালিকা জমা দাও।"),

        new(AssignmentType.ShortAnswer, 10,
            "সংক্ষিপ্ত প্রশ্নোত্তর — {chapter}",
            "{chapter} অধ্যায় থেকে ৫টি সংক্ষিপ্ত প্রশ্নের উত্তর দাও। প্রতিটির মান ২।"),

        new(AssignmentType.ClassWork, 10,
            "শ্রেণির কাজ — নকশা অঙ্কন",
            "বাংলাদেশের লোকশিল্পে ব্যবহৃত একটি নকশা দেখে সেটির অনুরূপ নকশা আঁকো। "
            + "নকশাটি কোন শিল্প থেকে নেওয়া, তা নিচে লিখে দেবে।")
    ];
}
