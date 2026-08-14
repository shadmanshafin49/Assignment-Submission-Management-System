# Build Log

Chronological record of what was built, what was decided, and what broke.
Newest entries at the bottom. Companion to [PLAN.md](PLAN.md) (the intent) and
[PROGRESS.md](PROGRESS.md) (the scoreboard).

Times are local (UTC+06).

---

## 2026-08-12

### 14:46 — Project intake
- Read the recruitment brief PDF (`Assistant Software Engineer Recruitment Project_*.pdf`).
- `git init` on `master`. No commit yet.

### 14:50 — Plan written
- [PLAN.md](PLAN.md) committed to disk: stack decisions, data model, the 16 business rules,
  API surface, 3-day schedule, cut order.
- Key call: **PostgreSQL + EF Core code-first**, so the evaluator never hand-creates tables.
- Key call: business rules live in `Ams.Application` services, not controllers — so unit tests
  hit the rules directly without spinning up HTTP.

### ~15:00 — Solution scaffold
- `Ams.sln` + 4 projects in Clean Architecture layering:
  `Ams.Domain` ← `Ams.Application` ← `Ams.Infrastructure` ← `Ams.Api`, plus `tests/Ams.UnitTests`.
- Target framework **net10.0**.

### ~15:07 — Domain layer
- 9 entities: `User`, `ClassRoom`, `Subject`, `Enrollment`, `TeacherAssignment`,
  `Assignment`, `Submission`, `AppSetting`, `RefreshToken`.
- 3 enums (`UserRole`, `AssignmentStatus`, `SubmissionStatus`) + domain exception types.
- Enums serialize as **names**, not ordinals, so the frontend never mirrors numeric values.

### ~15:14 — Application layer
- 6 services behind interfaces: `Auth`, `User`, `Academic`, `Assignment`, `Submission`, `Settings`.
- DTOs split by area; FluentValidation validators per area.
- `PagedResult<T>` + `QueryableExtensions` for pagination/filtering.

### 15:23 — Infrastructure + database
- `AppDbContext` with 9 `IEntityTypeConfiguration` classes (unique indexes on
  `User.Email`, `ClassRoom.Code`, `Subject.Code`, `(StudentId, ClassId)`,
  `(TeacherId, ClassId, SubjectId)`, `(AssignmentId, StudentId)`).
- Initial migration `20260812092344_InitialCreate` generated.
- `DbSeeder` (idempotent) + `DesignTimeDbContextFactory` for `dotnet ef` without a running app.
- JWT `TokenService`, `PasswordHasher` (PBKDF2 via `AspNetCore.Cryptography.KeyDerivation`).
- [docker-compose.yml](docker-compose.yml) with the Postgres service.

### 15:24 — API layer
- 7 controllers covering the full planned surface (see PROGRESS.md §API for the endpoint-by-endpoint map).
- `ExceptionHandlingMiddleware` → RFC 7807 `ProblemDetails`; `ValidationFilter` for model state.
- Serilog → console + daily rolling file (`logs/ams-.log`, 7 retained).
- Swagger with the **Authorize** button wired to raw-JWT bearer, XML doc comments included.
- CORS policy for `http://localhost:3000`.
- `GET /health` anonymous.
- **Migrations + seed run on boot** — `docker compose up` is enough to get a populated DB.
- Fail-fast guard: app refuses to start if `Jwt:Key` is missing or under 32 chars.
- [.env.example](.env.example) + [.gitignore](.gitignore) written; real `.env` is gitignored.

### 15:31 — Test suite
- `Ams.UnitTests`: xUnit + Shouldly + NSubstitute + `FakeTimeProvider`, EF Core **Sqlite in-memory**
  (chosen over the InMemory provider so unique-constraint violations actually surface).
- `TestWorld` / `TestContext` fixtures build a populated world per test.
- Coverage grouped by rule area: Auth, Admin, Assignments, Submissions, Grading.

---

## 2026-08-12 — session 2 (resumed)

### Baseline re-verified
- `dotnet` was **not on PATH** in this shell session (installed at `C:\Program Files\dotnet\dotnet.exe`).
  Workaround: invoke by full path. Not a project defect.
- `dotnet test` → **112 passed, 0 failed, 0 skipped** (5s).
- Docker daemon: up (server 29.5.2). Node v22.18.0 / npm 10.9.3 available for the frontend.
- Reviewed `Program.cs`: Serilog, Swagger bearer, health check, CORS, boot-time migrate+seed all
  confirmed present. The "backend gaps" I expected to find were already closed.
- **Risk flagged:** the repo still has *zero commits*. Everything above is untracked working tree.

### Started
- This file and [PROGRESS.md](PROGRESS.md).
- Next: frontend scaffold (the one mandated deliverable with nothing on disk yet).

