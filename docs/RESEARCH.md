# Curriculum research notes

Everything the seeded data claims about Gazipur Cantonment Board High School, the NCTB
curriculum and the weekly routine comes from the sources below. This file is the audit trail:
if a chapter name, a subject code or a period count looks odd, this is where it came from.

Research done **13 August 2026**.

---

## 1. The school

| Fact | Value | Source |
|---|---|---|
| Name | Gazipur Cantonment Board High School / গাজীপুর ক্যান্টনমেন্ট বোর্ড উচ্চ বিদ্যালয় | <https://www.gcbhs.edu.bd/> |
| EIIN | 108957 | school site + Dhaka Education Board portal |
| School code | 2075 | school site |
| Board | Dhaka | <https://deb108957.dhakaeducationboard.gov.bd/> |
| Founded | 1968 as a primary school inside the Bangladesh Ordnance Factories complex; became a high school in 1971; transferred to Gazipur Cantonment Board in 1972 | school site history page |
| Type | Boys' school — the girls' section separated into Gazipur Cantonment Board Girls' High School (EIIN 130711) in 2008 | school site + <https://www.gcbghs.edu.bd/> |
| Size | ~1,100 students, ~28 teachers | school site |
| Medium | Bangla version | school site |

The app is seeded as a **Bangla-version boys' school**, which is why every seeded student is
male and every UI string is Bangla.

---

## 2. Which curriculum is actually in force

This mattered more than expected. Bangladesh introduced a new competency-based curriculum in
2023, then **reverted to the 2012 National Curriculum** for secondary classes. The reversion is
not just news reporting — it is printed inside the books themselves:

> "২০২৪ সালের পরিবর্তিত পরিস্থিতিতে প্রয়োজনের নিরিখে পাঠ্যপুস্তকসমূহ পরিমার্জন করা হয়েছে। এক্ষেত্রে **২০১২ সালের
> শিক্ষাক্রম অনুযায়ী প্রণীত পাঠ্যপুস্তকের সর্বশেষ সংস্করণকে ভিত্তি হিসেবে গ্রহণ করা হয়েছে**।"
>
> — প্রসঙ্গকথা, গণিত ষষ্ঠ শ্রেণি, অক্টোবর ২০২৫, প্রফেসর রবিউল কবীর চৌধুরী, চেয়ারম্যান (অতিরিক্ত দায়িত্ব), NCTB

So the subject list in `enhancement.md` (কৃষিশিক্ষা, চারু ও কারুকলা, কর্ম ও জীবনমুখী শিক্ষা,
বাংলাদেশ ও বিশ্বপরিচয়) is the **correct, current** list for the 2026 school year, not a stale one.
Book titles below were read off the NCTB 2026 book lists:

- Class 6 — <https://nctb.gov.bd/pages/static-pages/695b987ac4774958d7b7040b>
- Class 7 — <https://nctb.gov.bd/pages/static-pages/695b9aeec4774958d7b70908>
- Class 8 — <https://nctb.gov.bd/pages/static-pages/695b9858c4774958d7b703d8>

---

## 3. Subject codes

