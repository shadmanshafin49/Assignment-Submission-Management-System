using Ams.Domain.Enums;

namespace Ams.Infrastructure.Persistence.SeedData;

/// <summary>
/// The staff and student body of the seeded school.
/// <para>
/// Teacher names and their subjects come from the brief. Which class each of them takes is a
/// scheduling decision made here: subjects with two named teachers are split across the three
/// classes, so no course is left unstaffed and no teacher is timetabled beyond what a six-day,
/// 36-period week can hold.
/// </para>
/// </summary>
public static class People
{
    public record TeacherSeed(string Key, string Name, string NameEn, string Email, string Designation);

    /// <summary>A course offering: which teacher takes this subject for this class.</summary>
    public record CourseStaffing(int Level, string SubjectCode, string TeacherKey);

    public const string DefaultTeacherPassword = "Teacher@123";
    public const string DefaultStudentPassword = "Student@123";
    public const string DefaultAdminPassword = "Admin@123";

    public static readonly TeacherSeed[] Teachers =
    [
        new("afsar", "গাজী মোঃ আফসার উদ্দিন", "Gazi Md Afsar Uddin",
            "afsar.uddin@gcbhs.edu.bd", "সহকারী শিক্ষক (বাংলা)"),

        new("humayun", "হুমায়ূন আহমেদ", "Humayun Ahmed",
            "humayun.ahmed@gcbhs.edu.bd", "সহকারী শিক্ষক (বাংলা)"),

        new("shahanaz", "শাহানাজ পারভীন", "Shahanaz Parvin",
            "shahanaz.parvin@gcbhs.edu.bd", "সহকারী শিক্ষক (বাংলা)"),

        new("sultan", "মোঃ সুলতান মাহমুদ", "Md Sultan Mahmud",
            "sultan.mahmud@gcbhs.edu.bd", "সহকারী শিক্ষক (ইংরেজি)"),

        new("aziz", "মোঃ আজিজ আহমেদ", "Md Aziz Ahmed",
            "aziz.ahmed@gcbhs.edu.bd", "সহকারী শিক্ষক (ইংরেজি)"),

        new("rejaul", "মোঃ রেজাউল করিম", "Md Rejaul Karim",
            "rejaul.karim@gcbhs.edu.bd", "সহকারী শিক্ষক (গণিত)"),

        new("ashraf", "আশরাফ আলী আহমেদ", "Ashraf Ali Ahmed",
            "ashraf.ali@gcbhs.edu.bd", "সহকারী শিক্ষক (গণিত ও কৃষি)"),

        new("satter", "মোঃ আব্দুস সাত্তার", "Md Abdus Satter",
            "abdus.satter@gcbhs.edu.bd", "সহকারী শিক্ষক (শারীরিক শিক্ষা)"),

        new("sirajul", "মোঃ সিরাজুল ইসলাম মোল্লা", "Md Sirajul Islam Molla",
            "sirajul.islam@gcbhs.edu.bd", "সহকারী শিক্ষক (আইসিটি)"),

        new("mukim", "মোঃ মুকিম বিল্লাহ", "Md Mukim Billah",
            "mukim.billah@gcbhs.edu.bd", "সহকারী শিক্ষক (ইসলাম শিক্ষা)"),

        new("purnima", "পূর্ণিমা সরকার", "Purnima Sarker",
            "purnima.sarker@gcbhs.edu.bd", "সহকারী শিক্ষক (হিন্দুধর্ম ও বিজ্ঞান)"),

        new("rafia", "রাফিয়া হাসিন", "Rafia Hasin",
            "rafia.hasin@gcbhs.edu.bd", "সহকারী শিক্ষক (চারু ও কারুকলা)"),

        new("selina", "সেলিনা আক্তার", "Selina Akter",
            "selina.akter@gcbhs.edu.bd", "সহকারী শিক্ষক (বিজ্ঞান)")
    ];