### ~15:45–23:52 — Frontend built
- Next.js 16 App Router + TypeScript + Tailwind 4, TanStack Query, react-hook-form + zod.
- **Auth architecture call:** tokens never touch browser JavaScript. `/api/auth/login` and a
  catch-all `/api/proxy/[...path]` route handler hold the access and refresh tokens in httpOnly
  cookies and attach the bearer server-side. Three things fall out of that: no XSS payload can
  read a token, CORS never applies because the browser only ever talks to its own origin, and a
  401 can be retried after a silent refresh instead of dropping the user on `/login` mid-task.
- `middleware.ts` guards routes by the role claim. Treated explicitly as a UX layer, not a
  security boundary — the API re-checks every rule on every request.
- All 20 routes built: login, student (list/detail/submit/my-submissions), teacher
  (list/detail/submissions/grade), admin (users, classes, subjects, enrollments,
  teacher-assignments, settings).
- `db/seed.sql` exported, `frontend/Dockerfile` and the compose `web` service written.

---

## 2026-08-13 — session 3 (resumed)

### 01:30 — Re-baselined against disk
- PROGRESS.md was badly out of date: it claimed the frontend was at 0%, but the whole app had
  been built the previous evening. Lesson: the scoreboard is only useful if it is written at the
  same time as the code, not from memory the next day.
- Real remaining gaps turned out to be: no README, no commits, and no end-to-end verification.

### 01:34 — Frontend production build
- `npm run build` → clean. 20 routes, TypeScript passes.
- One deprecation notice: Next 16 renamed the `middleware` file convention to `proxy`. Cosmetic;
  left alone this close to the deadline.

### 01:37 — Backend verified against live Postgres
This was the biggest open unknown, and it closed cleanly.

- `ams-db` had been up 10 hours; the schema was already migrated and seeded, which retroactively
  proves the boot-time migrate+seed path works.
- Confirmed in the database: 7 users, 2 classes, 4 subjects, 5 enrollments, 5 teacher-assignments,
  6 assignments, 6 submissions, 3 settings. Enums stored as **names** (`Admin`, `Published`), as
  intended.
- Over HTTP against the running API:
  - `GET /health` → 200
  - login as admin → JWT issued, `GET /api/auth/me` returns the right identity
  - `GET /api/users` as admin → 200; as **anonymous** → 401
  - `GET /api/student/assignments` as admin → **403** (rule 1 holds)
  - student sees **5** published assignments and never the draft (rules 5 and 9 hold)
  - student's own submissions only: 2 rows (rule 4 holds)
  - teacher sees only their own 4 assignments with correct submission/graded counts (rule 3)
- Snag: `dotnet run` failed to rebuild because two `Ams.Api` processes left over from the previous
  session held locks on the output DLLs. The verification was still valid — those processes started
  at 15:42, *after* the last real source edit at 15:30, so they were running current code. Killed
  them afterwards to free the locks.

### 01:50 — Test suite re-run
- `dotnet test` → **112 passed, 0 failed, 0 skipped** (11s). Matches the number claimed in the README.

### 01:55 — README written
The last mandated deliverable with nothing on disk. Covers overview, features by role, stack,
project structure, data model, all 16 business rules, a full API table with the roles that may
call each route, quick start via Docker, the no-Docker path, database setup, how to run the tests,
assumptions, and known limitations.

Two decisions recorded there rather than left implicit:
- **Correlation-id header** (PLAN §5) was dropped, not forgotten. Every `ProblemDetails` already
  carries a `traceId`; a second identifier would be ceremony. Listed under known limitations so the
  omission reads as a choice.
- The seed data section explains *why* the fixtures are shaped the way they are — draft and
  published, deadlines past and future, all four submission statuses — so signing in as the demo
  student immediately shows a locked assignment, an open one, and graded work with feedback.

### 02:05 — Git history created
The repo had **zero commits** through two full days of work; everything lived in an untracked
working tree, one bad `rm` from gone. Closed that now.

- Secret scan first: `.env` is git-ignored and stays out. The only credential-shaped strings in
  tracked files are the local-dev Postgres password (which matches `.env.example` and is meant to
  be public) and `"Key": ""`, an empty placeholder. No high-entropy strings, no `node_modules`,
  `bin/`, `obj/` or `.next/` in the tracked set — 150 files total.
- Seven commits, layered by concern rather than one dump: scaffolding, plan, backend, tests,
  frontend, db seed, docs. Each message says *why*, not just what.

### 02:15 — `docker compose up --build` was broken

Worth writing down, because it is the single command the README leads with and the first thing
the evaluator will run.

