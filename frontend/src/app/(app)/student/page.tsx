"use client";

import { PageHeader } from "@/components/app-shell";
import {
  Badge,
  DeadlineBadge,
  SubjectChip,
  SubmissionStatusBadge,
} from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { Pagination } from "@/components/ui/data-table";
import { EmptyState, ErrorState, TableSkeleton } from "@/components/ui/states";
import { useMyEnrolledCourses, useStudentAssignments } from "@/hooks/use-student";
import { useLabels } from "@/lib/reference";
import type { StudentAssignmentDto } from "@/lib/types";
import { bn, cn, formatDateTime, formatRelative, subjectTone } from "@/lib/utils";
import { CalendarClock, MessageCircle, Paperclip } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

type Filter = "all" | "pending" | "due";

const FILTERS: { id: Filter; label: string }[] = [
  { id: "all", label: "সব কাজ" },
  { id: "pending", label: "যা এখনো জমা দেইনি" },
  { id: "due", label: "চলমান" },
];

export default function StudentAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<Filter>("all");
  const [courseId, setCourseId] = useState<string>("");

  const { data: courses } = useMyEnrolledCourses();
  const { data, isPending, error, refetch } = useStudentAssignments({
    page,
    courseId: courseId || undefined,
    pendingOnly: filter === "pending" || undefined,
    dueOnly: filter === "due" || undefined,
  });

  function change(next: Partial<{ filter: Filter; courseId: string }>) {
    if (next.filter !== undefined) setFilter(next.filter);
    if (next.courseId !== undefined) setCourseId(next.courseId);
    setPage(1);
  }

  return (
    <>
      <PageHeader
        title="আমার অ্যাসাইনমেন্ট"
        description="তোমার শ্রেণির প্রকাশিত কাজ, সময়সীমা অনুযায়ী সাজানো।"
      />

      <div className="mb-4 flex flex-col gap-3">
        <div className="flex flex-wrap gap-2">
          {FILTERS.map((option) => (
            <button
              key={option.id}
              type="button"
              onClick={() => change({ filter: option.id })}
              className={cn(
                "rounded-full border px-3 py-1.5 text-sm font-medium transition-colors",
                filter === option.id
                  ? "border-primary bg-primary-soft text-primary"
                  : "border-border bg-surface text-muted hover:bg-surface-muted",
              )}
            >
              {option.label}
            </button>
          ))}
        </div>

        {/* Subject filter, in each subject's own colour — the same chips that
            label the cards below, so the mapping is learned once. */}
        {courses && courses.length > 0 && (
          <div className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
            <button
              type="button"
              onClick={() => change({ courseId: "" })}
              className={cn(
                "shrink-0 rounded-full border px-3 py-1 text-xs font-medium",
                courseId === ""
                  ? "border-foreground/20 bg-surface-muted text-foreground"
                  : "border-border bg-surface text-muted hover:bg-surface-muted",
              )}
            >
              সব বিষয়
            </button>
            {courses.map((course) => {
              const tone = subjectTone(course.code);
              const active = courseId === course.id;
              return (
                <button
                  key={course.id}
                  type="button"
                  onClick={() => change({ courseId: course.id })}
                  className="shrink-0 rounded-full border px-3 py-1 text-xs font-medium transition-colors"
                  style={{
                    color: tone.color,
                    backgroundColor: active ? tone.background : "var(--surface)",
                    borderColor: active ? tone.color : "var(--border)",
                  }}
                >
                  {course.subjectName}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {isPending ? (
        <Card>
          <TableSkeleton />
        </Card>
      ) : error ? (
        <Card>
          <ErrorState error={error} onRetry={refetch} />
        </Card>
      ) : data && data.items.length > 0 ? (
        <>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {data.items.map((assignment) => (
              <AssignmentCard key={assignment.id} assignment={assignment} />
            ))}
          </div>
          {data.totalPages > 1 && (
            <Card className="mt-4">
              <Pagination
                page={data.page}
                totalPages={data.totalPages}
                totalCount={data.totalCount}
                hasPrevious={data.hasPrevious}
                hasNext={data.hasNext}
                onPageChange={setPage}
              />
            </Card>
          )}
        </>
      ) : (
        <Card>
          <EmptyState
            title="এখানে কোনো কাজ নেই"
            description="শিক্ষক নতুন অ্যাসাইনমেন্ট প্রকাশ করলে সেটি এখানে দেখা যাবে।"
          />
        </Card>
      )}
    </>
  );
}

function AssignmentCard({ assignment }: { assignment: StudentAssignmentDto }) {
  const labels = useLabels();
  const submission = assignment.mySubmission;
  const tone = subjectTone(assignment.courseCode);

  return (
    <Link
      href={`/student/assignments/${assignment.id}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-border bg-surface shadow-sm transition-shadow hover:shadow-md"
    >
      {/* The subject's colour as a spine down the card: fourteen subjects is
          too many to tell apart by heading alone. */}
      <span
        aria-hidden
        className="h-1.5 w-full"
        style={{ backgroundColor: tone.color }}
      />

      <div className="flex flex-1 flex-col p-4">
        <div className="mb-2 flex flex-wrap items-center gap-1.5">
          <SubjectChip
            code={assignment.courseCode}
            name={assignment.subjectName}
          />
          <DeadlineBadge
            deadline={assignment.deadline}
            isPastDeadline={assignment.isPastDeadline}
          />
        </div>

        <h2 className="font-medium leading-snug">{assignment.title}</h2>
        <p className="mt-0.5 text-xs text-muted">
          {labels.assignmentType(assignment.type)}
          {assignment.chapterOrLesson && ` · ${assignment.chapterOrLesson}`}
        </p>

        <p className="mt-2 line-clamp-2 text-sm text-muted">
          {assignment.description}
        </p>

        <div className="mt-3 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted">
          <span className="inline-flex items-center gap-1">
            <CalendarClock className="size-3.5" />
            <span title={formatDateTime(assignment.deadline)}>
              {formatRelative(assignment.deadline)}
            </span>
          </span>
          <span aria-hidden>·</span>
          <span>পূর্ণমান {bn(assignment.maxMarks)}</span>
          {assignment.attachments.length > 0 && (
            <span className="inline-flex items-center gap-1">
              <Paperclip className="size-3.5" />
              {bn(assignment.attachments.length)}
            </span>
          )}
          {assignment.commentCount > 0 && (
            <span className="inline-flex items-center gap-1">
              <MessageCircle className="size-3.5" />
              {bn(assignment.commentCount)}
            </span>
          )}
        </div>

        <div className="mt-3 flex items-center justify-between border-t border-border pt-3">
          {submission ? (
            <SubmissionStatusBadge status={submission.status} />
          ) : (
            <Badge tone={assignment.canSubmit ? "info" : "neutral"}>
              {assignment.canSubmit ? "জমা দেওয়া হয়নি" : "সময় শেষ"}
            </Badge>
          )}
          {submission?.marks != null && (
            <span className="text-sm font-semibold tabular-nums">
              {bn(submission.marks)}
              <span className="text-muted">/{bn(submission.maxMarks)}</span>
            </span>
          )}
        </div>
      </div>
    </Link>
  );
}
