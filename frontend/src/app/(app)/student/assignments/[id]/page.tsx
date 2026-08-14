"use client";

import { PageHeader } from "@/components/app-shell";
import {
  AttachmentList,
  FilePicker,
  PendingFileList,
} from "@/components/attachments";
import { CommentThread } from "@/components/comments";
import {
  Badge,
  DeadlineBadge,
  SubjectChip,
  SubmissionStatusBadge,
} from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { Field, Textarea } from "@/components/ui/field";
import { ErrorState, Skeleton } from "@/components/ui/states";
import {
  useAddSubmissionAttachment,
  useRemoveSubmissionAttachment,
  useStudentAssignment,
  useSubmitAssignment,
  useUpdateSubmission,
} from "@/hooks/use-student";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { StudentAssignmentDto, SubmissionDto } from "@/lib/types";
import { bn, formatDateTime, formatRelative } from "@/lib/utils";
import { ArrowRight, Lock } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";

export default function StudentAssignmentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const labels = useLabels();
  const { data, isPending, error, refetch } = useStudentAssignment(id);

  if (isPending) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-56 w-full" />
      </div>
    );
  }

  if (error) return <ErrorState error={error} onRetry={refetch} />;

  return (
    <>
      <Link
        href="/student"
        className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted hover:text-foreground"
      >
        <ArrowRight className="size-4 rotate-180" />
        অ্যাসাইনমেন্ট তালিকায় ফিরে যাই
      </Link>

      <PageHeader
        title={data.title}
        description={`${data.courseCode} · ${data.classRoomName} · ${data.teacherName}`}
      />

      <div className="mb-4 flex flex-wrap items-center gap-1.5">
        <SubjectChip code={data.courseCode} name={data.subjectName} />
        <Badge tone="neutral">{labels.assignmentType(data.type)}</Badge>
        {data.chapterOrLesson && (
          <Badge tone="neutral">{data.chapterOrLesson}</Badge>
        )}
        <DeadlineBadge
          deadline={data.deadline}
          isPastDeadline={data.isPastDeadline}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="flex flex-col gap-4 lg:col-span-2">
          <Card>
            <CardHeader title="নির্দেশনা" />
            <CardBody className="flex flex-col gap-4">
              <p className="whitespace-pre-wrap text-sm leading-relaxed">
                {data.description}
              </p>

              {data.attachments.length > 0 && (
                <div>
                  <p className="mb-2 text-xs font-medium text-muted">
                    শিক্ষকের দেওয়া ফাইল
                  </p>
                  <AttachmentList
                    attachments={data.attachments}
                    downloadBase={`/assignments/${data.id}/attachments`}
                  />
                </div>
              )}
            </CardBody>
          </Card>

          <SubmissionSection assignment={data} />

          <CommentThread
            assignmentId={data.id}
            allowComments={data.allowComments}
            role="Student"
          />
        </div>

        <div className="flex flex-col gap-4">
          <Card>
            <CardHeader title="বিস্তারিত" />
            <CardBody className="flex flex-col gap-3 text-sm">
              <Detail label="সময়সীমা">
                <div className="flex flex-col items-end gap-1">
                  <span>{formatDateTime(data.deadline)}</span>
                  <span className="text-xs text-muted">
                    {formatRelative(data.deadline)}
                  </span>
                </div>
              </Detail>
              <Detail label="সপ্তাহ">{bn(data.weekNumber)} নম্বর সপ্তাহ</Detail>
              <Detail label="পূর্ণমান">{bn(data.maxMarks)}</Detail>
              <Detail label="বিলম্বে জমা">
                <Badge tone={data.allowLateSubmission ? "success" : "neutral"}>
                  {data.allowLateSubmission ? "গ্রহণযোগ্য" : "গ্রহণ করা হয় না"}
                </Badge>
              </Detail>
              <Detail label="জমার পর সম্পাদনা">
                <Badge tone={data.allowResubmission ? "success" : "neutral"}>
                  {data.allowResubmission ? "করা যাবে" : "করা যাবে না"}
                </Badge>
              </Detail>
            </CardBody>
          </Card>

          {data.mySubmission?.status === "Graded" && (
            <GradeCard submission={data.mySubmission} />
          )}
        </div>
      </div>
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

function GradeCard({ submission }: { submission: SubmissionDto }) {
  const pct =
    submission.marks != null && submission.maxMarks > 0
      ? Math.round((submission.marks / submission.maxMarks) * 100)
      : null;

  return (
    <Card>
      <CardHeader title="আমার নম্বর" />
      <CardBody className="flex flex-col gap-3">
        <div className="flex items-baseline gap-2">
          <span className="text-3xl font-semibold tabular-nums text-success">
            {bn(submission.marks)}
          </span>
          <span className="text-muted">/ {bn(submission.maxMarks)}</span>
          {pct !== null && <Badge tone="success">{bn(pct)}%</Badge>}
        </div>

        {submission.feedback && (
          <div>
            <p className="mb-1 text-xs font-medium text-muted">শিক্ষকের মন্তব্য</p>
            <p className="whitespace-pre-wrap rounded-lg bg-surface-muted p-3 text-sm">
              {submission.feedback}
            </p>
          </div>
        )}

        <p className="text-xs text-muted">
          মূল্যায়ন করেছেন {submission.gradedByTeacherName ?? "—"} ·{" "}
          {formatDateTime(submission.gradedAt)}
        </p>
      </CardBody>
    </Card>
  );
}

/**
 * Which of the four states we render is decided entirely by the server's
 * `canSubmit` / `canEdit` flags — the deadline and resubmission rules are the
 * API's to own, and duplicating them here would let the two drift apart.
 */
function SubmissionSection({
  assignment,
}: {
  assignment: StudentAssignmentDto;
}) {
  const submission = assignment.mySubmission;

  if (submission && !submission.canEdit) {
    return <LockedSubmission submission={submission} />;
  }

  if (submission?.canEdit) {
    return <EditSubmission assignment={assignment} submission={submission} />;
  }

  if (assignment.canSubmit) {
    return <NewSubmission assignment={assignment} />;
  }

  return (
    <Card>
      <CardHeader title="আমার জমা" />
      <CardBody>
        <div className="flex flex-col items-center gap-2 py-6 text-center">
          <Lock className="size-6 text-muted" />
          <p className="font-medium">জমা দেওয়ার সময় শেষ</p>
          <p className="max-w-sm text-sm text-muted">
            {formatDateTime(assignment.deadline)} তারিখে সময়সীমা শেষ হয়েছে এবং
            এই অ্যাসাইনমেন্টে বিলম্বে জমা গ্রহণ করা হয় না।
          </p>
        </div>
      </CardBody>
    </Card>
  );
}

function LockedSubmission({ submission }: { submission: SubmissionDto }) {
  return (
    <Card>
      <CardHeader
        title="আমার জমা"
        action={<SubmissionStatusBadge status={submission.status} />}
      />
      <CardBody className="flex flex-col gap-3">
        {submission.answerText && (
          <p className="whitespace-pre-wrap rounded-lg bg-surface-muted p-3 text-sm leading-relaxed">
            {submission.answerText}
          </p>
        )}

        <AttachmentList
          attachments={submission.attachments}
          downloadBase={`/student/submissions/${submission.id}/attachments`}
        />

        <p className="flex flex-wrap items-center gap-1.5 text-xs text-muted">
          <Lock className="size-3.5" />
          জমা দেওয়া হয়েছে {formatDateTime(submission.submittedAt)}
          {submission.updatedAt &&
            ` · সম্পাদিত ${formatDateTime(submission.updatedAt)}`}
          {" · এটি আর পরিবর্তন করা যাবে না"}
        </p>
      </CardBody>
    </Card>
  );
}

/**
 * A first submission sends text and files together in one multipart request:
 * the API refuses an answer that has neither, so uploading afterwards would
 * mean sending an empty submission first and being turned down.
 */
function NewSubmission({ assignment }: { assignment: StudentAssignmentDto }) {
  const submit = useSubmitAssignment(assignment.id);
  const [answerText, setAnswerText] = useState("");
  const [files, setFiles] = useState<File[]>([]);

  const isEmpty = answerText.trim().length === 0 && files.length === 0;

  async function send() {
    try {
      await submit.mutateAsync({ answerText, files });
      toast.success("অ্যাসাইনমেন্ট জমা হয়েছে");
      setAnswerText("");
      setFiles([]);
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <Card>
      <CardHeader
        title="উত্তর জমা দাও"
        description={
          assignment.isPastDeadline && assignment.allowLateSubmission
            ? "সময়সীমা পেরিয়ে গেছে — জমাটি বিলম্বিত হিসেবে চিহ্নিত হবে।"
            : undefined
        }
      />
      <CardBody className="flex flex-col gap-4">
        <Field
          label="লিখিত উত্তর"
          htmlFor="answerText"
          hint="খাতার ছবি বা পিডিএফ দিলে লেখা বাধ্যতামূলক নয়।"
        >
          <Textarea
            id="answerText"
            rows={9}
            value={answerText}
            onChange={(e) => setAnswerText(e.target.value)}
            placeholder="এখানে তোমার উত্তর লেখো…"
          />
        </Field>

        <div className="flex flex-col gap-2">
          <FilePicker
            onPick={(picked) => setFiles((prev) => [...prev, ...picked])}
            disabled={submit.isPending}
            hint="পিডিএফ, ছবি বা ডকুমেন্ট — সর্বোচ্চ ৩টি"
          />
          <PendingFileList
            files={files}
            onRemove={(index) =>
              setFiles((prev) => prev.filter((_, i) => i !== index))
            }
          />
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-xs text-muted">
            সময়সীমা {formatDateTime(assignment.deadline)}
          </p>
          <Button onClick={send} loading={submit.isPending} disabled={isEmpty}>
            জমা দাও
          </Button>
        </div>
      </CardBody>
    </Card>
  );
}

/** Editing an existing answer: text is saved as JSON, files go one at a time. */
function EditSubmission({
  assignment,
  submission,
}: {
  assignment: StudentAssignmentDto;
  submission: SubmissionDto;
}) {
  const update = useUpdateSubmission(submission.id);
  const addFile = useAddSubmissionAttachment(submission.id);
  const removeFile = useRemoveSubmissionAttachment(submission.id);

  const [answerText, setAnswerText] = useState(submission.answerText);
  const dirty = answerText !== submission.answerText;

  async function save() {
    try {
      await update.mutateAsync({ answerText });
      toast.success("জমা হালনাগাদ হয়েছে");
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  async function upload(picked: File[]) {
    for (const file of picked) {
      try {
        await addFile.mutateAsync(file);
      } catch (err) {
        toast.error(describeError(err));
        return;
      }
    }
    toast.success("ফাইল যুক্ত হয়েছে");
  }

  return (
    <Card>
      <CardHeader
        title="আমার জমা সম্পাদনা"
        description={
          submission.status === "ReturnedForRevision"
            ? "শিক্ষক কাজটি সংশোধনের জন্য ফেরত দিয়েছেন।"
            : undefined
        }
        action={<SubmissionStatusBadge status={submission.status} />}
      />
      <CardBody className="flex flex-col gap-4">
        <Field label="লিখিত উত্তর" htmlFor="answerText">
          <Textarea
            id="answerText"
            rows={9}
            value={answerText}
            onChange={(e) => setAnswerText(e.target.value)}
          />
        </Field>

        <div className="flex flex-col gap-2">
          <AttachmentList
            attachments={submission.attachments}
            downloadBase={`/student/submissions/${submission.id}/attachments`}
            onRemove={(attachmentId) =>
              removeFile.mutate(attachmentId, {
                onError: (err) => toast.error(describeError(err)),
              })
            }
            removing={removeFile.isPending}
          />
          <FilePicker
            onPick={upload}
            disabled={addFile.isPending}
            label={addFile.isPending ? "যুক্ত হচ্ছে…" : "আরও ফাইল যুক্ত করো"}
          />
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-xs text-muted">
            সর্বশেষ সংরক্ষণ{" "}
            {formatDateTime(submission.updatedAt ?? submission.submittedAt)} ·
            সময়সীমা {formatDateTime(assignment.deadline)}
          </p>
          <Button onClick={save} loading={update.isPending} disabled={!dirty}>
            পরিবর্তন সংরক্ষণ করো
          </Button>
        </div>
      </CardBody>
    </Card>
  );
}