- `docker compose build` failed on the `web` image at `RUN npm ci`.
- Cause: `package-lock.json` had drifted out of sync with `package.json` — a transitive `ajv`
  conflict (`lock file's ajv@6.15.0 does not satisfy ajv@8.20.0`, plus several missing entries).
  `npm ci` refuses a lockfile that does not match, by design; `npm run build` on the host had
  been passing the whole time because the existing `node_modules` was already correct. So the
  local build was green while the containerised build was broken — exactly the kind of gap that
  only a real `docker compose build` finds.
- Fix: `npm install` to regenerate the lockfile (95 insertions, 16 deletions), then confirmed
  `npm ci --dry-run` resolves cleanly, then rebuilt.
- Lesson: "the frontend builds" and "the frontend builds in the image" are different claims.

Also caught while verifying: the earlier build ran through `| tail -40`, so the pipeline reported
the exit status of `tail`, not of `docker compose`. It looked like a pass. Redirected to a log
file and checked `$?` directly instead.

### 02:20 — `middleware.ts` → `proxy.ts`
Next 16 renamed the file convention. Renamed the file and its export, and updated the two places
in the README that named the old path. The rebuild came through warning-free, confirming it.

### 02:35 — Full stack smoke-tested
`docker compose up -d` → `ams-db` healthy, `ams-api` and `ams-web` up. Then, against the containers:

- `GET :8080/health` → 200; `/swagger/index.html` → 200
- `POST :3000/api/auth/login` through the **web** container → 200, and the response body carries
  **only the user object**. The tokens come back as two `HttpOnly; Secure; SameSite=lax` cookies,
  which is the whole point of the proxy design — confirmed working in the built image, not just
  in dev.
- `GET :3000/api/proxy/student/assignments` with those cookies → the student's 5 published
  assignments. So the browser → Next proxy → API → Postgres path works end to end.

One caveat found and documented rather than "fixed": because `NODE_ENV=production` in the image,
the session cookies carry `Secure`. That is correct, and `http://localhost:3000` works because
browsers treat localhost as a secure context — but reaching the stack by LAN IP over plain HTTP
would silently drop the cookies and make login look like a no-op. Weakening the flag to make that
case work would be the wrong trade, so it went into the README's known limitations instead.

### 02:50 — Fresh-clone verification
The check that catches "it only works because of a file that was never committed."

- Cloned the repo into a temp directory. 154 tracked files on both sides, no `.env`, and the EF
  migrations are present. (Brief scare here: I looked for them under
  `Ams.Infrastructure/Migrations/` and found nothing — they actually live under
  `Ams.Infrastructure/Persistence/Migrations/`. Tracked all along.)
- `dotnet test` **from the clone** → 112 passed. So the committed tree is self-sufficient; nothing
  the build needs was sitting untracked on this machine.
- Also restored `db/seed.sql` into a scratch database to check the no-EF path the README offers the
  evaluator: 7 users, 6 assignments, 6 submissions, and the `__EFMigrationsHistory` row — so EF
  recognises the restored schema as current instead of trying to re-apply `InitialCreate`, exactly
  as `db/README.md` claims. Scratch database dropped afterwards.

### 02:35 — Guard re-verified after the rename

Renaming the route guard is exactly the kind of change that compiles, builds, boots and
silently protects nothing: if Next stops recognising the file or the export, every route
simply stops being guarded. The build cannot tell you that. So the full matrix was re-run
against the rebuilt server — 3 roles × 5 paths, plus anonymous:

| | `/` | `/student` | `/teacher` | `/admin` | `/login` |
|---|---|---|---|---|---|
| anonymous | → `/login` | → `/login?next=` | → `/login?next=` | → `/login?next=` | 200 |
| student | → `/student` | **200** | → `/student` | → `/student` | → `/student` |
| teacher | → `/teacher` | → `/teacher` | **200** | → `/teacher` | → `/teacher` |
| admin | → `/admin` | → `/admin` | → `/admin` | **200** | → `/admin` |

All 20 cells correct. The `next=` parameter round-trips, so a deep link survives login.

### 02:40 — Full stack verified in Docker

`docker compose up -d --build` from clean: all three containers running, and the whole
chain exercised through the published ports rather than the host toolchain.

- `ams-db`, `ams-api`, `ams-web` all running; API `/health` 200; Swagger UI 200.
- The redirect matrix above re-passes against the containerised web service.
- Login through the `web` container sets both httpOnly cookies, and `/api/proxy/*` reaches
  the API over the compose network (`web` → `api:8080`) — confirming the runtime
  `API_BASE_URL` wiring, not just the host default.
- Authorization holds across the container boundary: student → `/api/proxy/users` is **403**,
  admin → the same route is **200**, teacher → `/api/proxy/assignments` is **200**.

