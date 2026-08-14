# Database

There are **two** ways to get a working database. You only need one.

## Option A — automatic (recommended, nothing to run)

The API applies EF Core migrations and seeds demo data on startup. So either of these
is already enough:

```bash
docker compose up -d          # from the repository root
```

```bash
dotnet run --project backend/src/Ams.Api    # with Postgres running
```

No tables need to be created by hand.

## Option B — restore the plain SQL dump

`seed.sql` is a `pg_dump` of the fully migrated and seeded database, for evaluating
the schema without running EF Core.

```bash
# against the compose database
docker exec -i ams-db psql -U ams -d ams < db/seed.sql

# or against any local Postgres
psql -U <user> -d <database> -f db/seed.sql
```

The dump is `--clean --if-exists`, so it drops existing objects first and can be
re-run safely. It includes the `__EFMigrationsHistory` row, so a later
`dotnet run` will recognise the schema as current instead of re-applying migrations.

Two things it cannot carry:

- **Attachment bytes.** The database stores only file metadata; the files themselves
  live under `backend/src/Ams.Api/uploads/` (or wherever `Storage:Root` points). A
  restored database therefore lists the seeded attachments but cannot serve them —
  the API returns 404 for a download until the file exists. Seeding through Option A
  writes both.
- **Old psql clients.** `pg_dump` 16.10 and later emit `\restrict` / `\unrestrict`
  around the dump. Restore it with a psql of the same major version — the command
  above goes through the container's own psql, which always matches.

## Migrations

Migration files live in
[backend/src/Ams.Infrastructure/Persistence/Migrations/](../backend/src/Ams.Infrastructure/Persistence/Migrations/).

```bash
# apply migrations manually
dotnet ef database update \
  --project backend/src/Ams.Infrastructure \
  --startup-project backend/src/Ams.Infrastructure

# add a new migration
dotnet ef migrations add <Name> \
  --project backend/src/Ams.Infrastructure \
  --startup-project backend/src/Ams.Infrastructure \
  --output-dir Persistence/Migrations
```

A `DesignTimeDbContextFactory` supplies the connection string to the EF tooling, so
these commands need no application configuration. Override the target database with
the `AMS_CONNECTION_STRING` environment variable.

## What gets seeded

A whole school year of ষষ্ঠ–অষ্টম শ্রেণি, not a handful of placeholder rows.

| Table | Rows | Notes |
|---|---|---|
| `users` | 104 | 1 প্রধান শিক্ষক, 13 teachers, 90 students (30 per class) |
| `class_rooms` | 3 | ষষ্ঠ, সপ্তম, অষ্টম শ্রেণি |
| `subjects` | 14 | the NCTB list, with board codes (১০১ বাংলা ১ম পত্র … ১৫৫ কর্ম ও জীবনমুখী) |
| `subject_assignment_types` | 61 | which question types each subject may set |
| `courses` | 42 | 3 classes × 14 subjects, each with its teacher — `C06-109` style codes |
| `enrollments` | 90 | roll ১–৩০ in each class |
| `routine_periods` | 117 | 36 periods × 3 classes, plus the parallel ধর্ম শিক্ষা halves |
| `assignments` | 98 | two weeks of real work: published and draft, deadlines past and future |
| `submissions` | 1,283 | every status — Submitted, Late, Graded, ReturnedForRevision |
| `assignment_comments` | 24 | student questions with the teacher's reply |
| `assignment_attachments` | 10 | worksheet metadata (see the caveat above) |
| `app_settings` | 16 | school details plus admin-tunable defaults |

Passwords are stored as PBKDF2-HMAC-SHA256 hashes (`iterations.salt.subkey`), never
in plain text — including in the dump.
