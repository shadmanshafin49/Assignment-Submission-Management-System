# অ্যাসাইনমেন্ট ব্যবস্থাপনা — Assignment & Submission Management System

A role-based assignment and submission system, built as the real thing for a real school:
**গাজীপুর ক্যান্টনমেন্ট বোর্ড উচ্চ বিদ্যালয়** (Gazipur Cantonment Board High School, EIIN 108957),
Bangla version, classes ষষ্ঠ–অষ্টম.

Teachers set work on the courses they actually take, students submit answers — typed or as a
photo of their খাতা — against a deadline, and teachers return marks and feedback. The whole
interface is in Bangla, because the school is Bangla-version.

The curriculum is not invented. Subjects, board subject codes, weekly period counts, textbook
chapters and the question types a teacher may set all come from NCTB's own published documents;
every claim is sourced in **[docs/RESEARCH.md](docs/RESEARCH.md)**.

Built for the OnnoRokom Projukti Ltd. Assistant Software Engineer recruitment project.

---

## Contents

- [Quick start](#quick-start)
- [Demo credentials](#demo-credentials)
- [What the school looks like in the app](#what-the-school-looks-like-in-the-app)
- [Technology stack](#technology-stack)
- [Project structure](#project-structure)
- [Running without Docker](#running-without-docker)
- [Running the tests](#running-the-tests)
- [Data model](#data-model)
- [Business rules](#business-rules)
- [API reference](#api-reference)
- [Design decisions](#design-decisions)
- [Assumptions](#assumptions)
- [Known limitations](#known-limitations)

---

## Quick start

**Prerequisites:** Docker Desktop. Nothing else — the .NET SDK and Node are only needed if you
want to run the projects directly.

```bash
git clone <repository-url>
cd "Onnorokom Projects"

cp .env.example .env          # Windows: copy .env.example .env
```

Open `.env` and set `JWT_KEY` to any string of at least 32 characters:

```bash
openssl rand -base64 48
```

```powershell
# Windows PowerShell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

Then:

```bash
docker compose up -d --build
```

| Service | URL |
|---|---|
| Web app | <http://localhost:3000> |
| API (Swagger UI) | <http://localhost:8080/swagger> |
| API health check | <http://localhost:8080/health> |
| PostgreSQL | `localhost:5432` |

The database is created, migrated and seeded automatically on first start — **no tables need
to be created by hand.** Seeding builds a full school: 3 classes × 30 students, 14 subjects,
42 courses, a complete 36-period weekly routine per class, and two weeks of assignments with
submissions in every state. Give the API ten seconds or so on first run, then sign in.

To reset everything back to a clean seeded state:

```bash
docker compose down -v && docker compose up -d
```

---

## Demo credentials

| Role | Email | Password |
|---|---|---|
| **প্রধান শিক্ষক (Admin)** | `admin@gcbhs.edu.bd` | `Admin@123` |
| **শিক্ষক (Teacher)** | `rejaul.karim@gcbhs.edu.bd` | `Teacher@123` |
| **শিক্ষার্থী (Student)** | `c6r01@student.gcbhs.edu.bd` | `Student@123` |

The login page fills these in with one click. Student addresses follow `c<class>r<roll>` —
`c6r01` is roll ১ of ষষ্ঠ শ্রেণি, `c8r30` is roll ৩০ of অষ্টম শ্রেণি. Every teacher listed in
[docs/RESEARCH.md](docs/RESEARCH.md) has an account; a few worth trying:

| Teacher | Email | Teaches |
|---|---|---|
| মোঃ রেজাউল করিম | `rejaul.karim@gcbhs.edu.bd` | গণিত + বাংলাদেশ ও বিশ্বপরিচয় — 6 courses, the busiest timetable |
| মোঃ মুকিম বিল্লাহ | `mukim.billah@gcbhs.edu.bd` | ইসলাম ও নৈতিক শিক্ষা — a faith-group course |
| পূর্ণিমা সরকার | `purnima.sarker@gcbhs.edu.bd` | হিন্দুধর্ম ও নৈতিক শিক্ষা + বিজ্ঞান |

All teachers use `Teacher@123`, all students `Student@123`. These are seed values for a
throwaway local database, documented so the project can be evaluated. No real secrets are
committed anywhere in this repository.

### A five-minute tour

1. Sign in as **রেজাউল করিম**. His assignment list covers গণিত and বাংলাদেশ ও বিশ্বপরিচয় across
   three classes, drafts included. **আমার রুটিন** shows exactly which class he is with in each
   of the week's 36 periods.
2. Create an assignment. Pick a course and watch the **কাজের ধরন** list change: গণিত offers
   সৃজনশীল প্রশ্ন and গাণিতিক সমস্যা, বাংলা ২য় পত্র offers ভাবসম্প্রসারণ and সারাংশ. The API
   refuses a type the subject does not set — this is NCTB's own per-subject question list, not
   a UI convenience.
3. Open a published assignment → the submissions table is **in roll order**, because that is how
   a teacher marks. Grade one; try marks above the maximum and the API rejects it.
4. Sign in as **c6r01**. Drafts are absent — filtered out by the API, not hidden by the UI.
   Submit an answer with a photo attached, then edit it.
5. Ask a question in **শ্রেণি আলোচনা** under an assignment. It is visible to the whole class,
   like Google Classroom. Sign back in as the teacher and reply.
6. Sign in as **c6r28** — a Hindu student. Their subject list carries হিন্দুধর্ম ও নৈতিক শিক্ষা
   and *not* ইসলাম ও নৈতিক শিক্ষা, even though both run in their class. Their class routine shows
   both courses in the same period, taught in parallel.
7. Sign in as the **admin** to manage users, classes, subjects, courses, enrolments and the
   routine grid, and to watch every teacher's work read-only.

---

## What the school looks like in the app

| | |
|---|---|
| Classes | ষষ্ঠ, সপ্তম, অষ্টম — 30 students each, all male (the school is a boys' school) |
| Subjects | 14, with board codes: ১০১ বাংলা ১ম পত্র … ১৫৫ কর্ম ও জীবনমুখী শিক্ষা |
| Courses | 42 — one per (class, subject), coded `C06-101`, `C07-108`, `C08-109` |
| Week | শনিবার–বৃহস্পতিবার, 6 periods a day = **36 periods**; শুক্রবার is the holiday |
| Bell times | সমাবেশ ০৭:৪৫, first period 60 min for roll call, then 50 min; three periods, টিফিন ১০:৪০–১১:৩০, three more, out at ১৪:০০ |
| Assignment types | 18 NCTB question and coursework types, restricted per subject |

**Course codes** follow the school's own convention: `C` + two-digit class + `-` + the board's
subject code. `C06-101` is বাংলা ১ম পত্র for class 6; `C07-108` is ইংরেজি ২য় পত্র for class 7.

**Religion is a faith group, not a subject everyone shares.** ইসলাম ও নৈতিক শিক্ষা (১১১) and
হিন্দুধর্ম ও নৈতিক শিক্ষা (১১২) are separate courses in the same class, taught in the same period
by different teachers. A student's `Faith` decides which one they take, which submission they may
make, and which denominator a teacher sees ("১৮ / ২৭ জমা দিয়েছে", not out of 30).

---

## Technology stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS 4 |
| Forms & validation | react-hook-form + Zod |
| Data fetching | TanStack Query |
| Backend | ASP.NET Core 10 Web API, C# |
| Validation | FluentValidation |
| Logging | Serilog (console + rolling file) |
| API docs | Swagger / OpenAPI with JWT authorisation |
| Database | PostgreSQL 16 + EF Core 10 (code-first migrations) |
| Auth | JWT bearer (HS256) + rotating refresh tokens, PBKDF2 password hashing |
| File storage | local disk behind an `IFileStorage` abstraction (swap for S3/MinIO in one line) |
| Testing | xUnit, Shouldly, SQLite in-memory, `FakeTimeProvider` |
| Deployment | Docker + Docker Compose |

---

## Project structure

```
.
├─ backend/
│  ├─ src/
│  │  ├─ Ams.Domain/           entities, enums, domain rules — no dependencies
│  │  ├─ Ams.Application/      DTOs, services (all business rules), validators
│  │  ├─ Ams.Infrastructure/   EF Core, migrations, seed data, JWT, hashing, file storage
│  │  │  └─ Persistence/SeedData/   curriculum, teachers, routine builder, assignment templates
│  │  └─ Ams.Api/              controllers, middleware, Swagger, DI wiring
│  ├─ tests/Ams.UnitTests/     164 tests over business rules and authorization
│  └─ Dockerfile
├─ frontend/
│  ├─ src/app/(app)/           admin, teacher and student pages
│  ├─ src/app/api/             login/logout handlers + the API proxy route
│  ├─ src/components/          shared UI, forms, routine grid, comment thread, attachments
│  ├─ src/hooks/               TanStack Query hooks per role
│  ├─ src/lib/                 API client, session/cookie handling, types, Bangla labels
│  ├─ src/proxy.ts             role-aware routing guard
│  └─ Dockerfile
├─ db/
│  ├─ seed.sql                 pg_dump of the migrated + seeded database
│  └─ README.md                database setup notes
├─ docs/RESEARCH.md            curriculum sources — every NCTB fact the seed data claims
├─ docker-compose.yml
├─ .env.example
└─ README.md
```

Business rules live in `Ams.Application`, not in controllers, so the test suite exercises them
directly without going through HTTP.

---

## Running without Docker

<details>
<summary>Backend — requires .NET SDK 10</summary>

Start a PostgreSQL instance (the compose file provides one):

```bash
docker compose up -d db
```

Provide the JWT signing key. User-secrets keeps it out of the repository:

```bash
cd backend/src/Ams.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<at least 32 characters>"
dotnet run
```

The API starts on <http://localhost:5140> and applies migrations plus seed data on startup.
The default connection string in `appsettings.json` points at `localhost:5432` with the
credentials from `.env.example`; override it with `ConnectionStrings__Default` if needed.
Uploaded files are written under `backend/src/Ams.Api/uploads/`, overridable with `Storage:Root`.
</details>

<details>
<summary>Frontend — requires Node 20+</summary>

```bash
cd frontend
npm install
npm run dev
```

Runs on <http://localhost:3000> and expects the API at <http://localhost:5140>. Point it
elsewhere with `API_BASE_URL` in `frontend/.env.local`.
</details>

---

## Running the tests

```bash
cd backend
dotnet test
```

**164 tests, all passing.** They cover the rules an evaluator would want to see enforced:

| Area | What is covered |
|---|---|
| `Assignments/AssignmentAuthorizationTests` | who may set work; a teacher is confined to their own courses; per-subject type rules |
| `Assignments/AssignmentLifecycleTests` | draft → published; deadline and marks validation; delete/retarget guards |
| `Assignments/StudentVisibilityTests` | drafts, other classes' work and another faith's religion course never reach a student |
| `Submissions/SubmissionWorkflowTests` | deadlines, late policy, one submission per student, edit windows, answer files |
| `Submissions/GradingTests` | mark bounds, who may grade, return-for-revision, roll-order marking list |
| `Admin/AcademicStructureTests` | courses, staffing, enrolment, roll numbers, faith prerequisites |
| `Admin/RoutineTests` | 36-period week, teacher double-booking, parallel religion periods |
| `Admin/UserManagementTests` | admin-only account management, self-lockout guards, faith/designation rules |
| `Auth/AuthenticationTests` | login, refresh-token rotation, password change |
| `Auth/PasswordHasherTests` | salting, verification, malformed-hash handling |

Two choices worth calling out:

- Tests run against **SQLite in-memory**, not the EF Core InMemory provider, because the rules
  under test depend on real relational behaviour. The unique index on
  `(AssignmentId, StudentId)` that enforces "one submission per student" is silently ignored by
  the InMemory provider, which would make those tests pass for the wrong reason.
- Every deadline-sensitive rule uses an injected `TimeProvider` with `FakeTimeProvider`, so the
  clock can be advanced explicitly. No test depends on when the suite happens to run.

The test fixture is a small slice of the real school — ষষ্ঠ and সপ্তম শ্রেণি, রেজাউল teaching
গণিত, আফসার teaching বাংলা and deliberately *not* গণিত, and one Hindu student in a class of
Muslims. Every authorisation test turns on one of those facts.

The frontend build also type-checks the whole app:

```bash
cd frontend && npm run build
```

---

## Data model

```
User ──< Enrollment >── ClassRoom ──┐
 │  (roll number)                   │
 │                                  ├──< Course >── Subject ──< SubjectAssignmentType
 └──────── teaches ─────────────────┘      │  (C06-109)
                                           │
                          RoutinePeriod ───┤   (day × period → course)
                                           │
                                    Assignment ──< Submission >── User (student)
                                       │  │            │
                                       │  └─ AssignmentAttachment
                                       │               └─ SubmissionAttachment
                                       └─ AssignmentComment
```

| Table | Purpose |
|---|---|
| `users` | all accounts; `Role` is Admin, Teacher or Student; students carry a `Faith` |
| `class_rooms` | ষষ্ঠ / সপ্তম / অষ্টম শ্রেণি, with the class level the course code is built from |
| `subjects` | the NCTB subject, its board code, full marks, weekly periods and faith group |
| `subject_assignment_types` | **which question types this subject may set** |
| `courses` | **one subject taught to one class by one teacher** — `C06-109` |
| `enrollments` | which student sits in which class, and at which roll number |
| `routine_periods` | one cell of the weekly routine: (class, day, period) → course |
| `assignments` | the work itself: type, chapter, deadline, marks, draft/published |
| `assignment_attachments` | worksheets and question papers hung off an assignment |
| `assignment_comments` | the class conversation under an assignment |
| `submissions` | one student's answer, with status, marks and feedback |
| `submission_attachments` | the files that make up an answer |
| `app_settings` | school details and admin-tunable defaults |
| `refresh_tokens` | hashed, rotating refresh tokens |

`courses` is the load-bearing table. It replaced an earlier "teaching grant" table, which only
recorded who was *allowed* to teach: a course records what is actually taught, and is
simultaneously the gate on assignment creation, the row the routine points at, and the unit a
student's subject list is built from. A teacher who does not hold the course simply cannot set
work on it, and the API says so with a 403.

Enums are stored as **text** (`'Published'`, not `2`) so the database is readable on its own.

---

## Business rules

These are implemented in the service layer and covered by the test suite.

**Authorisation**

1. Only an admin may manage users, classes, subjects, courses, enrolments, the routine and settings.
2. A teacher may create an assignment only on a course an admin put them on.
3. A teacher may edit, delete and grade only within their own assignments.
4. An admin sees everything read-only, and deliberately **cannot grade** and **cannot comment** —
   teaching is the teacher's job.
5. A student may read only their own submissions.

**Curriculum**

6. An assignment's type must be one the subject actually sets — ভাবসম্প্রসারণ is a বাংলা ২য় পত্র
   task and cannot be set in গণিত.
7. A subject may not withdraw an allowed type while assignments of that type exist.
8. One offering of a subject per class per year, and a course code is derived, never typed.
9. A student must have a faith group before enrolment, because ধর্ম ও নৈতিক শিক্ষা is compulsory
   and nothing else can decide which of the streams they take.
10. An enrolled student's faith group cannot be changed — it would silently swap their religion
    course out from under their existing work.
11. Roll numbers are unique within a class, and marking lists come back in roll order.

**Routine**

12. A period is 1–6 on one of six teaching days; শুক্রবার cannot be expressed at all.
13. A slot holds one course, *except* that faith-group courses run in parallel — the two halves
    of a class take religion at the same time.
14. A teacher cannot be booked into two classrooms in the same period. No unique index can
    express this, because it spans two classes' rows.

**Assignment lifecycle**

15. Assignments start as **drafts** and are invisible to students until published.
16. Publishing requires a future deadline and `maxMarks > 0`; a draft may be parked with any
    deadline while it is being written.
17. Omitting the deadline applies the school's default window (seven days, admin-tunable).
18. The course cannot be changed once submissions exist.
19. An assignment with submissions cannot be deleted or reverted to draft.
20. `maxMarks` cannot be lowered below marks already awarded.
21. Deleting an assignment deletes its stored files too — rows cascade, bytes do not.

**Submission workflow**

22. Students see only published work on courses they take — which already excludes another
    faith's religion course.
23. An answer must contain something: typed text, at least one file, or both.
24. After the deadline: refused unless the assignment allows late work, in which case the
    submission is accepted and flagged `Late`.
25. One submission per student per assignment — enforced by a unique index, not just in code.
26. A student may edit a submission only while resubmission is allowed, the work is ungraded,
    and the deadline has not passed.
27. Work **returned for revision** re-opens editing even past the deadline — the teacher
    deliberately reopened it — and returns to the grading queue on resubmit.
28. Removing the last file from an answer with no text is refused; it would leave a submission
    that reads as submitted with nothing in it.
29. Uploads are checked against an extension allow-list, a per-file size limit and a per-post
    count, all admin-tunable.

**Grading**

30. Marks must satisfy `0 ≤ marks ≤ maxMarks`.
31. Only the teacher who owns the assignment may grade it.
32. Returning work for revision clears the previous grade, so a student is never shown marks
    for an answer they are being asked to replace.
33. The expected-submission count excludes students of another faith, so a religion course reads
    "১৮ / ২৭" rather than out of the whole class.

---

## API reference

Full interactive documentation is at **<http://localhost:8080/swagger>**. Sign in via
`POST /api/auth/login`, copy the `accessToken`, click **Authorize**, and paste it.

```
POST   /api/auth/login                          public
POST   /api/auth/refresh                        public
POST   /api/auth/logout                         public
GET    /api/auth/me                             any signed-in user
POST   /api/auth/change-password                any signed-in user

GET    /api/reference                           any signed-in user — Bangla labels, bell times

GET    /api/users                               Admin
POST   /api/users                               Admin
PUT    /api/users/{id}                          Admin
DELETE /api/users/{id}                          Admin  (deactivates)

GET    /api/classes  /api/subjects              any signed-in user (for dropdowns)
POST   /api/classes  /api/subjects              Admin
PUT    /api/classes/{id}  /api/subjects/{id}    Admin
DELETE /api/classes/{id}  /api/subjects/{id}    Admin
GET    /api/classes/{id}/roster                 Teacher (own classes) / Admin

GET    /api/courses                             Teacher (own) / Admin (all)
GET    /api/courses/{id}                        Teacher / Admin / enrolled Student
POST   /api/courses                             Admin
PUT    /api/courses/{id}/teacher                Admin
DELETE /api/courses/{id}                        Admin
GET    /api/me/courses                          Teacher
GET    /api/me/enrolled-courses                 Student

GET    /api/enrollments                         Admin
POST   /api/enrollments                         Admin
DELETE /api/enrollments/{id}                    Admin

GET    /api/classes/{id}/routine                any signed-in user (students: own class)
GET    /api/me/routine                          Teacher
PUT    /api/routine                             Admin
DELETE /api/routine?classRoomId=&day=&periodIndex=   Admin

GET    /api/assignments                         Teacher (own) / Admin (all)
POST   /api/assignments                         Teacher
PUT    /api/assignments/{id}                    owning Teacher / Admin
POST   /api/assignments/{id}/publish            owning Teacher / Admin
POST   /api/assignments/{id}/unpublish          owning Teacher / Admin
DELETE /api/assignments/{id}                    owning Teacher / Admin
POST   /api/assignments/{id}/attachments        owning Teacher      (multipart)
GET    /api/assignments/{id}/attachments/{aid}  anyone who may see the assignment
DELETE /api/assignments/{id}/attachments/{aid}  owning Teacher
GET    /api/assignments/{id}/comments           Teacher / Admin / enrolled Student
POST   /api/assignments/{id}/comments           Teacher / enrolled Student
DELETE /api/assignments/{id}/comments/{cid}     author or the course's Teacher
GET    /api/assignments/{id}/submissions        owning Teacher / Admin

GET    /api/submissions                         Teacher (own) / Admin (all)
GET    /api/submissions/{id}                    author / owning Teacher / Admin
PUT    /api/submissions/{id}/grade              owning Teacher
PUT    /api/submissions/{id}/status             owning Teacher
GET    /api/submissions/{id}/attachments/{aid}  author / owning Teacher / Admin

GET    /api/student/assignments                 Student
GET    /api/student/assignments/{id}            Student
POST   /api/student/assignments/{id}/submission Student  (multipart: text + files)
PUT    /api/student/submissions/{id}            Student
POST   /api/student/submissions/{id}/attachments        Student  (multipart)
GET    /api/student/submissions/{id}/attachments/{aid}  Student
DELETE /api/student/submissions/{id}/attachments/{aid}  Student
GET    /api/student/submissions                 Student

GET    /api/settings                            any signed-in user
PUT    /api/settings                            Admin

GET    /health                                  public
```

Every list endpoint supports `?page=` and `?pageSize=` plus filters relevant to the resource
(`status`, `courseId`, `classRoomId`, `subjectId`, `type`, `weekNumber`, `search`, …). Errors are
returned as RFC 7807 `ProblemDetails` with an `errorCode` and `traceId` — and their messages are
in Bangla, because they are shown to the user.

---

## Design decisions

**PostgreSQL over MongoDB.** The domain is inherently relational — a submission is meaningless
without its assignment, which is meaningless without its course, class and subject. Foreign keys
and unique indexes let the database enforce rules like "one submission per student" rather than
trusting application code to always check first.

**Business rules in the service layer, not controllers.** Controllers translate HTTP and
nothing more. This keeps the rules unit-testable without spinning up a web host, and means a
rule cannot be bypassed by adding a second endpoint that forgets to check it.

**`Course` instead of a teaching-grant table.** An earlier design had a grant table recording
which teacher *may* teach which (class, subject). Modelling the course itself is strictly better:
it is the same gate on assignment creation, but it is also the thing the routine schedules, the
thing a student's subject list is built from, and the thing that carries a code the school
already uses (`C06-109`). One row, four jobs.

**Per-subject assignment types.** NCTB publishes the question types for each subject, so
`SubjectAssignmentType` makes that a rule rather than a convention: a maths teacher cannot set a
ভাবসম্প্রসারণ, and the type dropdown narrows to the subject the moment a course is chosen. An
empty list fails closed — a misconfigured subject refuses everything rather than allowing
everything.

**Faith group on the student, not a separate enrolment.** ধর্ম ও নৈতিক শিক্ষা is compulsory and
every student takes exactly one of its streams, so a single `Faith` field derives their course,
their visibility, their submission rights and the teacher's denominator. A second join table
would have made a one-of-four choice look like a many-to-many.

**Bangla labels served from the API.** "সৃজনশীল প্রশ্ন" is the name of an NCTB question type, not
interface copy, so it lives with the enum that defines it and reaches the frontend through
`GET /api/reference`, fetched once per navigation in the server layout. A second copy in the
frontend is a copy that drifts.

**Identity always comes from the token.** Services read the caller from `ICurrentUser`, which is
derived from the validated JWT. No endpoint trusts a user id in a request body, so a student
cannot submit "as" a classmate by editing the payload.

**Authorisation is enforced twice.** Controllers carry `[Authorize(Roles = …)]` for a fast,
declarative first check, and the services re-check independently. The attribute alone would be
a single point of failure if a route were ever added without it.

**404 rather than 403 for hidden resources.** A student requesting a draft assignment, another
class's work, or another faith's religion course gets 404. Returning 403 would confirm the thing
exists.

**Files behind `IFileStorage`.** The database stores metadata; bytes go to disk under a
server-generated key, so an uploaded filename never reaches the filesystem. Swapping the local
implementation for S3 or MinIO is a single registration change, and the tests use an in-memory
one so upload rules are covered without touching a disk.

**Text and files submitted together.** A submission must contain *something*, so the first
submit is one multipart request. Uploading afterwards would mean creating an empty submission
first and having it rejected.

**Tokens in httpOnly cookies, proxied through Next.js.** The browser never holds the JWT in
JavaScript-reachable storage, so an XSS payload cannot exfiltrate it. All browser traffic goes
to a same-origin `/api/proxy` route which attaches the bearer token server-side, transparently
refreshes on a 401, and keeps CORS out of the picture entirely. It forwards multipart bodies and
binary responses untouched, so uploads and downloads take the same protected path.

**The shipped stack runs in `Production`, not `Development`.** The 500 handler is the only thing
in the app that branches on the environment: in Development it returns `exception.ToString()` so a
developer sees the stack trace, and in Production it returns "Please try again later." Compose
originally set `Development`, which meant the one command the README leads with served internal
type names, file paths and line numbers to anyone who could provoke an error. Nothing else needed
that mode — Swagger and the migrate-and-seed on boot are both unconditional — so compose now sets
`Production`, and two tests in `ResponsePipelineTests` hold both halves of the branch.

**Three response headers, set from `OnStarting`.** `X-Content-Type-Options: nosniff`,
`X-Frame-Options: DENY` and `Referrer-Policy: no-referrer` on every API response, and the same
three plus `frame-ancestors 'none'` on the web tier. `nosniff` is the one that earns its place: an
attachment's `Content-Type` is whatever the uploading browser claimed, and it is echoed back on
download. `Content-Disposition: attachment` already means the bytes are saved rather than rendered;
`nosniff` is what stops a browser overriding that from the bytes themselves. They are registered
through an `OnStarting` callback rather than written on the way in, because the exception handler
calls `Response.Clear()` — so headers written earlier would vanish from exactly the error responses
that most need them. A test covers that case specifically.

**A light, cheerful interface — deliberately not a dark console.** The users are eleven to
fourteen. Each subject carries its own colour, keyed by board code, so a stream of fourteen
subjects is scannable without reading. `color-scheme: light` is pinned, otherwise a Windows
machine in dark mode renders the form controls dark against light surfaces.

**Bangla-first typography.** The interface is set in Kalpurush, self-hosted from `public/fonts`
(SIL OFL 1.1) rather than a CDN, so it renders the same on a machine with no route to the open
internet. The `@font-face` tries `local()` first — most Bangladeshi desktops already have the
font and pay nothing — and falls back through Noto Sans Bengali, Nirmala UI and the other device
Bengali faces, so a browser that never gets the webfont still lands on a Bengali face rather than
on Arial. Line height is raised because Bangla's মাত্রা and below-line vowel signs need the room.

**Admins cannot grade.** The brief assigns marking to teachers. Rather than leaving this
ambiguous, admins have full read-only oversight and grading is refused with a 403. They are also
kept out of the class comment thread: they observe the school, they are not in the lesson.

**Accounts are deactivated, never deleted.** Hard-deleting a user would orphan or cascade away
the assignments and submissions that reference them. Deactivation blocks login and revokes
outstanding sessions immediately — and a teacher still holding courses cannot be deactivated at
all, because their courses would be left unteachable.

---

## Assumptions

Where the brief or the school's own practice left something open, these are the calls I made:

1. **Six teaching days, six periods a day = 36 periods.** The NCTB document specifies a five-day,
   30-period week; the six-day week is the pre-2022 national norm and remains common in
   cantonment-board schools. Friday is the weekly holiday. Derivation in
   [docs/RESEARCH.md §4](docs/RESEARCH.md).
2. **Four optional-group subjects are taught, not one.** NCTB requires any one of কৃষিশিক্ষা /
   শারীরিক শিক্ষা / চারু ও কারুকলা / কর্ম ও জীবনমুখী শিক্ষা; this school teaches all four, which is
   where the seven periods above NCTB's 29 compulsory ones go. Every compulsory subject keeps its
   official weekly count.
3. **One section per class.** The data model carries a `Section` column, so ষষ্ঠ-ক and ষষ্ঠ-খ are
   expressible, but the seeded school runs one section of thirty per class.
4. **A student belongs to one class,** modelled as an `Enrollment` row so a mid-year transfer or a
   second class is possible without a schema change.
5. **Assignments target a course,** so গণিত for ষষ্ঠ শ্রেণি is distinct from গণিত for সপ্তম শ্রেণি
   even though both are taught by the same teacher.
6. **Answers may be typed, uploaded, or both.** Handwritten maths and চারু ও কারুকলা drawings are
   photographed far more often than typed, so a file-only answer is a first-class case.
7. **"Update a submission before the deadline, if allowed"** is a per-assignment
   `allowResubmission` flag, defaulting from a school-wide setting.
8. **Late submissions are accepted and flagged rather than refused,** when the assignment's
   `allowLateSubmission` permits it. The teacher sees the `Late` badge and decides what to do.
9. **Comments are a class conversation, not a private channel.** Like Google Classroom, a question
   asked under an assignment is visible to everyone on the course, so the answer reaches the
   whole class. Per-assignment `allowComments` can close it.
10. **There is no public sign-up.** Accounts are created by an admin, because self-registration
    with a role field would let anyone claim Teacher or Admin.
11. **Subjects are global** and shared across classes; the (class, subject, teacher) triple lives
    in `courses`.
12. **All times are stored in UTC** (`timestamptz`) and rendered in the browser's local timezone.
13. **Single institution.** No multi-school or multi-tenant support.
14. **"Application-level settings"** is read as the school's own record (name, EIIN, academic
    year) plus admin-tunable defaults for assignments and uploads — sixteen keys at
    `/api/settings`, grouped by category on the admin page, with the structural ones
    (periods per day, teaching days) deliberately read-only.

---

## Known limitations

Things I would add next, listed honestly rather than hidden:

- **Uploaded files are not scanned.** Extension allow-list, size cap and per-post count are
  enforced, and the stored name is server-generated, but there is no virus scanning and no
  content sniffing to catch a `.pdf` that is really something else.
- **Attachment bytes live on the API container's disk.** Fine for this exercise and abstracted
  behind `IFileStorage`, but a multi-instance deployment needs S3/MinIO, and `db/seed.sql`
  cannot carry the files (see [db/README.md](db/README.md)).
- **No notifications.** No email or in-app alert when work is published, submitted or graded.
- **No plagiarism or duplicate-answer detection.**
- **No bulk operations.** Enrolling thirty students is thirty forms; the seeder does it in code,
  but an admin would want a CSV import.
- **The routine is edited one cell at a time.** There is no auto-scheduler that fills 36 periods
  against each subject's weekly quota — the seeder does exactly that, but the admin UI does not
  expose it.
- **Refresh tokens are not cleaned up.** Revoked and expired rows accumulate; production would
  want a background job to prune them.
- **No rate limiting on login, and the topology is why it is not a one-liner.** The endpoint
  returns an identical error for unknown users and wrong passwords so it cannot be used to
  enumerate accounts, but nothing throttles repeated attempts. The obvious fix — ASP.NET's
  built-in rate limiter partitioned by client IP — is the wrong one here: every browser request
  reaches the API through the Next proxy, so the API sees one source address for the whole school.
  An IP limiter would bucket all six hundred users together and turn a brute-force defence into a
  self-inflicted outage. Doing this properly means `UseForwardedHeaders` with the proxy trusted,
  or a per-account failed-attempt counter and lockout, which is a schema change. Left undone
  rather than done wrongly.
- **No `script-src` Content-Security-Policy.** The web tier sends `frame-ancestors 'none'`,
  `nosniff` and `no-referrer`, and there is no `dangerouslySetInnerHTML` anywhere in the frontend,
  so React's escaping is the actual XSS defence. A real `script-src` policy needs per-request
  nonces threaded through Next's inline bootstrap scripts; a policy loose enough to skip that
  (`'unsafe-inline'`) would assert protection it does not provide.
- **Grading is one submission at a time.** No batch marking UI.
- **Analytics are basic.** Submission and graded counts per assignment, but no class averages,
  per-student progress, or the ধারাবাহিক মূল্যায়ন roll-up NCTB describes.
- **Tests are unit and service-level.** There is no browser end-to-end suite; the frontend is
  covered by TypeScript, a production build, and scripted HTTP verification through its own proxy
  rather than automated UI tests.
- **No correlation-id response header.** Every `ProblemDetails` body already carries a `traceId`,
  so a second request identifier would have been ceremony.
- **Session cookies are `Secure` in production.** Correct, but it means the Docker stack must be
  reached at <http://localhost:3000> — browsers treat `localhost` as a secure context — or over
  HTTPS. Browsing to it by LAN IP over plain HTTP silently drops the cookies, and login will look
  like it does nothing.
- **English is not offered.** The school is Bangla-version, so the UI is Bangla only; the data
  model keeps an English name on every person, class and subject, which is where a future
  language toggle would start.