One diagnostic worth recording: `docker compose ps` rendered the `web` row as bare
`3000/tcp` with no host mapping, which looks exactly like an unpublished port. It was a
display artifact — `docker compose config` shows the ingress mapping, and port 3000 is held
by `wslrelay`/`com.docker.backend`, i.e. Docker itself. Checked before believing it, because
the alternative explanation (a leftover host `next start` answering the probes) would have
made every result above meaningless.

### Session close — where it stands

Backend, frontend, tests, docs and Docker are all done and verified. The remaining work is
delivery only: fresh-clone smoke test, push to GitHub, submit. Nothing is off this machine yet.

---

# Session 4 — 13 August 2026: from a generic system to one real school

The system worked. It was also generic: "Class 10-A", "Mathematics", `teacher@school.test`.
Every screen was the shape a marking rubric expects and nothing an actual school would
recognise. This session replaced the placeholder domain with a real one — গাজীপুর ক্যান্টনমেন্ট
বোর্ড উচ্চ বিদ্যালয়, Bangla version, ষষ্ঠ–অষ্টম শ্রেণি — and added the features that school needs.

Scope came from `enhancement.md`. Everything below either implements it or explains a decision
it left open.

### Research before schema

The temptation was to invent plausible-looking Bangla data. That would have been the same
generic system in a different alphabet, and anyone who went to school in Bangladesh would spot
it immediately. So the curriculum was researched first and the schema followed:

- NCTB's 2026 book lists for classes 6, 7 and 8, then **42 textbook PDFs** (~1 GB) downloaded,
  one per (class, subject).
- The books draw Bangla as vector outlines, so `get_text()` returns mojibake. Each সূচিপত্র was
  located by scoring pages 3–14 for long horizontal rules — a ruled contents table scores far
  above prose — and read from the rendered page image. Four unruled ones by hand.
- Two **signed board documents** (09-02-2025) settled the numbers: the subject/period structure
  and the per-subject question types. The second is what makes `AssignmentType` real rather than
  invented — it names অ্যাসাইনমেন্ট as a graded component and lists exactly which question types
  each subject sets.

One finding changed the whole premise: Bangladesh introduced a competency-based curriculum in
2023 and then **reverted to the 2012 curriculum**. That is printed inside the 2026 books
themselves. So the subject list in `enhancement.md` is current, not stale — worth confirming
before building fourteen subjects on top of it.

All of it is written up in `docs/RESEARCH.md` with sources, including the two places where the
app deliberately departs from NCTB (six teaching days instead of five; four optional-group
subjects instead of one) and why.

### The schema change that carried the rest

`TeacherAssignment` — "this teacher may teach this (class, subject)" — became **`Course`**.

The grant table only ever answered one question, and answered it as a permission. A course
answers four: it is the same gate on assignment creation, *and* the row the routine schedules,
*and* the unit a student's subject list is built from, *and* the thing that carries the code the
school already uses. `C06-109` is class 6, board subject 109, গণিত. The code is derived from the
class level and the board code, never typed.

Around it:

- **`SubjectAssignmentType`** — the NCTB question types each subject may set. An empty list fails
  closed: a misconfigured subject refuses everything rather than allowing everything.
- **`FaithGroup`** on the student. ধর্ম ও নৈতিক শিক্ষা is not one subject everyone shares — it is
  one course per stream, taught in parallel in the same period. That single field decides which
  course a student sees, what they may submit to, and the denominator the teacher is shown:
  "১৮ / ২৭ জমা দিয়েছে", not out of thirty.
- **`RoutinePeriod`** — six days × six periods. `WeekDay` is deliberately not `System.DayOfWeek`:
  that type starts on Sunday and includes Friday, which would let a row be written for a day the
  school is closed.
- **Attachments** on both assignments and submissions, behind `IFileStorage` so no service
  touches a disk.
- **`AssignmentComment`** — the class conversation, Google Classroom's shape.
- **Roll numbers** on enrolment, unique per class.

### Two rules that no index can express

Worth calling out because they are the reason the routine is a service and not a CRUD table:

1. **A teacher cannot be in two classrooms in the same period.** A unique index can stop a *class*
   being double-booked, because that is one class's rows. Teacher clash spans two classes' rows,
   so it is a query in `SetRoutinePeriodAsync`.
2. **A slot holds one course — except religion.** The Muslim and Hindu halves of a class take
   ধর্ম ও নৈতিক শিক্ষা at the same time in different rooms. So the index is on
   (class, day, period, course), and the service enforces the narrower rule: more than one course
   in a slot only when every course in it belongs to a different faith group. Anything else
   replaces what was there, which is what an admin means by "set this period to গণিত".

Both have tests. So does the case that makes the second one honest: setting গণিত onto a slot that
holds both religion courses clears both.

### Tests: 112 → 164

