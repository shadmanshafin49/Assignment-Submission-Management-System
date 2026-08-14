namespace Ams.Domain.Enums;

/// <summary>
/// Religion stream. NCTB item 9 for classes 6–8 is "ইসলাম শিক্ষা / হিন্দুধর্ম শিক্ষা /
/// খ্রীষ্টধর্ম শিক্ষা / বৌদ্ধধর্ম শিক্ষা (যেকোনো একটি)" — every student takes exactly one.
/// <para>
/// A student's value decides which religion course appears in their timetable and assignment
/// feed; a subject's value marks it as belonging to that stream. Compulsory subjects carry
/// <c>null</c> and are taken by everybody.
/// </para>
/// </summary>
public enum FaithGroup
{
    Islam = 1,
    Hindu = 2,
    Christian = 3,
    Buddhist = 4
}
