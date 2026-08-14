"use client";

import { PageHeader } from "@/components/app-shell";
import { GradeModal } from "@/components/grade-modal";
import { SubmissionStatusBadge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Select } from "@/components/ui/field";
import { useMyCourses, useSubmissions } from "@/hooks/use-teacher";
import { useLabels } from "@/lib/reference";
import type { SubmissionDto, SubmissionStatus } from "@/lib/types";
import { bn, formatDateTime } from "@/lib/utils";
import { Paperclip } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

export default function TeacherSubmissionsPage() {
  const labels = useLabels();
  const [status, setStatus] = useState<SubmissionStatus | "">("");
  const [courseId, setCourseId] = useState("");
  const [page, setPage] = useState(1);
  const [grading, setGrading] = useState<SubmissionDto | null>(null);

  const { data: courses } = useMyCourses();
  const { data, isPending, error, refetch } = useSubmissions({
    page,
    status,
    courseId: courseId || undefined,
  });

  const columns: Column<SubmissionDto>[] = [
    {
      id: "student",
      header: "শিক্ষার্থী",
      primary: true,
      cell: (row) => (
        <div className="flex items-center gap-2">
          <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-surface-muted text-xs font-semibold tabular-nums">
            {bn(row.rollNumber)}
          </span>
          <div className="flex min-w-0 flex-col">
            <span className="truncate font-medium">{row.studentName}</span>
            <span className="truncate text-xs text-muted">
              {row.studentEmail}
            </span>
          </div>
        </div>
      ),
    },
    {
      id: "assignment",
      header: "অ্যাসাইনমেন্ট",
      cell: (row) => (
        <Link
          href={`/teacher/assignments/${row.assignmentId}`}
          className="text-primary hover:underline"
        >
          {row.assignmentTitle}
        </Link>
      ),
    },
    {
      id: "status",
      header: "অবস্থা",
      cell: (row) => <SubmissionStatusBadge status={row.status} />,
    },
    {
      id: "submitted",
      header: "জমার সময়",
      hideOnMobile: true,
      cell: (row) => (
        <span className="text-muted">{formatDateTime(row.submittedAt)}</span>
      ),
    },
    {
      id: "files",
      header: "ফাইল",
      hideOnMobile: true,
      cell: (row) =>
        row.attachments.length > 0 ? (
          <span className="inline-flex items-center gap-1 text-muted">
            <Paperclip className="size-3.5" />
            {bn(row.attachments.length)}
          </span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      id: "marks",
      header: "নম্বর",
      align: "right",
      cell: (row) =>
        row.marks != null ? (
          <span className="font-medium tabular-nums">
            {bn(row.marks)}
            <span className="text-muted">/{bn(row.maxMarks)}</span>
          </span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      id: "actions",
      header: "",
      align: "right",
      cell: (row) => (
        <Button size="sm" variant="secondary" onClick={() => setGrading(row)}>
          {row.status === "Graded" ? "নম্বর সংশোধন" : "মূল্যায়ন"}
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="জমা ও মূল্যায়ন"
        description="আপনার সব অ্যাসাইনমেন্টে আসা জমা এক জায়গায়।"
      />

      <Card>
        <div className="flex flex-wrap items-center gap-3 border-b border-border p-3">
          <Select
            aria-label="অবস্থা অনুযায়ী ছাঁকুন"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as SubmissionStatus | "");
              setPage(1);
            }}
            className="w-full sm:w-56"
          >
            <option value="">সব অবস্থা</option>
            {labels.options.submissionStatuses.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>

          <Select
            aria-label="কোর্স অনুযায়ী ছাঁকুন"
            value={courseId}
            onChange={(e) => {
              setCourseId(e.target.value);
              setPage(1);
            }}
            className="w-full sm:w-64"
          >
            <option value="">সব কোর্স</option>
            {courses?.map((course) => (
              <option key={course.id} value={course.id}>
                {course.code} — {course.classRoomName} · {course.subjectName}
              </option>
            ))}
          </Select>
        </div>

        <DataTable
          columns={columns}
          rows={data?.items}
          keyOf={(row) => row.id}
          loading={isPending}
          error={error}
          onRetry={refetch}
          emptyTitle="কোনো জমা নেই"
          emptyDescription="এই ছাঁকনিতে এখনো কিছু পড়েনি।"
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

      <GradeModal submission={grading} onClose={() => setGrading(null)} />
    </>
  );
}