The suite had to be re-fixtured, not just re-run: `TeacherOne`/`ClassA`/`Alice` no longer
described anything. `TestWorld` is now a small slice of the real school — ষষ্ঠ and সপ্তম শ্রেণি,
রেজাউল teaching গণিত to both, আফসার teaching বাংলা and deliberately *not* গণিত, and one Hindu
student in a class of Muslims. Every authorisation test turns on one of those facts, so a test
that fails now says something about the school rather than about a letter of the alphabet.

New coverage: per-subject type restriction, faith-group visibility (both directions), routine
scheduling, upload rules (extension, size, count, and "removing the last file from a text-less
answer"), roll-ordered marking lists, and the expected-submission denominator.

One test had to be dropped rather than translated: "creating an assignment with a past deadline
is refused". It no longer is — a draft may be parked with any deadline while it is being written,
and only *publishing* requires a future one. The replacement asserts exactly that, plus the new
default-window behaviour: omit the deadline and the server applies the school's seven-day rule.

### Verified against a real database, not just SQLite

`dotnet run` against a fresh Postgres database — EF created it, applied the migration, seeded it —
then every surface exercised over HTTP:

- 3 classes × 30 students, 14 subjects, **42 courses, none unstaffed**, 90 enrolments
- Class 6's routine: **36/36 slots filled, 39 entries** — the extra three are the parallel
  religion periods
- রেজাউল: 6 courses, 20 routine slots, 14 assignments, and **no other teacher's work in his list**
- A student: 13 courses (not 14 — one religion course is not theirs), 26 assignments, 15 submissions
- `c6r28`, a Hindu student: has `C06-112`, does not have `C06-111`
- student → `/api/users` 403, teacher → `/api/users` 403, student → `/api/assignments` 403
- admin posting a comment → **403**, by design: an admin observes the school, they are not in the
  lesson

### The frontend, rebuilt

Not translated — rebuilt. `enhancement.md` asked for something a school would use: *"dark ui? ew.
school should be white and add some colors. make it cheerful and lightweight"*.

- **Light only**, with `color-scheme: light` pinned. Without that, a Windows machine in dark mode
  renders the form controls dark against light surfaces.
- **Subject colours keyed by board code**, so a subject keeps its colour on every screen and a
  fourteen-subject stream is scannable without reading. The card carries the colour as a spine;
  the filter chips are the same chips.
- **Set in Kalpurush**, self-hosted (SIL OFL 1.1, 112 KB woff2) with `local()` tried first and the
  device Bengali faces behind it. A Latin-first stack pushes যুক্তাক্ষর through a fallback face with
  the wrong metrics, and বিজ্ঞান and গণিত end up on different baselines in the same table row.
  `size-adjust: 108%` compensates for Kalpurush's small x-height, which also lands its line box on
  the body's 1.7 leading. Line height is raised for the মাত্রা and the below-line vowel signs.
- **Bangla numerals everywhere** — `Intl` handles dates, but marks, rolls and counts are
  interpolated into strings all over the UI, and a page that mixes ২০ and 20 reads as a bug.
- **Labels come from the API.** "সৃজনশীল প্রশ্ন" names an NCTB question type, so it lives with the
  enum. `GET /api/reference` is fetched once in the server layout and handed down through a
  provider, so every badge resolves a label synchronously with no loading state.
- The assignment form's type list **narrows to the chosen course's subject** — the same rule the
  API enforces, surfaced rather than duplicated.

### The proxy could not carry a file

The Next proxy forced `Content-Type: application/json` and read every body as text. That was fine
for a text-only app and fatal for this one: it corrupts a multipart boundary and mangles a PDF on
the way back.

Rewritten to forward the request's own content-type, buffer the body as bytes (the 401-refresh
retry has to replay it, and a stream reads once), and pass `content-type` /
`content-disposition` back so a download saves with its real name. Downloads cannot be a plain
`<a href>` either — the bearer token is in an httpOnly cookie only the proxy can exchange — so
they go through `fetch` → blob → synthetic anchor.

### Verifying the built frontend, and one honest false alarm

Production build clean, 24 routes. Then a scripted click-through against the real API:

- Route guard: 3 roles × 5 paths, all 20 cells correct; anonymous bounced to `/login`
- Every page in each role's area renders 200 with the shell
- Login response body carries **only the user object** — both tokens are httpOnly cookies
- **Upload → download → grade → student sees the mark**: a student submitted a PNG through the
  proxy, the owning teacher downloaded it and got **byte-identical** content, graded it 8/10 with
  feedback, and the student's view showed the mark and `canEdit: false`

Two diagnostics worth recording, because both looked like bugs:

- Every proxied call returned 401 while login returned 200. Cause: `NODE_ENV=production` makes
  the session cookies `Secure`, and Python's cookiejar — unlike a browser, which exempts
  localhost — will not replay a `Secure` cookie over plain HTTP. The test harness was wrong, not
  the app. Fixed by carrying the cookies explicitly, with a comment saying why.