    /// <summary>
    /// All 42 course offerings — 14 subjects × 3 classes — and who takes each.
    /// <para>
    /// The resulting weekly loads are 2–20 periods, with মোঃ রেজাউল করিম the heaviest at 20 of
    /// 36 (maths for two classes, বাংলাদেশ ও বিশ্বপরিচয় for all three, and চারু ও কারুকলা for
    /// class 8). The routine builder verifies that nobody is asked to be in two rooms at once.
    /// </para>
    /// </summary>
    public static readonly CourseStaffing[] Staffing =
    [
        // বাংলা ১ম পত্র — two teachers over three classes
        new(6, Curriculum.Bangla1, "afsar"),
        new(7, Curriculum.Bangla1, "afsar"),
        new(8, Curriculum.Bangla1, "humayun"),

        // বাংলা ২য় পত্র — one teacher
        new(6, Curriculum.Bangla2, "shahanaz"),
        new(7, Curriculum.Bangla2, "shahanaz"),
        new(8, Curriculum.Bangla2, "shahanaz"),

        // ইংরেজি ১ম পত্র
        new(6, Curriculum.English1, "sultan"),
        new(7, Curriculum.English1, "sultan"),
        new(8, Curriculum.English1, "aziz"),

        // ইংরেজি ২য় পত্র
        new(6, Curriculum.English2, "aziz"),
        new(7, Curriculum.English2, "aziz"),
        new(8, Curriculum.English2, "aziz"),

        // গণিত
        new(6, Curriculum.Math, "rejaul"),
        new(7, Curriculum.Math, "rejaul"),
        new(8, Curriculum.Math, "ashraf"),

        // বিজ্ঞান
        new(6, Curriculum.Science, "selina"),
        new(7, Curriculum.Science, "selina"),
        new(8, Curriculum.Science, "purnima"),

        // বাংলাদেশ ও বিশ্বপরিচয়
        new(6, Curriculum.Bgs, "rejaul"),
        new(7, Curriculum.Bgs, "rejaul"),
        new(8, Curriculum.Bgs, "rejaul"),

        // তথ্য ও যোগাযোগ প্রযুক্তি
        new(6, Curriculum.Ict, "sirajul"),
        new(7, Curriculum.Ict, "sirajul"),
        new(8, Curriculum.Ict, "sirajul"),

        // ধর্ম ও নৈতিক শিক্ষা — the two streams run in parallel
        new(6, Curriculum.Islam, "mukim"),
        new(7, Curriculum.Islam, "mukim"),
        new(8, Curriculum.Islam, "mukim"),
        new(6, Curriculum.Hindu, "purnima"),
        new(7, Curriculum.Hindu, "purnima"),
        new(8, Curriculum.Hindu, "purnima"),

        // কৃষিশিক্ষা
        new(6, Curriculum.Agriculture, "ashraf"),
        new(7, Curriculum.Agriculture, "ashraf"),
        new(8, Curriculum.Agriculture, "ashraf"),

        // শারীরিক শিক্ষা ও স্বাস্থ্য
        new(6, Curriculum.PhysicalEducation, "satter"),
        new(7, Curriculum.PhysicalEducation, "satter"),
        new(8, Curriculum.PhysicalEducation, "satter"),

        // কর্ম ও জীবনমুখী শিক্ষা
        new(6, Curriculum.WorkAndLife, "purnima"),
        new(7, Curriculum.WorkAndLife, "shahanaz"),
        new(8, Curriculum.WorkAndLife, "shahanaz"),

        // চারু ও কারুকলা
        new(6, Curriculum.Arts, "rafia"),
        new(7, Curriculum.Arts, "rafia"),
        new(8, Curriculum.Arts, "rejaul")
    ];

