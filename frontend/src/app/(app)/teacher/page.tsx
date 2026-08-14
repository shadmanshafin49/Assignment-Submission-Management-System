"use client";

import { PageHeader } from "@/components/app-shell";
import { AssignmentFormModal } from "@/components/assignment-form";
import {
  AssignmentStatusBadge,
  Badge,
  SubjectChip,
} from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Input, Select } from "@/components/ui/field";
import { useAssignments, useMyCourses } from "@/hooks/use-teacher";
import { useLabels } from "@/lib/reference";
import type { AssignmentDto, AssignmentStatus } from "@/lib/types";
import { bn, cn, formatDateTime, formatRelative } from "@/lib/utils";
import { Plus, Search } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

const TABS: { label: string; value: AssignmentStatus | "" }[] = [
  { label: "সব", value: "" },
  { label: "খসড়া", value: "Draft" },
  { label: "প্রকাশিত", value: "Published" },
];

function useColumns(): Column<AssignmentDto>[] {
  const labels = useLabels();

  return [
    {
      id: "title",
      header: "অ্যাসাইনমেন্ট",
      primary: true,
      cell: (row) => (
        <div className="flex flex-col gap-0.5">
          <Link
            href={`/teacher/assignments/${row.id}`}
            className="font-medium text-primary hover:underline"
          >
            {row.title}
          </Link>
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
      id: "deadline",
      header: "সময়সীমা",
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
        // Three numbers a teacher actually needs: marked, received, and the
        // class size the work was set for.
        <Badge tone={row.submissionCount > 0 ? "info" : "neutral"}>
          {bn(row.gradedCount)}/{bn(row.submissionCount)} মূল্যায়িত ·{" "}
          {bn(row.expectedSubmissionCount)} জনের
        </Badge>
      ),
    },
  ];
}

export default function TeacherAssignmentsPage() {
  const [status, setStatus] = useState<AssignmentStatus | "">("");
  const [courseId, setCourseId] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);

  const columns = useColumns();
  const { data: courses } = useMyCourses();
  const { data, isPending, error, refetch } = useAssignments({
    page,
    status,
    courseId: courseId || undefined,
    search: search.trim() || undefined,
  });

  return (
    <>
      <PageHeader
        title="অ্যাসাইনমেন্ট"
        description="নিজের কোর্সের কাজ তৈরি, প্রকাশ ও মূল্যায়ন করুন।"
        action={
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            নতুন অ্যাসাইনমেন্ট
          </Button>
        }
      />

      <Card>
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border p-3">
          <div
            role="tablist"
            aria-label="অবস্থা অনুযায়ী ছাঁকুন"
            className="flex gap-1 rounded-lg bg-surface-muted p-1"
          >
            {TABS.map((tab) => (
              <button
                key={tab.label}
                role="tab"
                aria-selected={status === tab.value}
                onClick={() => {
                  setStatus(tab.value);
                  setPage(1);
                }}
                className={cn(
                  "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
                  status === tab.value
                    ? "bg-surface text-foreground shadow-sm"
                    : "text-muted hover:text-foreground",
                )}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <div className="flex flex-1 flex-wrap items-center justify-end gap-2">
            <Select
              aria-label="কোর্স অনুযায়ী ছাঁকুন"
              value={courseId}
              onChange={(e) => {
                setCourseId(e.target.value);
                setPage(1);
              }}
              className="w-full sm:w-56"
            >
              <option value="">সব কোর্স</option>
              {courses?.map((course) => (
                <option key={course.id} value={course.id}>
                  {course.code} — {course.subjectName}
                </option>
              ))}
            </Select>

            <div className="relative w-full sm:w-56">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" />
              <Input
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
                placeholder="অ্যাসাইনমেন্ট খুঁজুন…"
                aria-label="অ্যাসাইনমেন্ট খুঁজুন"
                className="pl-9"
              />
            </div>
          </div>
        </div>

        <DataTable
          columns={columns}
          rows={data?.items}
          keyOf={(row) => row.id}
          loading={isPending}
          error={error}
          onRetry={refetch}
          emptyTitle={
            search
              ? "এই খোঁজে কিছু পাওয়া যায়নি"
              : status === "Draft"
                ? "কোনো খসড়া নেই"
                : status === "Published"
                  ? "এখনো কিছু প্রকাশ করা হয়নি"
                  : "এখনো কোনো অ্যাসাইনমেন্ট নেই"
          }
          emptyDescription="একটি অ্যাসাইনমেন্ট তৈরি করে প্রকাশ করুন, তখন শিক্ষার্থীরা দেখতে পাবে।"
          emptyAction={
            <Button size="sm" onClick={() => setCreating(true)}>
              <Plus className="size-4" />
              নতুন অ্যাসাইনমেন্ট
            </Button>
          }
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

      <AssignmentFormModal open={creating} onClose={() => setCreating(false)} />
    </>
  );
}