- A teacher download returned 403 and briefly looked like a regression. It was correct: that
  assignment belonged to চারু ও কারুকলা, which রেজাউল does not teach. Re-run against one of his
  own courses, it returned the bytes. The 403 was the rule working.

### Delivery items

- **`db/seed.sql` regenerated** from a pristine seeded database — 14 tables, 42 courses, 117
  routine rows, 1,283 submissions. Written without a BOM: PowerShell's `Out-File -Encoding utf8`
  adds one, and a BOM at the top of a `psql` script is a syntax error waiting to happen.
- **Compose gained an uploads volume.** The seeder writes real files through `IFileStorage`, but
  nothing mounted `/app/uploads`. `docker compose down && up` (without `-v`) would have kept the
  database and thrown the files away, leaving attachment rows pointing at nothing and every
  download 404ing. The entity comment had claimed a volume was mounted; now one is.
- `db/README.md` documents the two things a SQL dump cannot carry: the attachment bytes, and
  compatibility with a psql older than the `\restrict` directives pg_dump 16.10+ emits.
- README rewritten around the school, the curriculum, the new rules (33 now) and the new routes.
- `PLAN.md` §11 records what the revision supersedes rather than quietly editing the old plan.

### The compose run that paid for itself

Adding the uploads volume was supposed to be a one-line safety fix. Running `docker compose up`
from clean turned it into the most useful ten minutes of the session.

The stack came up green — API healthy, Swagger 200, web 200, login working for all three roles.
Then the click-through died on an empty student feed, and the counts explained why:

```
users: 104   classes: 3   courses: 42   enrollments: 90
assignments: 0   submissions: 0
```

Two faults, stacked:

1. **The volume arrived owned by root.** The runtime image runs as `$APP_UID` (non-root), and
   Docker creates a fresh named volume owned by root. The seeder's first worksheet write hit
   `UnauthorizedAccessException: Access to the path '/app/uploads/assignments' is denied` — a
   failure introduced by the very change meant to protect those files. Fixed by creating the
   directory *while still root* and chowning it to `$APP_UID` before the `USER` switch; Docker
   seeds a new volume from the image, ownership included.

2. **The seed was not atomic, and the idempotency check papered over it.** Seeding writes users
   first and asks "any users?" to decide whether to run at all. So the crash committed 104 users,
   42 courses and the whole routine, then every restart logged *"Database already seeded —
   skipping"* over a database with no assignments in it. That is a worse bug than the permission
   error: it is silent, it survives restarts, and nothing about the running app looks wrong until
   somebody logs in as a student. Fixed by wrapping the seed in one transaction, so a failure
   rolls back and the check stays honest. (Files written before the rollback can orphan; harmless,
   and noted in the code.)

Neither fault could have been found on the host, where the process owns its own directory.

Re-verified after both fixes: `down -v`, rebuild, **seed completes — 13 teachers, 90 students,
42 courses, 98 assignments**; a seeded worksheet downloads through the API; then `down` and `up`
again with volumes kept, and the same file comes back **byte-identical** with the database
reporting "already seeded". Which is what the volume was for in the first place.

The whole browser click-through then re-ran green against the containers: route guard 20/20,
every page rendering, and the upload → download → grade → student-sees-the-mark loop.

### Where it stands

Backend, frontend, tests, seed data, Docker and docs are done and verified, in nine commits
layered by concern. Remaining: push and submit.

---

# Session 5 — 14 August 2026: the routine said one thing and drew another

Reported from a screenshot of the running app: the routine grid printed **টিফিন ১১:৩০** under the
header of the fourth period, and then rendered six days of classes in the column beneath it. The
schedule was correct — tiffin genuinely started when the fourth period ended — but the grid put
the label and the classes in the same column, so it read as though lessons ran through the break.

That is a design fault, not a data fault, and it had a second half: the break was in the wrong
place for this school. Three classes, tiffin, three classes is what the bell actually rings, and
the day ends at 14:00.

### The bell times moved

`PeriodSchedule` now splits the day evenly and closes on 14:00:

| | before | after |
|---|---|---|
| break after | period 4 | **period 3** |
| টিফিন | 11:30–12:05 (35 min) | **10:40–11:30 (50 min)** |
| ৪র্থ–৬ষ্ঠ | 12:05 → 13:45 | **11:30 → 14:00** |

The 50 minutes are not a preference, they are what is left: six periods of the NCTB-mandated
length (60 + 5×50) between 08:00 and 14:00 leave exactly that in the middle. So the break got
*longer* than the sheet's 35 minutes rather than shorter — recorded in `docs/RESEARCH.md` §4 as
the third deliberate deviation from the board document, with the arithmetic, since the other two
were already documented and an undocumented third would look like a mistake.

