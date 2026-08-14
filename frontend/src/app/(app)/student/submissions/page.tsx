"use client";

import { PageHeader } from "@/components/app-shell";
import { SubmissionStatusBadge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { useMySubmissions } from "@/hooks/use-student";
import type { SubmissionDto } from "@/lib/types";
import { bn, formatDateTime } from "@/lib/utils";
import { Paperclip } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

const columns: Column<SubmissionDto>[] = [
  {
    id: "assignment",
    header: "অ্যাসাইনমেন্ট",
    primary: true,
    cell: (row) => (
      <Link
        href={`/student/assignments/${row.assignmentId}`}
        className="font-medium text-primary hover:underline"
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
    id: "feedback",
    header: "শিক্ষকের মন্তব্য",
    cell: (row) =>
      row.feedback ? (
        <span className="line-clamp-2 max-w-xs text-sm text-muted">
          {row.feedback}
        </span>
      ) : (
        <span className="text-muted">—</span>
      ),
  },
];

export default function MySubmissionsPage() {
  const [page, setPage] = useState(1);
  const { data, isPending, error, refetch } = useMySubmissions(page);

  return (
    <>
      <PageHeader
        title="আমার জমা"
        description="যা যা জমা দিয়েছি, তার নম্বর ও শিক্ষকের মন্তব্যসহ।"
      />

      <Card>
        <DataTable
          columns={columns}
          rows={data?.items}
          keyOf={(row) => row.id}
          loading={isPending}
          error={error}
          onRetry={refetch}
          emptyTitle="এখনো কিছু জমা দেওয়া হয়নি"
          emptyDescription="কোনো অ্যাসাইনমেন্ট জমা দিলে সেটি এখানে দেখা যাবে।"
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