Board subject codes come from the published subject/code table for classes 6–10
(<https://smhsmp.edu.bd/literacy/৬ষ্ঠ-১০ম-শ্রেণীর-জন্য-পঠি/>), cross-checked against the NCTB
subject structure document in §4.

| Code | Subject | Weekly periods (NCTB) |
|---|---|---|
| 101 | বাংলা ১ম পত্র | 3 |
| 102 | বাংলা ২য় পত্র | 2 |
| 107 | ইংরেজি ১ম পত্র | 4 |
| 108 | ইংরেজি ২য় পত্র | 2 |
| 109 | গণিত | 5 |
| 127 | বিজ্ঞান | 5 |
| 150 | বাংলাদেশ ও বিশ্বপরিচয় | 3 |
| 154 | তথ্য ও যোগাযোগ প্রযুক্তি | 2 |
| 111 | ইসলাম ও নৈতিক শিক্ষা | 3 (faith group) |
| 112 | হিন্দুধর্ম ও নৈতিক শিক্ষা | 3 (faith group) |
| 134 | কৃষিশিক্ষা | optional group |
| 147 | শারীরিক শিক্ষা, স্বাস্থ্যবিজ্ঞান ও খেলাধুলা | optional group |
| 148 | চারু ও কারুকলা | optional group |
| 155 | কর্ম ও জীবনমুখী শিক্ষা | optional group |

Course codes in the app follow the pattern from `enhancement.md`: `C06-101` = class 6,
Bangla 1st paper. `C` + two-digit class + `-` + board subject code.

---

## 4. Weekly period allocation — the primary source

**জাতীয় শিক্ষাক্রম ও পাঠ্যপুস্তক বোর্ড — "বিষয় কাঠামো, নম্বর, সময় বণ্টন এবং মূল্যায়ন পদ্ধতি,
সাধারণ শিক্ষা ধারার ষষ্ঠ থেকে অষ্টম শ্রেণির জন্য (জাতীয় শিক্ষাক্রম ২০১২ এর ভিত্তিতে ২০২৫
শিক্ষাবর্ষ থেকে কার্যকর)"**, signed 09-02-2025.

Downloaded from <https://drive.egovcloud.gov.bd/index.php/s/sTWwtPxA87onTxu>, linked from
<https://nctb.gov.bd/pages/static-pages/6922df50933eb65569e21292>.

It specifies, for classes 6–8:

- 9 compulsory subjects totalling **29 periods** (table in §3), plus **1 period** for one
  optional-group subject → **30 periods/week**, 800 marks.
- One-shift schools: Sunday–Thursday, **6 periods per day**, first period 60 minutes and the
  rest **50 minutes**, 15-minute assembly, 35-minute tiffin break, 6 hours total.
- Two-shift schools: 6 periods per day, first period 45 minutes and the rest 40 minutes.

### How the app's 36-period week is derived

`enhancement.md` specifies a **six-day week, six 50-minute periods a day = 36 periods**. That
differs from the NCTB document in two ways, both deliberate and both documented in the README:

1. **Six teaching days (Saturday–Thursday) instead of five.** This is the pre-2022 national norm
   and is still common in cantonment-board schools. Friday is the weekly holiday.
2. **Four optional-group subjects taught instead of one.** NCTB item 10 is "any one of
   আরবি/সংস্কৃত/পালি/শারীরিক শিক্ষা ও স্বাস্থ্য/কর্ম ও জীবনমুখী শিক্ষা/কৃষিশিক্ষা/গার্হস্থ্যবিজ্ঞান/চারু ও
   কারুকলা/সংগীত". The school in `enhancement.md` teaches four of them.

A third departure — where the tiffin break falls, and how long it runs — is described under
**Bell times** below. It changes the shape of the day, not the 36 periods in it.

The 36 periods therefore land as:

| Subject | Periods | Basis |
|---|---|---|
| বাংলা ১ম পত্র | 3 | NCTB |
| বাংলা ২য় পত্র | 2 | NCTB |
| ইংরেজি ১ম পত্র | 4 | NCTB |
| ইংরেজি ২য় পত্র | 2 | NCTB |
| গণিত | 5 | NCTB |
| বিজ্ঞান | 5 | NCTB |
| বাংলাদেশ ও বিশ্বপরিচয় | 3 | NCTB |
| তথ্য ও যোগাযোগ প্রযুক্তি | 2 | NCTB |
| ধর্ম ও নৈতিক শিক্ষা | 3 | NCTB |
| **subtotal (compulsory)** | **29** | |
| কৃষিশিক্ষা | 2 | school allocation of the 7 remaining periods |
| শারীরিক শিক্ষা ও স্বাস্থ্য | 2 | ” |
| কর্ম ও জীবনমুখী শিক্ষা | 2 | ” |
| চারু ও কারুকলা | 1 | ” |
| **total** | **36** | |

Every compulsory subject keeps its official weekly count. Only the optional group is expanded.

### Bell times

Derived from the NCTB one-shift rule (assembly 15 min, first period 60 min, later periods
50 min), from a clean 07:45 start to a 14:00 close:

| | |
|---|---|
| সমাবেশ | 07:45–08:00 |
| ১ম পিরিয়ড | 08:00–09:00 (60 min, roll call) |
| ২য় পিরিয়ড | 09:00–09:50 |
| ৩য় পিরিয়ড | 09:50–10:40 |
| **টিফিন** | **10:40–11:30 (50 min)** |
| ৪র্থ পিরিয়ড | 11:30–12:20 |
| ৫ম পিরিয়ড | 12:20–13:10 |
| ৬ষ্ঠ পিরিয়ড | 13:10–14:00 |

**Third deviation from the NCTB sheet: the break is after the third period, not the fourth.**
The sheet allots 35 minutes and places the break so that four periods precede it; this school
splits the day in half — three periods, tiffin, three periods — and closes at 14:00. Six periods
of the mandated length between 08:00 and 14:00 leave 50 minutes in the middle, so the break is
15 minutes longer than the sheet's rather than shorter. Nothing else moves: the period lengths,
the count, and the assembly are all still NCTB's.

The break is a gap in the day, not a period. `PeriodSchedule` therefore numbers only the six
teaching slots, and the routine table draws tiffin as a column with no cells in it — the earlier
version labelled the fourth period "টিফিন" and left classes rendered underneath, which stated the
opposite of what the schedule meant.

---

## 5. Assignment types — the second primary source

**"ষষ্ঠ থেকে অষ্টম শ্রেণির বিষয়ভিত্তিক প্রশ্নের ধরন, মূল্যায়ন নির্দেশনা ও নম্বর বিভাজন"**, same
board, same date. <https://drive.egovcloud.gov.bd/index.php/s/6gliHfrMN7doHHa>

This is what makes the assignment types in this app real rather than invented. It also
explicitly names **অ্যাসাইনমেন্ট** as a graded component, which is the whole premise of the app:

> ধারাবাহিক মূল্যায়নের ক্ষেত্র ও নম্বর বণ্টন — শ্রেণির কাজ ২০; **অনুসন্ধানমূলক কাজ/ব্যবহারিক
> কাজ/প্রজেক্ট/অ্যাসাইনমেন্ট ১০**; শ্রেণি অভীক্ষা ২০; মোট ৫০

### Per-subject question types (verbatim from the document)

**বাংলা ১ম পত্র — 100**
- সৃজনশীল প্রশ্ন 50: গদ্য থেকে ৪টি, কবিতা থেকে ৪টি = ৮টি; ৫টির উত্তর (গদ্য ও কবিতা থেকে কমপক্ষে ২টি করে); প্রতিটি ১০
- বর্ণনামূলক প্রশ্ন 20: আনন্দপাঠ থেকে ৪টি, ২টির উত্তর; প্রতিটি ১০ (ক অংশ ৩ + খ অংশ ৭)
- বহুনির্বাচনি 30: গদ্য ১৫ + কবিতা ১৫; সব উত্তর দিতে হবে; প্রতিটি ১

**বাংলা ২য় পত্র — 50**
- নির্মিতি 35: অনুধাবন দক্ষতা/অনুচ্ছেদ রচনা ৫ · পত্র রচনা ৫ · সারাংশ/সারমর্ম ৫ · ভাবসম্প্রসারণ ৫ · প্রবন্ধ রচনা ১৫
- বহুনির্বাচনি (ব্যাকরণ অংশ) 15: ১৫টি প্রশ্ন, প্রতিটি ১

**ইংরেজি ১ম পত্র — 100**
- Part-A Reading 70: MCQ (seen-1) 7 · Answering questions (seen-1) 10 · Gap filling (seen-2) 5 ·
  Vocabulary synonyms & antonyms (seen-2) 5 · Information transfer (unseen) 5 · True/False (unseen) 5 ·
  Writing summary 10 · Matching 5 · Re-arranging sentences 8 · Answering questions from poems in EFT (any 5 of 8) 10
- Part-B Writing 30: Completing stories 10 · Writing paragraph (120 words class 6, 150 words classes 7–8) 10 · Writing dialogues 10

**ইংরেজি ২য় পত্র — 50**
- Part-A Grammar 30: parts of speech / gap filling with clues / substitution table / right form of verbs /
  changing sentences / punctuation & capitalisation. Class 7 adds Narration; class 8 adds Passage Narration,
  Voice and Degrees of Comparison.
- Part-B Writing 20: Letter/E-mail (formal & informal) 8 · Short composition 12 (200 words class 6, 250 words classes 7–8)

**গণিত — 100**
- সৃজনশীল 50: ক পাটিগণিত ২টি, খ বীজগণিত ২টি, গ জ্যামিতি ২টি, ঘ তথ্য ও উপাত্ত ২টি = ৮টি; প্রত্যেক বিভাগ থেকে ন্যূনতম ১টি করে ৫টির উত্তর; প্রতিটি ১০
- সংক্ষিপ্ত-উত্তর 20: ১৫টি প্রশ্ন, ১০টির উত্তর, প্রতিটি ২
- বহুনির্বাচনি 30: পাটিগণিত ৮–১০, বীজগণিত ৮–১০, জ্যামিতি ৬–৮, তথ্য ও উপাত্ত ৩–৪

**বিজ্ঞান / বাংলাদেশ ও বিশ্বপরিচয় / ধর্ম ও নৈতিক শিক্ষা — 100 each**
- সৃজনশীল 50 (৮টি, ৫টির উত্তর) · সংক্ষিপ্ত-উত্তর 20 (১৫টি, ১০টির উত্তর) · বহুনির্বাচনি 30
- বিজ্ঞান adds: প্রতিটি অধ্যায় থেকে ন্যূনতম ১টি সংক্ষিপ্ত ও ২টি বহুনির্বাচনি প্রশ্ন

**তথ্য ও যোগাযোগ প্রযুক্তি — 50**
- তত্ত্বীয় 25: ১৫টি বহুনির্বাচনি (সব উত্তর) + ৮টি সংক্ষিপ্ত উত্তর প্রশ্ন থেকে ৫টি
- ব্যবহারিক 25: যন্ত্র/উপকরণ সংযোজন ও ব্যবহার, উপাত্ত সংগ্রহ ও প্রক্রিয়াকরণ, অঙ্কন, পর্যবেক্ষণ, শনাক্তকরণ, অনুশীলন ১৫ · প্রতিবেদন প্রণয়ন ৫ · মৌখিক অভীক্ষা ৫

**কৃষিশিক্ষা / শারীরিক শিক্ষা / চারু ও কারুকলা / কর্ম ও জীবনমুখী শিক্ষা — 50 each, ধারাবাহিক মূল্যায়ন**
- শ্রেণির কাজ 20 — প্রশ্নের উত্তর লেখা, মৌখিক উপস্থাপনা, ছবি/চিত্র/সারণি/মানচিত্র/লেখচিত্র আঁকা, দলগত ও জোড়ায় কাজ, বিতর্ক, ভূমিকাভিনয়, ব্যবহারিক কাজ
- অনুসন্ধানমূলক কাজ/ব্যবহারিক কাজ/প্রজেক্ট/অ্যাসাইনমেন্ট 10 — হাতে-কলমে কাজ, মডেল তৈরি, স্বল্প পরিসরে অনুসন্ধান, প্রতিবেদন প্রণয়ন ও উপস্থাপন
- শ্রেণি অভীক্ষা 20

The app's `AssignmentType` enum is this list, collapsed to the types a teacher would actually
set as homework.

---

## 6. Textbook chapter lists

Read directly from the সূচিপত্র page of each 2026 NCTB PDF (42 books downloaded from the class
6/7/8 book pages listed in §2). The books render their Bangla as vector outlines, so the text
layer is unusable — the tables of contents were located by detecting the ruled table on each
page and read from the rendered page image.

### বাংলা ১ম পত্র

**Class 6 — চারুপাঠ**
গদ্য: সততার পুরস্কার (মুহম্মদ শহীদুল্লাহ) · মিনু (বনফুল) · নীল নদ আর পিরামিডের দেশ (সৈয়দ মুজতবা আলী) ·
তোলপাড় (শওকত ওসমান) · আকাশ (আবদুল্লাহ আল-মুতী) · মাদার তেরেসা (সন্‌জীদা খাতুন) · আমাদের লোকশিল্প
(কামরুল হাসান) · কত কাল ধরে (আনিসুজ্জামান) · কার্টুন, ব্যঙ্গচিত্র ও পোস্টারের ভাষা (সংকলিত)
কবিতা: জন্মভূমি (রবীন্দ্রনাথ ঠাকুর) · সুখ (কামিনী রায়) · মানুষ জাতি (সত্যেন্দ্রনাথ দত্ত) · ঝিঙে ফুল
(কাজী নজরুল ইসলাম) · আসমানি (জসীমউদ্‌দীন) · চিঠি বিলি (রোকনুজ্জামান খান) · বাঁচতে দাও (শামসুর রাহমান) ·
পাখির কাছে ফুলের কাছে (আল মাহমুদ) · ফাগুন মাস (হুমায়ুন আজাদ)

**Class 7 — সপ্তবর্ণা**
গদ্য: কাবুলিওয়ালা (রবীন্দ্রনাথ ঠাকুর) · লখার একুশে (আবুবকর সিদ্দিক) · মরু-ভাস্কর (হবীবুল্লাহ বাহার) ·
শব্দ থেকে কবিতা (হুমায়ুন আজাদ) · পাখি (লীলা মজুমদার) · পিতৃপুরুষের গল্প (হারুন হাবীব) · ছবির রং
(হাশেম খান) · সেই ছেলেটি (মামুনুর রশীদ) · বহু জাতিসত্তার দেশ–বাংলাদেশ (এ. কে. শেরাম)
কবিতা: নতুন দেশ (রবীন্দ্রনাথ ঠাকুর) · কুলি-মজুর (কাজী নজরুল ইসলাম) · আমার বাড়ি (জসীমউদ্‌দীন) ·
শ্রাবণে (সুকুমার রায়) · গরবিনী মা-জননী (সিকান্দার আবু জাফর) · সাম্য (সুফিয়া কামাল) · মেলা
(আহসান হাবীব) · এই অক্ষরে (মহাদেব সাহা) · সিঁথি (হাসান রোবায়েত)

**Class 8 — সাহিত্য-কণিকা**
গদ্য: অতিথির স্মৃতি (শরৎচন্দ্র চট্টোপাধ্যায়) · পড়ে পাওয়া (বিভূতিভূষণ বন্দ্যোপাধ্যায়) · ভাব ও কাজ
(কাজী নজরুল ইসলাম) · লাইব্রেরি (মোতাহের হোসেন চৌধুরী) · তৈলচিত্রের ভূত (মানিক বন্দ্যোপাধ্যায়) ·
সুখী মানুষ (মমতাজউদদীন আহমদ) · শিল্পকলার নানা দিক (মুস্তাফা মনোয়ার) · মংডুর পথে (বিপ্রদাশ বড়ুয়া) ·
বাংলা নববর্ষ (শামসুজ্জামান খান) · বাংলা ভাষার জন্মকথা (হুমায়ুন আজাদ) · গণঅভ্যুত্থানের কথা (সংকলিত)
কবিতা: মানবধর্ম (লালন শাহ্) · বঙ্গভূমির প্রতি (মাইকেল মধুসূদন দত্ত) · প্রার্থনা (কায়কোবাদ) ·
দুই বিঘা জমি (রবীন্দ্রনাথ ঠাকুর) · পাছে লোকে কিছু বলে (কামিনী রায়) · বাবুরের মহত্ত্ব (কালিদাস রায়) ·
নারী (কাজী নজরুল ইসলাম) · আবার আসিব ফিরে (জীবনানন্দ দাশ) · রূপাই (জসীমউদ্‌দীন) · নদীর স্বপ্ন
(বুদ্ধদেব বসু) · জাগো তবে অরণ্য কন্যারা (সুফিয়া কামাল) · প্রার্থী (সুকান্ত ভট্টাচার্য) · একুশের গান
(আবদুল গাফ্‌ফার চৌধুরী)

### বাংলা ২য় পত্র — বাংলা ব্যাকরণ ও নির্মিতি

**Class 6** — ক. ব্যাকরণ: ভাষা ও বাংলা ভাষা · ধ্বনিতত্ত্ব · রূপতত্ত্ব · বাক্যতত্ত্ব · বাগর্থ · বানান ·
বিরামচিহ্ন · অভিধান। খ. নির্মিতি: অনুধাবন · সারাংশ ও সারমর্ম রচনা · ভাবসম্প্রসারণ · পত্র রচনা ·
অনুচ্ছেদ রচনা · প্রবন্ধ রচনা

**Class 8** — ব্যাকরণ: ভাষা · মাতৃভাষা ও রাষ্ট্রভাষা · সাধু ও চলিত রীতির পার্থক্য · ধ্বনি ও বর্ণ · সন্ধি ·
শব্দ ও পদ · শব্দগঠন · বাক্য · বিরামচিহ্ন · বানান · অভিধান · শব্দার্থ · বাগ্‌ধারা।
নির্মিতি: অনুধাবন শক্তি · সারাংশ ও সারমর্ম · ভাব-সম্প্রসারণ · পত্র রচনা (ব্যক্তিগত পত্র, আবেদন পত্র,
আমন্ত্রণ পত্র) · প্রবন্ধ রচনা (বাংলাদেশের ষড়ঋতু, বাংলা নববর্ষ, বিজয় দিবস, ট্রেন ভ্রমণ, মুক্তিযুদ্ধ
জাদুঘর, আমার ছেলেবেলা, বাংলাদেশের কৃষক, দৈনন্দিন জীবন ও বিজ্ঞান, ছাত্রজীবনের দায়িত্ব ও কর্তব্য,
শ্রমের মর্যাদা, পাঠাগারের প্রয়োজনীয়তা, কর্মমুখী শিক্ষা, অধ্যবসায়, স্বদেশ প্রেম)

*(Class 7's ব্যাকরণ ও নির্মিতি contents page could not be isolated from the PDF; the app uses only
the নির্মিতি topics common to classes 6 and 8, which are stable across the three books.)*

### ইংরেজি ১ম পত্র — English For Today

**Class 6** — 33 lessons: Going to a New School · Congratulations! Well Done! · At a Railway Station ·
Where are You From? · Thanks for Your Work · It Smells Good! · Holding Hands · Grocery Shopping ·
Health is Wealth · Remedies: Modern and Traditional · Are You Listening?-1 · An Unseen Beauty of
Bangladesh · Our Pride · The Lion's Mane · An Old People's Home · Boats Sail on the Rivers ·
Are You Listening?-2 · Make Your Snacks · Stop, Look and Listen · Hason Raja: The Mystic Bard of
Bangladesh · Wonders of the World-1 · Wonders of the World-2 · We Live in a Global Village ·
Our Wage Earners · The Concert for Bangladesh · Buying Clothes · Andre · Are You Listening?-3 ·
Taking a Test · What Should We do? · Too Much or Too Little Water · An Invitation for Robin · The Garden

**Class 7** — 9 units: Attention, Please · My Study Guide · What are Friends for? · People Who Make a
Difference · Great Women to Remember · Leisure · Games and Sports · Likes and Dislikes · Climate Change

**Class 8** — 11 units: A Glimpse of Our Culture · Food and Nutrition · Health and Hygiene ·
Check Your References · Humans and Environment · Going on a Trip · Occupations at Risk ·
News! News! News! · Things that Have Changed Our Lives · Fables · Women's Role in Uprisings

### ইংরেজি ২য় পত্র — English Grammar and Composition

**Class 6** — Grammar: Parts of Speech · The Tenses · Articles: a, an, the · Possessives ·
The -ing form of Verb: Gerund and Participle · Sentences · Introductory 'There' and 'It' ·
Punctuation and Capitalisation. Composition: Letters and E-mails · Writing Paragraphs

**Class 7** — Grammar: Parts of Speech · Modals · The Tense · Forms of Verbs · More about Adjectives ·
More about Adverbs · More about Prepositions · Linking Words · Introducing Articles · Possessives ·
The Sentence · Introductory 'There' · There isn't/There aren't · Infinitives, Gerunds and Participles ·
Capitalization and Punctuation · Direct Speech and Indirect Speech · Voice.
Composition: Writing Paragraphs · Letter Writing

**Class 8** — Grammar: Parts of Speech · Modals · Articles, linking words and Possessives ·
Degrees of Adjectives · Tenses · Infinitive, Gerund and Participle · Sentences · Voice ·
Direct and Indirect Speech · Suffixes and Prefixes · Capitalisation and Punctuation.
Composition: Writing process · Letter writing · Writing email · Developing Composition

### গণিত

**Class 6** — স্বাভাবিক সংখ্যা ও ভগ্নাংশ · অনুপাত ও শতকরা · পূর্ণসংখ্যা · বীজগণিতীয় রাশি · সরল সমীকরণ ·
জ্যামিতির মৌলিক ধারণা · ব্যবহারিক জ্যামিতি · তথ্য ও উপাত্ত

**Class 7** — মূলদ ও অমূলদ সংখ্যা · সমানুপাত ও লাভ-ক্ষতি · পরিমাপ · বীজগণিতীয় রাশির গুণ ও ভাগ ·
বীজগণিতীয় সূত্রাবলি ও প্রয়োগ · বীজগণিতীয় ভগ্নাংশ · সরল সমীকরণ · সমান্তরাল সরলরেখা · ত্রিভুজ ·
সর্বসমতা ও সদৃশতা · তথ্য ও উপাত্ত

**Class 8** — প্যাটার্ন · মুনাফা · পরিমাপ · বীজগণিতীয় সূত্রাবলি ও প্রয়োগ · বীজগণিতীয় ভগ্নাংশ ·
সরল সহসমীকরণ · সেট · চতুর্ভুজ · পিথাগোরাসের উপপাদ্য · বৃত্ত · তথ্য ও উপাত্ত

### বিজ্ঞান

**Class 6** — বৈজ্ঞানিক প্রক্রিয়া ও পরিমাপ · জীবজগৎ · উদ্ভিদ ও প্রাণীর কোষীয় সংগঠন · উদ্ভিদের বাহ্যিক
বৈশিষ্ট্য · সালোকসংশ্লেষণ · সংবেদী অঙ্গ · পদার্থের বৈশিষ্ট্য এবং বাহ্যিক প্রভাব · মিশ্রণ · আলোর ঘটনা ·
গতি · বল এবং সরল যন্ত্র · পৃথিবীর উৎপত্তি ও গঠন · খাদ্য ও পুষ্টি · পরিবেশের ভারসাম্য এবং আমাদের জীবন

**Class 7** — নিম্নশ্রেণির জীব · উদ্ভিদ ও প্রাণীর কোষীয় সংগঠন · উদ্ভিদের বাহ্যিক বৈশিষ্ট্য · শ্বসন ·
পরিপাকতন্ত্র এবং রক্ত সংবহনতন্ত্র · পদার্থের গঠন · শক্তির ব্যবহার · শব্দের কথা · তাপ ও তাপমাত্রা ·
বিদ্যুৎ ও চুম্বকের ঘটনা · পারিপার্শ্বিক পরিবর্তন ও বিভিন্ন ঘটনা · সৌরজগৎ ও আমাদের পৃথিবী ·
প্রাকৃতিক পরিবেশ এবং দূষণ · জলবায়ু পরিবর্তন

**Class 8** — প্রাণিজগতের শ্রেণিবিন্যাস · জীবের বৃদ্ধি ও বংশগতি · ব্যাপন, অভিস্রবণ ও প্রস্বেদন ·
উদ্ভিদের বংশ বৃদ্ধি · সমন্বয় ও নিঃসরণ · পরমাণুর গঠন · পৃথিবী ও মহাকর্ষ · রাসায়নিক বিক্রিয়া ·
বর্তনী ও চলবিদ্যুৎ · অম্ল, ক্ষারক ও লবণ · আলো · মহাকাশ ও উপগ্রহ · খাদ্য ও পুষ্টি · পরিবেশ এবং বাস্তুতন্ত্র

### বাংলাদেশ ও বিশ্বপরিচয়

**Class 6** — সমাজ বিবর্তনের ইতিহাস · বাংলাদেশের ইতিহাস · বাংলাদেশের সংস্কৃতি ও সমাজ · বাংলাদেশের
অর্থনীতি · বাংলাদেশ ও বাংলাদেশের নাগরিক · বাংলাদেশের পরিবেশ · শিশুর বেড়ে ওঠা ও প্রতিবন্ধকতা:
সামাজিকীকরণ · বাংলাদেশ ও আঞ্চলিক সহযোগিতা

**Class 7** — বাংলাদেশের মুক্তিসংগ্রাম ও গণআন্দোলন · বাংলাদেশের সংস্কৃতি ও সাংস্কৃতিক বৈচিত্র্য ·
পরিবারে শিশুর বেড়ে ওঠা · বাংলাদেশের অর্থনীতি · বাংলাদেশ ও বাংলাদেশের নাগরিক · বাংলাদেশের জলবায়ু ·
বাংলাদেশের জনসংখ্যা পরিচিতি · বাংলাদেশের সামাজিক সমস্যা · বাংলাদেশে প্রবীণ ব্যক্তি ও নারীর অধিকার ·
এশিয়ার কয়েকটি দেশ · বাংলাদেশ ও আন্তর্জাতিক সহযোগিতা

**Class 8** — ঔপনিবেশিক যুগ ও বাংলার স্বাধীনতা সংগ্রাম · ঔপনিবেশিক যুগের প্রত্নতাত্ত্বিক ঐতিহ্য ·
বাংলাদেশের মুক্তিযুদ্ধ ও গণতান্ত্রিক সংগ্রাম · বাংলাদেশের অর্থনীতি · বাংলাদেশ: রাষ্ট্র ও সরকার ব্যবস্থা ·
বাংলাদেশের সাংস্কৃতিক পরিবর্তন · সামাজিকীকরণ · বাংলাদেশের বিভিন্ন নৃগোষ্ঠী · বাংলাদেশের সামাজিক সমস্যা ·
বাংলাদেশের জনসংখ্যা ও উন্নয়ন · বাংলাদেশে জলবায়ু ও দুর্যোগ মোকাবিলা · বাংলাদেশের প্রাকৃতিক সম্পদ ·
বাংলাদেশ এবং বিভিন্ন আঞ্চলিক ও আন্তর্জাতিক সহযোগী সংস্থা

### তথ্য ও যোগাযোগ প্রযুক্তি

**Class 6** — তথ্য ও যোগাযোগ প্রযুক্তি পরিচিতি · তথ্য ও যোগাযোগ প্রযুক্তি সংশ্লিষ্ট যন্ত্রপাতি ·
তথ্য ও যোগাযোগ প্রযুক্তির নিরাপদ ব্যবহার · ওয়ার্ড প্রসেসিং · ইন্টারনেট পরিচিতি

**Class 7** — প্রাত্যহিক জীবনে তথ্য ও যোগাযোগ প্রযুক্তি · কম্পিউটার-সংশ্লিষ্ট যন্ত্রপাতি ·
নিরাপদ ও নৈতিক ব্যবহার · ওয়ার্ড প্রসেসিং · শিক্ষায় ইন্টারনেটের ব্যবহার

**Class 8** — তথ্য ও যোগাযোগ প্রযুক্তির গুরুত্ব · কম্পিউটার নেটওয়ার্ক · তথ্য ও যোগাযোগ প্রযুক্তির
নিরাপদ ও নৈতিক ব্যবহার · স্প্রেডশিটের ব্যবহার · শিক্ষা ও দৈনন্দিন জীবনে ইন্টারনেটের ব্যবহার

### কৃষিশিক্ষা

**Class 6** — আমাদের জীবনে কৃষি · কৃষি প্রযুক্তি ও যন্ত্রপাতি · কৃষি উপকরণ · কৃষি ও জলবায়ু ·
কৃষিজ উৎপাদন · বনায়ন

**Class 7** — কৃষি এবং আমাদের সংস্কৃতি · কৃষি প্রযুক্তি · কৃষি উপকরণ · কৃষি ও জলবায়ু · কৃষিজ উৎপাদন · বনায়ন

**Class 8** — বাংলাদেশের কৃষি ও আন্তর্জাতিক প্রেক্ষাপট · কৃষিপ্রযুক্তি · কৃষি উপকরণ · কৃষি ও জলবায়ু ·
কৃষিজ উৎপাদন · বনায়ন

### শারীরিক শিক্ষা ও স্বাস্থ্য

**Class 6** — শরীরচর্চা ও সুস্থ জীবন · স্কাউটিং ও গার্ল গাইডিং · স্বাস্থ্যবিজ্ঞান পরিচিতি ও স্বাস্থ্যসেবা ·
আমাদের জীবনে বয়ঃসন্ধিকাল · জীবনের জন্য খেলাধুলা

**Class 7** — শরীরচর্চা ও সুস্থজীবন · স্কাউটিং ও গার্ল গাইডিং · স্বাস্থ্যবিজ্ঞান পরিচিতি ও স্বাস্থ্যসেবা ·
বয়ঃসন্ধিকালে ব্যক্তিগত নিরাপত্তা · জীবনের জন্য খেলাধুলা

**Class 8** — শরীরচর্চা ও সুস্থজীবন · স্কাউটিং, গার্ল গাইডিং ও বাংলাদেশ রেড ক্রিসেন্ট সোসাইটি ·
আমাদের জীবনে প্রজনন স্বাস্থ্য · জীবনের জন্য খেলাধুলা

### চারু ও কারুকলা

**Class 6** — চারু ও কারুকলার পরিচয় · বাংলাদেশের চারু ও কারুকলা শিক্ষার ইতিহাস · বাংলাদেশের লোকশিল্প ও
কারুশিল্প · ছবি আঁকার সাধারণ নিয়ম, উপকরণ ও মাধ্যম · ছবি আঁকার অনুশীলন · কাগজ ও ফেলনা জিনিস দিয়ে
শিল্পকর্ম · রং ও রঙের ব্যবহার

**Class 7** — বাংলাদেশে চারুকলা শিক্ষার ইতিহাস · চিত্রকলা সর্বকালে সব মানুষের ভাষা · বাংলাদেশের লোকশিল্প
ও কারুশিল্প · ছবি আঁকার বিভিন্ন মাধ্যম · ছবি আঁকার নানারকম আনন্দদায়ক অনুশীলন · বিভিন্ন প্রকার শিল্পকর্ম ·
রঙ ও রঙের ব্যবহার

**Class 8** — বাংলাদেশের প্রাচীন শিল্পকলা ও ঐতিহ্যের পরিচয় · বাংলাদেশের অভ্যুদয়ে চারুশিল্প ও শিল্পীরা ·
বিখ্যাত শিল্পী ও শিল্পকর্ম · শিল্পকলা · ছবি আঁকার বিভিন্ন মাধ্যম ও উপকরণ · বিষয়ভিত্তিক ছবি ও নকশা অঙ্কন ·
বিভিন্ন মাধ্যমের শিল্পকর্ম · রং ও রঙিন ছবি

### কর্ম ও জীবনমুখী শিক্ষা

**Class 6** — কর্মেই আনন্দ · আমাদের প্রয়োজনীয় কাজ · শিক্ষায় সাফল্য

**Class 7** — কর্ম ও মানবিকতা · পারিবারিক কাজ ও পেশা · শিক্ষা পরিকল্পনা ও কর্মক্ষেত্রে সফলতা

**Class 8** — মেধা, কায়িকশ্রম ও আত্ম-অনুসন্ধান · আমাদের কাজ: যেগুলো অন্যেরা করে · আমাদের শিক্ষা ও কর্ম

### ধর্ম ও নৈতিক শিক্ষা

**ইসলাম শিক্ষা, classes 6–8** — আকাইদ · ইবাদত · কুরআন ও হাদিস শিক্ষা · আখলাক · আদর্শ জীবনচরিত
(the same five অধ্যায় in all three classes, with different sub-topics)

**হিন্দুধর্ম শিক্ষা**
- Class 6 — স্রষ্টা ও সৃষ্টি · ধর্মগ্রন্থ · হিন্দুধর্মের স্বরূপ ও বিশ্বাস · নিত্যকর্ম ও যোগাসন ·
  দেব-দেবী ও পূজা-পার্বণ · ধর্মীয় উপাখ্যানে নৈতিক শিক্ষা · অবতার ও আদর্শ জীবনচরিত · হিন্দুধর্ম ও নৈতিক মূল্যবোধ
- Classes 7–8 — ঈশ্বরের স্বরূপ · ধর্মগ্রন্থ · হিন্দুধর্মের স্বরূপ ও বিশ্বাস · নিত্যকর্ম ও যোগাসন ·
  দেব-দেবী ও পূজা-পার্বণ · ধর্মীয় উপাখ্যানে নৈতিক শিক্ষা · অবতার ও আদর্শ জীবনচরিত · হিন্দুধর্ম ও নৈতিক মূল্যবোধ

---

## 7. Method

1. Pulled the NCTB 2026 book list pages for classes 6, 7 and 8 (`nctb.gov.bd` serves an
   incomplete certificate chain, so the pages were fetched with the chain check relaxed).
2. Downloaded all 42 relevant textbook PDFs from the Google Drive links on those pages
   (~1 GB), one per (class, subject).
3. The PDFs draw Bangla as vector outlines, so `get_text()` returns either nothing or mojibake.
   Located each সূচিপত্র by scoring pages 3–14 for long horizontal rules — a ruled contents
   table scores far above a prose page — then read the winning page as a rendered image.
   Four books whose contents page is unruled were located by hand.
4. Cross-checked the derived subject list and period counts against the two signed NCTB
   assessment documents in §4 and §5.