Nothing in the database moved. `RoutinePeriod` stores a period *index*; the clock lives in code,
which is exactly why this was a constant change and not a migration.

### The break became a column

The fix for the grid is structural rather than typographic. Tiffin is now its own column between
the third and fourth periods, and its body is a single `<td rowSpan={6}>` spanning every day —
one cell, because the break belongs to the school day and not to any row of it. A column with no
slot cells in it cannot show a class during the break; the old caption could only ask you to
believe it wouldn't.

It also fixes the admin editor for free: the break column contains no slot buttons, so there is
nothing there to click and no way to place a course into the gap.

Extracted to `components/routine-break.tsx` and used by both the class grid and the teacher's own
week, so the two cannot drift into disagreeing about when the school stops. The teacher's grid
had never drawn the break at all.

### While in there: the period labels were not Bangla

Both grids built their headers as `` `${bn(index)}ম পিরিয়ড` ``, which is right for ১ম and ৫ম and
wrong for the other four — "৪ম" is not a word. Bangla ordinals are irregular over exactly the
1–6 range a routine needs, so `periodLabel()` now carries the six of them: ১ম, ২য়, ৩য়, ৪র্থ, ৫ম,
৬ষ্ঠ. Four call sites, including the editor's modal title.

### Verified

165 tests pass (was 164). The new one is `No_period_runs_through_the_tiffin_break`, which asserts
the property the grid now draws rather than the specific numbers — it would have failed against
the old schedule for the right reason, and it keeps failing if a future edit walks a period back
over the break.

Then the containers were rebuilt and all three routine pages driven in a real browser. The header
row reads ১ম · ২য় · ৩য় · **টিফিন ১০:৪০–১১:৩০** · ৪র্থ · ৫ম · ৬ষ্ঠ, the break cell reports
`rowSpan=6`, and the first row has 8 cells to the other five rows' 7 — which is the rowSpan doing
its job.

---

# Session 6 — 14 August 2026: audit, security review, and what was still only on this machine

A deliberate pass over finished work rather than new features: is it actually done, does it hold
up to a security read, and is there anything that would only surface for somebody else running it.

### What came back clean

Worth listing, because "I checked and found nothing" is a result and an unrecorded check is
indistinguishable from one that never happened.

- **Tests** — 165 passing, 0 failed, 0 skipped, before any change today.
- **Frontend production build** — clean, 24 routes, TypeScript passes, with the session-5 routine
  and typography work in the tree.
- **Dependencies** — `npm audit --omit=dev` reports 0 vulnerabilities;
  `dotnet list package --vulnerable --include-transitive` reports none across all five projects.
- **Secrets in tracked files** — the only credential-shaped strings are the three demo passwords
  printed on the login page on purpose, and `"Key": ""`, an empty placeholder. `.env` is ignored
  and untracked; only `.env.example` ships.
- **Authorization** — every one of the eight controllers carries a class-level `[Authorize]`, and
  the services re-derive the caller's rights from `ICurrentUser` rather than trusting the request
  body. The attribute is the first gate, not the only one.
- **Injection** — no `FromSqlRaw`/`ExecuteSqlRaw` anywhere; no `dangerouslySetInnerHTML`,
  `innerHTML` or `eval` in the frontend.
- **Path traversal** — storage keys are server-generated GUIDs and `ResolveWithinRoot` re-checks
  the resolved path against the root anyway.
- **Passwords and sessions** — PBKDF2-HMAC-SHA256 at 210,000 iterations, fresh 128-bit salt,
  fixed-time comparison. Refresh tokens are 64 random bytes stored SHA-256 hashed, rotated on use,
  and revoked on password change and on deactivation. Login returns an identical failure for an
  unknown address and a wrong password.

One near-miss that turned out fine on inspection: `CurrentUser.Role` falls back to `default` when
the role claim will not parse. `UserRole` starts at `Admin = 1`, so `default` is `0` — not a valid
role, and matched by no `[Authorize(Roles = ...)]`. Had the enum started at zero, a malformed token
would have resolved to Admin. It reads as luck; it is worth knowing it holds.

### The one that mattered: compose ran the API in Development

`ExceptionHandlingMiddleware` is the only thing in the app that branches on the environment:

```csharp
environment.IsDevelopment() ? exception.ToString() : "Please try again later."
```

and `docker-compose.yml` set `ASPNETCORE_ENVIRONMENT: Development`. Confirmed against the running
container, not just read off the file — `docker exec ams-api printenv ASPNETCORE_ENVIRONMENT` →
`Development`. So the single command the README leads with, the one an evaluator runs, would have
returned full .NET stack traces — type names, source paths, line numbers, and whatever an Npgsql
exception message happens to carry — to anyone who could provoke a 500.

