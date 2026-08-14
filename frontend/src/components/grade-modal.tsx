"use client";

import { AttachmentList } from "@/components/attachments";
import { SubmissionStatusBadge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Field, Input, Textarea } from "@/components/ui/field";
import { Modal } from "@/components/ui/modal";
import {
  useGradeSubmission,
  useUpdateSubmissionStatus,
} from "@/hooks/use-teacher";
import { describeError } from "@/lib/api-client";
import type { SubmissionDto } from "@/lib/types";
import { bn, formatDateTime } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

/** Marks are bounded by the assignment's own maximum, so the schema is built per submission. */
function buildSchema(maxMarks: number) {
  return z.object({
    marks: z.coerce
      .number({ message: "নম্বর দিন" })
      .int("নম্বর পূর্ণসংখ্যা হতে হবে")
      .min(0, "নম্বর ঋণাত্মক হতে পারে না")
      .max(maxMarks, `নম্বর ${bn(maxMarks)} এর বেশি হতে পারে না`),
    feedback: z.string().trim().max(2000, "মন্তব্য অনেক বড় হয়ে গেছে"),
  });
}

export function GradeModal({
  submission,
  onClose,
}: {
  submission: SubmissionDto | null;
  onClose: () => void;
}) {
  if (!submission) return null;
  return <GradeForm submission={submission} onClose={onClose} />;
}

function GradeForm({
  submission,
  onClose,
}: {
  submission: SubmissionDto;
  onClose: () => void;
}) {
  const schema = buildSchema(submission.maxMarks);
  type Values = z.input<typeof schema>;
  type Parsed = z.output<typeof schema>;

  const grade = useGradeSubmission();
  const changeStatus = useUpdateSubmissionStatus();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Values, unknown, Parsed>({
    resolver: zodResolver(schema),
    defaultValues: {
      marks: submission.marks ?? 0,
      feedback: submission.feedback ?? "",
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await grade.mutateAsync({
        id: submission.id,
        body: { marks: values.marks, feedback: values.feedback || null },
      });
      toast.success("মূল্যায়ন সংরক্ষিত হয়েছে");
      onClose();
    } catch (err) {
      toast.error(describeError(err));
    }
  });

  async function returnForRevision() {
    try {
      await changeStatus.mutateAsync({
        id: submission.id,
        body: { status: "ReturnedForRevision" },
      });
      toast.success("সংশোধনের জন্য ফেরত পাঠানো হয়েছে");
      onClose();
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      size="lg"
      title={`মূল্যায়ন — রোল ${bn(submission.rollNumber)}, ${submission.studentName}`}
      description={submission.assignmentTitle}
      footer={
        <>
          <Button
            variant="secondary"
            onClick={returnForRevision}
            loading={changeStatus.isPending}
          >
            সংশোধনের জন্য ফেরত
          </Button>
          <Button type="submit" form="grade-form" loading={grade.isPending}>
            নম্বর সংরক্ষণ
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <SubmissionStatusBadge status={submission.status} />
          <span className="text-muted">
            জমা {formatDateTime(submission.submittedAt)}
            {submission.updatedAt &&
              ` · সম্পাদিত ${formatDateTime(submission.updatedAt)}`}
          </span>
        </div>

        {submission.answerText && (
          <div>
            <p className="mb-1.5 text-xs font-medium text-muted">উত্তর</p>
            <p className="max-h-60 overflow-y-auto whitespace-pre-wrap rounded-lg bg-surface-muted p-3 text-sm leading-relaxed">
              {submission.answerText}
            </p>
          </div>
        )}

        {submission.attachments.length > 0 && (
          <div>
            <p className="mb-1.5 text-xs font-medium text-muted">
              জমা দেওয়া ফাইল
            </p>
            {/* Handwritten maths and drawing work arrives as a photo far more
                often than as typed text, so the files are part of the answer. */}
            <AttachmentList
              attachments={submission.attachments}
              downloadBase={`/submissions/${submission.id}/attachments`}
            />
          </div>
        )}

        <form
          id="grade-form"
          onSubmit={onSubmit}
          noValidate
          className="flex flex-col gap-4"
        >
          <Field
            label={`নম্বর (পূর্ণমান ${bn(submission.maxMarks)})`}
            htmlFor="marks"
            required
            error={errors.marks?.message}
          >
            <Input
              id="marks"
              type="number"
              min={0}
              max={submission.maxMarks}
              invalid={!!errors.marks}
              {...register("marks")}
            />
          </Field>

          <Field
            label="মন্তব্য"
            htmlFor="feedback"
            error={errors.feedback?.message}
            hint="ঐচ্ছিক — নম্বরের সাথে শিক্ষার্থী এটি দেখতে পাবে।"
          >
            <Textarea
              id="feedback"
              rows={4}
              placeholder="কী ভালো হয়েছে, কোথায় উন্নতি দরকার…"
              invalid={!!errors.feedback}
              {...register("feedback")}
            />
          </Field>
        </form>
      </div>
    </Modal>
  );
}
