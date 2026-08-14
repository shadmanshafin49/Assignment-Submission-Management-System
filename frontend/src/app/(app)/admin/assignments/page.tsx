"use client";

import { PageHeader } from "@/components/app-shell";
import { AssignmentStatusBadge, Badge, SubjectChip } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Input, Select } from "@/components/ui/field";
import { useAllClasses, useAllSubjects, useUsersByRole } from "@/hooks/use-admin";
import { useAssignments } from "@/hooks/use-teacher";
import { useLabels } from "@/lib/reference";
import type { AssignmentDto, AssignmentStatus } from "@/lib/types";
import { bn, formatDateTime, formatRelative } from "@/lib/utils";
import { Search } from "lucide-react";
import { useState } from "react";

/**
 * School-wide oversight of every teacher's work. Read-only by design: an admin
 * runs the school, but setting and marking work belongs to the teacher who
 * takes the course.
 */
export default function AdminAssignmentsPage() {
  const labels = useLabels();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<AssignmentStatus | "">("");
  const [classRoomId, setClassRoomId] = useState("");
  const [subjectId, setSubjectId] = useState("");
  const [teacherId, setTeacherId] = useState("");
  const [search, setSearch] = useState("");

  const classes = useAllClasses();
  const subjects = useAllSubjects();
  const teachers = useUsersByRole("Teacher");

  const { data, isPending, error, refetch } = useAssignments({
    page,
    status,
    classRoomId: classRoomId || undefined,
    subjectId: subjectId || undefined,
    teacherId: teacherId || undefined,
    search: search.trim() || undefined,
  });

  const columns: Column<AssignmentDto>[] = [
    {
      id: "title",
      header: "অ্যাসাইনমেন্ট",
      primary: true,
      cell: (row) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-medium">{row.title}</span>
          <span className="text-xs text-muted">
            {labels.assignmentType(row.type)}
            {row.chapterOrLesson && ` · ${row.chapterOrLesson}`}
          </span>
        </div>
      ),
    },
    {
      id: "course",
      header: "কোর্স",
      cell: (row) => (
        <div className="flex flex-col items-start gap-1">
          <SubjectChip code={row.courseCode} name={row.subjectName} />
          <span className="text-xs text-muted">
            {row.classRoomName} · {row.courseCode}
          </span>
        </div>
      ),
    },
    {
      id: "teacher",
      header: "শিক্ষক",
      cell: (row) => (
        <span className="text-muted">{row.createdByTeacherName}</span>
      ),
    },
    {
      id: "deadline",
      header: "সময়সীমা",
      hideOnMobile: true,
      cell: (row) => (
        <span title={formatDateTime(row.deadline)}>
          {formatRelative(row.deadline)}
        </span>
      ),
    },
    {
      id: "status",
      header: "অবস্থা",
      cell: (row) => <AssignmentStatusBadge status={row.status} />,
    },
    {
      id: "submissions",
      header: "জমা",
      align: "right",
      cell: (row) => (
        <Badge tone={row.submissionCount > 0 ? "info" : "neutral"}>
          {bn(row.gradedCount)}/{bn(row.submissionCount)} মূল্যায়িত
        </Badge>
      ),
    },
  ];

  function reset<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value);
      setPage(1);
    };
  }

  return (
    <>
      <PageHeader
        title="অ্যাসাইনমেন্ট"
        description="সব শিক্ষকের দেওয়া কাজ — কেবল পর্যবেক্ষণের জন্য।"
      />

      <Card>
        <div className="grid gap-3 border-b border-border p-3 sm:grid-cols-2 lg:grid-cols-5">
          <Select
            aria-label="অবস্থা"
            value={status}
            onChange={(e) =>
              reset(setStatus)(e.target.value as AssignmentStatus | "")
            }
          >
            <option value="">সব অবস্থা</option>
            {labels.options.assignmentStatuses.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </Select>

          <Select
            aria-label="শ্রেণি"
            value={classRoomId}
            onChange={(e) => reset(setClassRoomId)(e.target.value)}
          >
            <option value="">সব শ্রেণি</option>
            {classes.data?.items.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>

          <Select
            aria-label="বিষয়"
            value={subjectId}
            onChange={(e) => reset(setSubjectId)(e.target.value)}
          >
            <option value="">সব বিষয়</option>
            {subjects.data?.items.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </Select>

          <Select
            aria-label="শিক্ষক"
            value={teacherId}
            onChange={(e) => reset(setTeacherId)(e.target.value)}
          >
            <option value="">সব শিক্ষক</option>
            {teachers.data?.items.map((t) => (
              <option key={t.id} value={t.id}>
                {t.fullName}
              </option>
            ))}
          </Select>

          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" />
            <Input
              value={search}
              onChange={(e) => reset(setSearch)(e.target.value)}
              placeholder="খুঁজুন…"
              aria-label="অ্যাসাইনমেন্ট খুঁজুন"
              className="pl-9"
            />
          </div>
        </div>

        <DataTable
          columns={columns}
          rows={data?.items}
          keyOf={(row) => row.id}
          loading={isPending}
          error={error}
          onRetry={refetch}
          emptyTitle="কোনো অ্যাসাইনমেন্ট নেই"
          emptyDescription="এই ছাঁকনিতে কিছু পাওয়া যায়নি।"
        />

        {data && data.totalPages > 1 && (
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            hasPrevious={data.hasPrevious}
            hasNext={data.hasNext}
            onPageChange={setPage}
          />
        )}
      </Card>
    </>
  );
}