    /// <summary>
    /// Ninety students, thirty per class, all male — Gazipur Cantonment Board High School has
    /// been a boys' school since its girls' section separated in 2008.
    /// <para>
    /// Roll numbers are assigned in list order, which is how a Bangladeshi class roster works.
    /// The last three of each class are Hindu, so every class has both religion streams and the
    /// parallel ধর্ম period has students on both sides of it.
    /// </para>
    /// </summary>
    public static readonly (string Name, string NameEn, FaithGroup Faith)[] StudentsClass6 =
    [
        ("মোঃ সাদমান সাকিব", "Md Sadman Sakib", FaithGroup.Islam),
        ("আরিফুল ইসলাম", "Ariful Islam", FaithGroup.Islam),
        ("মোঃ তানভীর হাসান", "Md Tanvir Hasan", FaithGroup.Islam),
        ("রাফিদ হোসেন", "Rafid Hossain", FaithGroup.Islam),
        ("মোঃ নাঈম উদ্দিন", "Md Naim Uddin", FaithGroup.Islam),
        ("সাকিব আল হাসান", "Sakib Al Hasan", FaithGroup.Islam),
        ("মোঃ রায়হান কবির", "Md Raihan Kabir", FaithGroup.Islam),
        ("ফাহিম মুনতাসির", "Fahim Muntasir", FaithGroup.Islam),
        ("মোঃ ইমরান হোসেন", "Md Imran Hossain", FaithGroup.Islam),
        ("তাহসিন আহমেদ", "Tahsin Ahmed", FaithGroup.Islam),
        ("মোঃ শাহরিয়ার রহমান", "Md Shahriar Rahman", FaithGroup.Islam),
        ("আব্দুল্লাহ আল মামুন", "Abdullah Al Mamun", FaithGroup.Islam),
        ("মোঃ জুবায়ের আলম", "Md Zubair Alam", FaithGroup.Islam),
        ("নাফিস ইকবাল", "Nafis Iqbal", FaithGroup.Islam),
        ("মোঃ আসিফ মাহমুদ", "Md Asif Mahmud", FaithGroup.Islam),
        ("রেদোয়ান হোসেন", "Redwan Hossain", FaithGroup.Islam),
        ("মোঃ সিয়াম হাওলাদার", "Md Siam Howlader", FaithGroup.Islam),
        ("তামিম ইকবাল", "Tamim Iqbal", FaithGroup.Islam),
        ("মোঃ রাকিবুল হাসান", "Md Rakibul Hasan", FaithGroup.Islam),
        ("সাইফুল ইসলাম", "Saiful Islam", FaithGroup.Islam),
        ("মোঃ আরাফাত রহমান", "Md Arafat Rahman", FaithGroup.Islam),
        ("জিসান মাহমুদ", "Zisan Mahmud", FaithGroup.Islam),
        ("মোঃ ফয়সাল আহমেদ", "Md Faisal Ahmed", FaithGroup.Islam),
        ("হাসিবুল হাসান", "Hasibul Hasan", FaithGroup.Islam),
        ("মোঃ নাহিদ হোসেন", "Md Nahid Hossain", FaithGroup.Islam),
        ("মাহির তাজওয়ার", "Mahir Tajwar", FaithGroup.Islam),
        ("মোঃ শাকিল আহমেদ", "Md Shakil Ahmed", FaithGroup.Islam),
        ("অর্ণব চক্রবর্তী", "Arnab Chakraborty", FaithGroup.Hindu),
        ("সৌরভ দাস", "Sourav Das", FaithGroup.Hindu),
        ("রাহুল সরকার", "Rahul Sarker", FaithGroup.Hindu)
    ];

    public static readonly (string Name, string NameEn, FaithGroup Faith)[] StudentsClass7 =
    [
        ("মোঃ ইশতিয়াক আহমেদ", "Md Ishtiaq Ahmed", FaithGroup.Islam),
        ("সাদ বিন সিদ্দিক", "Saad Bin Siddique", FaithGroup.Islam),
        ("মোঃ তাওহীদ ইসলাম", "Md Towhid Islam", FaithGroup.Islam),
        ("আরাফ হোসেন", "Araf Hossain", FaithGroup.Islam),
        ("মোঃ রিফাত হাসান", "Md Rifat Hasan", FaithGroup.Islam),
        ("আদনান সামি", "Adnan Sami", FaithGroup.Islam),
        ("মোঃ সাজিদ হোসেন", "Md Sajid Hossain", FaithGroup.Islam),
        ("ইয়াসিন আরাফাত", "Yeasin Arafat", FaithGroup.Islam),
        ("মোঃ সাব্বির আহমেদ", "Md Sabbir Ahmed", FaithGroup.Islam),
        ("নাজমুস সাকিব", "Nazmus Sakib", FaithGroup.Islam),
        ("মোঃ আশিকুর রহমান", "Md Ashiqur Rahman", FaithGroup.Islam),
        ("তানজিদ হাসান", "Tanzid Hasan", FaithGroup.Islam),
        ("মোঃ শাহাদাত হোসেন", "Md Shahadat Hossain", FaithGroup.Islam),
        ("রুবেল মিয়া", "Rubel Mia", FaithGroup.Islam),
        ("মোঃ জাহিদ হাসান", "Md Zahid Hasan", FaithGroup.Islam),
        ("সিফাত উল্লাহ", "Sifat Ullah", FaithGroup.Islam),
        ("মোঃ মেহেদী হাসান", "Md Mehedi Hasan", FaithGroup.Islam),
        ("আয়ান রহমান", "Ayan Rahman", FaithGroup.Islam),
        ("মোঃ নাফিউল ইসলাম", "Md Nafiul Islam", FaithGroup.Islam),
        ("তৌসিফ মাহবুব", "Tousif Mahbub", FaithGroup.Islam),
        ("মোঃ সাদিক ইসলাম", "Md Sadik Islam", FaithGroup.Islam),
        ("রায়হান উদ্দিন", "Raihan Uddin", FaithGroup.Islam),
        ("মোঃ আবির হোসেন", "Md Abir Hossain", FaithGroup.Islam),
        ("শাহরিয়ার নাফিস", "Shahriar Nafees", FaithGroup.Islam),
        ("মোঃ তাহমিদ রহমান", "Md Tahmid Rahman", FaithGroup.Islam),
        ("জুনায়েদ আহমেদ", "Junaid Ahmed", FaithGroup.Islam),
        ("মোঃ আসিফ ইকবাল", "Md Asif Iqbal", FaithGroup.Islam),
        ("দীপ্ত বিশ্বাস", "Dipto Biswas", FaithGroup.Hindu),
        ("প্রীতম কুমার ঘোষ", "Pritom Kumar Ghosh", FaithGroup.Hindu),
        ("শুভ মজুমদার", "Shuvo Majumder", FaithGroup.Hindu)
    ];

