"use client";

import { PageHeader } from "@/components/app-shell";
import { AssignmentStatusBadge, Badge, SubjectChip } from "@/components/ui/badge";
import { Card, CardHeader, StatTile } from "@/components/ui/card";
import { DataTable, type Column } from "@/components/ui/data-table";
import {
  useClasses,
  useCourses,
  useSubjects,
  useUsers,
} from "@/hooks/use-admin";
import { useAssignments, useSubmissions } from "@/hooks/use-teacher";
import { useSchoolDay } from "@/lib/reference";
import type { AssignmentDto } from "@/lib/types";
import { bn, formatDateTime, formatRelative } from "@/lib/utils";
import {
  BookOpen,
  ClipboardList,
  FileText,
  GraduationCap,
  Library,
  Users,
} from "lucide-react";
import Link from "next/link";

/** Totals come from `totalCount`, so we ask for the smallest page possible. */
const COUNT_ONLY = { page: 1, pageSize: 1 } as const;

export default function AdminOverviewPage() {
  const { periodsPerWeek } = useSchoolDay();

  const students = useUsers({ ...COUNT_ONLY, role: "Student" });
  const teachers = useUsers({ ...COUNT_ONLY, role: "Teacher" });
  const classes = useClasses(COUNT_ONLY);
  const subjects = useSubjects(COUNT_ONLY);
  const courses = useCourses(COUNT_ONLY);
  const assignments = useAssignments(COUNT_ONLY);
  const submissions = useSubmissions(COUNT_ONLY);
  const unstaffed = useCourses({ ...COUNT_ONLY, unstaffed: true });

  const recent = useAssignments({ page: 1, pageSize: 8 });

  const columns: Column<AssignmentDto>[] = [
    {
      id: "title",
      header: "অ্যাসাইনমেন্ট",
      primary: true,
      cell: (row) => <span className="font-medium">{row.title}</span>,
    },
    {
      id: "course",
      header: "কোর্স",
      cell: (row) => (
        <div className="flex flex-col items-start gap-1">
          <SubjectChip code={row.courseCode} name={row.subjectName} />
          <span className="text-xs text-muted">{row.classRoomName}</span>
        </div>
      ),
    },
    {
      id: "teacher",
      header: "শিক্ষক",
      hideOnMobile: true,
      cell: (row) => (
        <span className="text-muted">{row.createdByTeacherName}</span>
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
        <span className="tabular-nums text-muted">
          {bn(row.gradedCount)}/{bn(row.submissionCount)}
        </span>
      ),
    },
  ];

  const unstaffedCount = unstaffed.data?.totalCount ?? 0;

  return (
    <>
      <PageHeader
        title="সারসংক্ষেপ"
        description="বিদ্যালয়ের সামগ্রিক চিত্র ও সাম্প্রতিক অ্যাসাইনমেন্ট।"
      />

      <div className="mb-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile
          label="শিক্ষার্থী"
          value={bn(students.data?.totalCount ?? "—")}
          icon={<Users className="size-5" />}
        />
        <StatTile
          label="শিক্ষক"
          value={bn(teachers.data?.totalCount ?? "—")}
          icon={<GraduationCap className="size-5" />}
        />
        <StatTile
          label="শ্রেণি"
          value={bn(classes.data?.totalCount ?? "—")}
          hint={`সপ্তাহে ${bn(periodsPerWeek)}টি পিরিয়ড`}
          icon={<Library className="size-5" />}
        />
        <StatTile
          label="বিষয়"
          value={bn(subjects.data?.totalCount ?? "—")}
          icon={<BookOpen className="size-5" />}
        />
        <StatTile
          label="কোর্স"
          value={bn(courses.data?.totalCount ?? "—")}
          hint={
            unstaffedCount > 0
              ? `${bn(unstaffedCount)}টিতে শিক্ষক নেই`
              : "সব কোর্সে শিক্ষক নিয়োগ আছে"
          }
          icon={<GraduationCap className="size-5" />}
        />
        <StatTile
          label="অ্যাসাইনমেন্ট"
          value={bn(assignments.data?.totalCount ?? "—")}
          icon={<ClipboardList className="size-5" />}
        />
        <StatTile
          label="জমা"
          value={bn(submissions.data?.totalCount ?? "—")}
          icon={<FileText className="size-5" />}
        />
      </div>

      {unstaffedCount > 0 && (
        <Card className="mb-4 p-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <p className="font-medium">শিক্ষকবিহীন কোর্স রয়েছে</p>
              <p className="text-sm text-muted">
                শিক্ষক নিয়োগ না দিলে ঐ কোর্সে কেউ অ্যাসাইনমেন্ট দিতে পারবে না।
              </p>
            </div>
            <Link
              href="/admin/courses"
              className="text-sm font-medium text-primary hover:underline"
            >
              <Badge tone="warning">{bn(unstaffedCount)}টি কোর্স দেখুন</Badge>
            </Link>
          </div>
        </Card>
      )}

      <Card>
        <CardHeader
          title="সাম্প্রতিক অ্যাসাইনমেন্ট"
          description="প্রশাসক কেবল পর্যবেক্ষণ করেন — কাজ দেওয়া ও মূল্যায়ন শিক্ষকের এখতিয়ার।"
          action={
            <Link
              href="/admin/assignments"
              className="text-sm font-medium text-primary hover:underline"
            >
              সব দেখুন
            </Link>
          }
        />
        <DataTable
          columns={columns}
          rows={recent.data?.items}
          keyOf={(row) => row.id}
          loading={recent.isPending}
          error={recent.error}
          onRetry={recent.refetch}
          emptyTitle="এখনো কোনো অ্যাসাইনমেন্ট নেই"
          emptyDescription="শিক্ষকরা এখনো কোনো কাজ তৈরি করেননি।"
        />
      </Card>
    </>
  );
}
