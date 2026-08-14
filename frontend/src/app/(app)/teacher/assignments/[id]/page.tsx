"use client";

import { PageHeader } from "@/components/app-shell";
import { AssignmentFormModal } from "@/components/assignment-form";
import { AttachmentList, FilePicker } from "@/components/attachments";
import { CommentThread } from "@/components/comments";
import { GradeModal } from "@/components/grade-modal";
import {
  AssignmentStatusBadge,
  Badge,
  SubjectChip,
  SubmissionStatusBadge,
} from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, StatTile } from "@/components/ui/card";
import { DataTable, type Column } from "@/components/ui/data-table";
import { ConfirmDialog } from "@/components/ui/modal";
import { ErrorState, Skeleton } from "@/components/ui/states";
import {
  useAddAssignmentAttachment,
  useAssignment,
  useAssignmentPublication,
  useAssignmentSubmissions,
  useDeleteAssignment,
  useRemoveAssignmentAttachment,
} from "@/hooks/use-teacher";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { SubmissionDto } from "@/lib/types";
import { bn, formatDateTime, formatRelative } from "@/lib/utils";
import {
  ArrowRight,
  CheckCircle2,
  Clock,
  FileText,
  Paperclip,
  Trash2,
} from "lucide-react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";

export default function TeacherAssignmentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const labels = useLabels();

  const { data, isPending, error, refetch } = useAssignment(id);
  const submissions = useAssignmentSubmissions(id);
  const publication = useAssignmentPublication();
  const remove = useDeleteAssignment();
  const addFile = useAddAssignmentAttachment(id);
  const removeFile = useRemoveAssignmentAttachment(id);

  const [editing, setEditing] = useState(false);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [grading, setGrading] = useState<SubmissionDto | null>(null);

  if (isPending) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-72" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error) return <ErrorState error={error} onRetry={refetch} />;

  const isPublished = data.status === "Published";
  const pending = data.submissionCount - data.gradedCount;
  const missing = data.expectedSubmissionCount - data.submissionCount;

  async function togglePublication() {
    try {
      await publication.mutateAsync({ id, publish: !isPublished });
      toast.success(
        isPublished
          ? "খসড়ায় ফেরানো হয়েছে — শিক্ষার্থীরা আর দেখতে পাবে না"
          : "প্রকাশিত হয়েছে — শিক্ষার্থীরা এখন দেখতে পাবে",
      );
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  async function confirmDelete() {
    try {
      await remove.mutateAsync(id);
      toast.success("অ্যাসাইনমেন্ট মুছে ফেলা হয়েছে");
      router.push("/teacher");
    } catch (err) {
      // The API returns 409 when submissions exist — surface its own wording.
      toast.error(describeError(err));
      setConfirmingDelete(false);
    }
  }

  async function uploadFiles(files: File[]) {
    for (const file of files) {
      try {
        await addFile.mutateAsync(file);
      } catch (err) {
        toast.error(describeError(err));
        return;
      }
    }
    toast.success("ফাইল সংযুক্ত হয়েছে");
  }

  const columns: Column<SubmissionDto>[] = [
    {
      id: "student",
      header: "শিক্ষার্থী",
      primary: true,
      cell: (row) => (
        <div className="flex items-center gap-2">
          {/* Marking runs down the roll sheet, so the roll number leads. */}
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
      <Link
        href="/teacher"
        className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted hover:text-foreground"
      >
        <ArrowRight className="size-4 rotate-180" />
        অ্যাসাইনমেন্ট তালিকায় ফিরে যাই
      </Link>

      <PageHeader
        title={data.title}
        description={`${data.courseCode} · ${data.classRoomName} · ${data.subjectName}`}
        action={
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => setEditing(true)}>
              সম্পাদনা
            </Button>
            <Button
              variant={isPublished ? "secondary" : "success"}
              onClick={togglePublication}
              loading={publication.isPending}
            >
              {isPublished ? "খসড়ায় ফেরাও" : "প্রকাশ করুন"}
            </Button>
            <Button
              variant="ghost"
              onClick={() => setConfirmingDelete(true)}
              aria-label="অ্যাসাইনমেন্ট মুছুন"
            >
              <Trash2 className="size-4" />
            </Button>
          </div>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-1.5">
        <SubjectChip code={data.courseCode} name={data.subjectName} />
        <Badge tone="neutral">{labels.assignmentType(data.type)}</Badge>
        {data.chapterOrLesson && (
          <Badge tone="neutral">{data.chapterOrLesson}</Badge>
        )}
        <Badge tone="neutral">{bn(data.weekNumber)} নম্বর সপ্তাহ</Badge>
      </div>

      <div className="mb-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile
          label="অবস্থা"
          value={<AssignmentStatusBadge status={data.status} />}
          hint={
            data.publishedAt
              ? `প্রকাশিত ${formatDateTime(data.publishedAt)}`
              : "শিক্ষার্থীরা এখনো দেখতে পায় না"
          }
        />
        <StatTile
          label="জমা পড়েছে"
          value={`${bn(data.submissionCount)}/${bn(data.expectedSubmissionCount)}`}
          hint={missing > 0 ? `${bn(missing)} জন এখনো দেয়নি` : "সবাই জমা দিয়েছে"}
          icon={<FileText className="size-5" />}
        />
        <StatTile
          label="মূল্যায়িত"
          value={bn(data.gradedCount)}
          hint={
            pending > 0 ? `${bn(pending)}টি মূল্যায়ন বাকি` : "সব মূল্যায়ন শেষ"
          }
          icon={<CheckCircle2 className="size-5" />}
        />
        <StatTile
          label="সময়সীমা"
          value={formatRelative(data.deadline)}
          hint={formatDateTime(data.deadline)}
          icon={<Clock className="size-5" />}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="flex flex-col gap-4 lg:col-span-2">
          <Card>
            <CardHeader
              title="জমা ও মূল্যায়ন"
              description="রোল অনুসারে সাজানো — শিক্ষার্থীর কাজ দেখে নম্বর ও মন্তব্য দিন।"
            />
            <DataTable
              columns={columns}
              rows={submissions.data?.items}
              keyOf={(row) => row.id}
              loading={submissions.isPending}
              error={submissions.error}
              onRetry={submissions.refetch}
              emptyTitle="এখনো কোনো জমা নেই"
              emptyDescription={
                isPublished
                  ? "শিক্ষার্থীরা এখনো এই কাজ জমা দেয়নি।"
                  : "এটি এখনো খসড়া — প্রকাশ করলে শিক্ষার্থীরা জমা দিতে পারবে।"
              }
            />
          </Card>

          <CommentThread
            assignmentId={data.id}
            allowComments={data.allowComments}
            role="Teacher"
          />
        </div>

        <Card>
          <CardHeader title="বিস্তারিত" />
          <CardBody className="flex flex-col gap-3 text-sm">
            <Detail label="পূর্ণ নম্বর">{bn(data.maxMarks)}</Detail>
            <Detail label="বিলম্বে জমা">
              <Badge tone={data.allowLateSubmission ? "success" : "neutral"}>
                {data.allowLateSubmission ? "গ্রহণযোগ্য" : "গ্রহণ করা হয় না"}
              </Badge>
            </Detail>
            <Detail label="শিক্ষার্থীর সম্পাদনা">
              <Badge tone={data.allowResubmission ? "success" : "neutral"}>
                {data.allowResubmission ? "অনুমোদিত" : "বন্ধ"}
              </Badge>
            </Detail>
            <Detail label="শ্রেণি আলোচনা">
              <Badge tone={data.allowComments ? "success" : "neutral"}>
                {data.allowComments ? "খোলা" : "বন্ধ"}
              </Badge>
            </Detail>
            <Detail label="তৈরি">{formatDateTime(data.createdAt)}</Detail>

            <div className="border-t border-border pt-3">
              <p className="mb-1.5 text-xs font-medium text-muted">নির্দেশনা</p>
              <p className="whitespace-pre-wrap text-sm leading-relaxed">
                {data.description}
              </p>
            </div>

            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <p className="text-xs font-medium text-muted">সংযুক্ত ফাইল</p>
              <AttachmentList
                attachments={data.attachments}
                downloadBase={`/assignments/${data.id}/attachments`}
                onRemove={(attachmentId) =>
                  removeFile.mutate(attachmentId, {
                    onError: (err) => toast.error(describeError(err)),
                  })
                }
                removing={removeFile.isPending}
              />
              <FilePicker
                onPick={uploadFiles}
                disabled={addFile.isPending}
                multiple={false}
                label={
                  addFile.isPending ? "যুক্ত হচ্ছে…" : "প্রশ্নপত্র / ওয়ার্কশিট"
                }
              />
            </div>
          </CardBody>
        </Card>
      </div>

      <AssignmentFormModal
        open={editing}
        onClose={() => setEditing(false)}
        assignment={data}
      />

      <GradeModal submission={grading} onClose={() => setGrading(null)} />

      <ConfirmDialog
        open={confirmingDelete}
        onClose={() => setConfirmingDelete(false)}
        onConfirm={confirmDelete}
        loading={remove.isPending}
        destructive
        title="অ্যাসাইনমেন্টটি মুছে ফেলবেন?"
        message={
          data.submissionCount > 0
            ? "এই অ্যাসাইনমেন্টে ইতিমধ্যে জমা রয়েছে, তাই সার্ভার এটি মুছতে দেবে না। শিক্ষার্থীদের থেকে লুকাতে চাইলে খসড়ায় ফিরিয়ে নিন।"
            : "এটি আর ফেরানো যাবে না।"
        }
        confirmLabel="মুছে ফেলুন"
      />
    </>
  );
}

function Detail({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex items-start justify-between gap-3">
      <span className="text-muted">{label}</span>
      <span className="text-right font-medium">{children}</span>
    </div>
  );
}