Nothing needed that mode. Swagger is registered unconditionally and so is the migrate-and-seed on
boot; the environment name was doing no work except widening the error body. Set to `Production`.

### Three response headers, and where they had to be registered

`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` on the
API, and the same three plus `frame-ancestors 'none'` on the web tier.

`nosniff` is the one with a concrete job here. An attachment's `Content-Type` is whatever the
uploading browser claimed — fully caller-controlled — and it is stored and echoed back on
download. `File(stream, contentType, fileName)` already sends `Content-Disposition: attachment`, so
the bytes are saved rather than rendered, and `nosniff` is what stops a browser second-guessing
that from the bytes themselves.

The placement is the interesting part. Written the obvious way — set the headers on the way in —
they would be **silently dropped from exactly the responses that most need them**, because
`ExceptionHandlingMiddleware` calls `Response.Clear()` before writing ProblemDetails, and `Clear()`
clears headers too. So they go on through an `OnStarting` callback, which runs at flush time, after
any clear. There is a test for that specific case; without it the middleware would look correct and
be wrong on every 500.

### Two findings left undone on purpose

- **Login is not rate-limited.** Already in the README's known limitations, but the entry was too
  short to be useful, because the obvious fix is actively harmful here. Every browser request
  reaches the API through the Next proxy, so the API sees **one source address for the whole
  school**. An IP-partitioned limiter would bucket all six hundred users together — a brute-force
  defence that is a self-inflicted outage. Doing it properly means `UseForwardedHeaders` with the
  proxy trusted, or a per-account failed-attempt counter and lockout, which is a schema change and
  a migration. Left undone, with the reasoning written down rather than the gap left looking like
  an oversight.
- **No `script-src` CSP.** The web tier sends `frame-ancestors 'none'`, `nosniff` and
  `no-referrer`. A real `script-src` needs per-request nonces threaded through Next's inline
  bootstrap scripts; the shortcut that avoids that is `'unsafe-inline'`, which would assert a
  protection it does not provide.

Swagger is served unconditionally, including in Production. For a project whose README tells the
evaluator to click through it, that is the point rather than an oversight — noted here so it reads
as a decision.

### Tests: 165 → 169

Both fixes became assertions rather than one-line config changes nobody would notice regressing.
`Api/ResponsePipelineTests` drives the two middlewares through a real host pipeline and covers:
Production leaks no internals (asserted against a planted fake password in the exception message),
Development still shows the detail, the headers are present on a normal response, and the headers
survive the error handler's `Response.Clear()`.

Driven through `WebApplication.CreateSlimBuilder` + `UseTestServer` rather than a
`DefaultHttpContext`, and that is not ceremony: `DefaultHttpContext`'s response feature **never
fires `OnStarting`**, so the header assertions would have passed while testing nothing.

### The Swagger page was still advertising accounts that no longer exist

Found by accident, which is the only way this one was ever going to be found: the smoke test after
the rebuild failed to log in, and for a moment that looked like the `Production` switch having
broken authentication. It had not — the test was typing `admin@school.test`, and session 4 had
replaced every account with the real school's (`admin@gcbhs.edu.bd`). The 403 was the rule working.

But the credentials had come from somewhere, and that somewhere was `Program.cs`: the Swagger
landing page still read *"Demo accounts: `admin@school.test` / `teacher@school.test` /
`student@school.test`"*. The README was correct and had been updated; the API's own front page was
eight months of school out of date. Since the README explicitly sends the evaluator to Swagger to
click through the API, the first thing they would have done is copy three dead addresses out of it,
get a 403, and reasonably conclude that login was broken.

Nothing automated could have caught this — it is a string in a description field, and no test
asserts on marketing copy. Swept the rest of the repo for the retired domain: the only other live
occurrences are the unit-test fixtures, which are self-contained and mean nothing outside their own
in-memory database, and `PLAN.md`, where the old table is the historical record that §11
supersedes. Both correct as they stand.

### The finding that had nothing to do with security

The audit's most valuable result was a delivery one. All of session 5 was still an untracked
working tree, and inside it:

- `frontend/public/fonts/kalpurush.woff2` and `OFL.txt` were untracked, and
  `git show HEAD:frontend/src/app/globals.css` has **no `@font-face` rule at all**. The typography
  work — the self-hosted font, the `local()`-first `src`, the `size-adjust: 108%` — existed only
  on this machine. A clone would have fallen through to device fonts and looked subtly wrong with
  nothing to explain why.
- The repo has **no git remote**. Nine commits, none of them anywhere but here, on the day of the
  deadline.

Neither is a bug in the code. Both would have made the code irrelevant.