    public static readonly (string Name, string NameEn, FaithGroup Faith)[] StudentsClass8 =
    [
        ("মোঃ আবরার ফাহাদ", "Md Abrar Fahad", FaithGroup.Islam),
        ("তানজিম হাসান সাকিব", "Tanzim Hasan Sakib", FaithGroup.Islam),
        ("মোঃ ইফতেখার আলম", "Md Iftekhar Alam", FaithGroup.Islam),
        ("সাজ্জাদুল করিম", "Sajjadul Karim", FaithGroup.Islam),
        ("মোঃ রাফসান জানি", "Md Rafsan Jani", FaithGroup.Islam),
        ("নাবিল আহসান", "Nabil Ahsan", FaithGroup.Islam),
        ("মোঃ তাসনিম হাসান", "Md Tasnim Hasan", FaithGroup.Islam),
        ("ইমতিয়াজ উদ্দিন", "Imtiaz Uddin", FaithGroup.Islam),
        ("মোঃ সালমান ফারসি", "Md Salman Farsi", FaithGroup.Islam),
        ("আরিয়ান খান", "Arian Khan", FaithGroup.Islam),
        ("মোঃ জাকারিয়া হোসেন", "Md Zakaria Hossain", FaithGroup.Islam),
        ("সাদমান সাদিক", "Sadman Sadik", FaithGroup.Islam),
        ("মোঃ হাসান মাহমুদ", "Md Hasan Mahmud", FaithGroup.Islam),
        ("রাফসান আহমেদ", "Rafsan Ahmed", FaithGroup.Islam),
        ("মোঃ তানজিল হক", "Md Tanzil Haque", FaithGroup.Islam),
        ("ফারহান ইসলাম", "Farhan Islam", FaithGroup.Islam),
        ("মোঃ ইমতিয়াজ আহমেদ", "Md Imtiaz Ahmed", FaithGroup.Islam),
        ("নাজিব ওয়াদুদ", "Najib Wadud", FaithGroup.Islam),
        ("মোঃ সাইম হোসেন", "Md Saim Hossain", FaithGroup.Islam),
        ("তানভীর মাহতাব", "Tanvir Mahtab", FaithGroup.Islam),
        ("মোঃ রাশেদুল ইসলাম", "Md Rashedul Islam", FaithGroup.Islam),
        ("আবির হাসান", "Abir Hasan", FaithGroup.Islam),
        ("মোঃ শাফিন আহমেদ", "Md Shafin Ahmed", FaithGroup.Islam),
        ("যুবায়ের হোসেন", "Zubayer Hossain", FaithGroup.Islam),
        ("মোঃ নাঈমুর রহমান", "Md Naimur Rahman", FaithGroup.Islam),
        ("সাদাত হোসেন", "Sadat Hossain", FaithGroup.Islam),
        ("মোঃ আরমান হোসেন", "Md Arman Hossain", FaithGroup.Islam),
        ("নিলয় সাহা", "Niloy Saha", FaithGroup.Hindu),
        ("অভিজিৎ রায়", "Abhijit Roy", FaithGroup.Hindu),
        ("তন্ময় পাল", "Tanmoy Pal", FaithGroup.Hindu)
    ];

    public static (string Name, string NameEn, FaithGroup Faith)[] StudentsFor(int level) => level switch
    {
        6 => StudentsClass6,
        7 => StudentsClass7,
        8 => StudentsClass8,
        _ => []
    };
}
