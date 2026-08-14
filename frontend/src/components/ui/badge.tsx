"use client";

import { useLabels } from "@/lib/reference";
import type {
  AssignmentStatus,
  AssignmentType,
  SubmissionStatus,
  UserRole,
} from "@/lib/types";
import { cn, hoursUntil, subjectTone } from "@/lib/utils";

type Tone = "neutral" | "primary" | "success" | "warning" | "danger" | "info";

const TONES: Record<Tone, string> = {
  neutral: "bg-surface-muted text-muted",
  primary: "bg-primary-soft text-primary",
  success: "bg-success-soft text-success",
  warning: "bg-warning-soft text-warning",
  danger: "bg-danger-soft text-danger",
  info: "bg-info-soft text-info",
};

export function Badge({
  tone = "neutral",
  className,
  children,
}: {
  tone?: Tone;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap",
        TONES[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}

/**
 * A subject chip in that subject's own colour. Fourteen subjects in a stream is
 * too many to tell apart by reading, so colour carries the identification and
 * the text confirms it.
 */
export function SubjectChip({
  code,
  name,
  className,
}: {
  /** Course code ("C06-109") or bare subject code ("109"). */
  code: string;
  name: string;
  className?: string;
}) {
  const tone = subjectTone(code);

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap",
        className,
      )}
      style={{ color: tone.color, backgroundColor: tone.background }}
    >
      {name}
    </span>
  );
}

const SUBMISSION_TONES: Record<SubmissionStatus, Tone> = {
  Submitted: "info",
  Late: "warning",
  Graded: "success",
  ReturnedForRevision: "danger",
};

export function SubmissionStatusBadge({ status }: { status: SubmissionStatus }) {
  const labels = useLabels();
  return (
    <Badge tone={SUBMISSION_TONES[status]}>
      {labels.submissionStatus(status)}
    </Badge>
  );
}

export function AssignmentStatusBadge({ status }: { status: AssignmentStatus }) {
  const labels = useLabels();
  return (
    <Badge tone={status === "Published" ? "success" : "neutral"}>
      {labels.assignmentStatus(status)}
    </Badge>
  );
}

/** The NCTB question type — সৃজনশীল প্রশ্ন, ভাবসম্প্রসারণ, ব্যবহারিক কাজ, and so on. */
export function AssignmentTypeBadge({ type }: { type: AssignmentType }) {
  const labels = useLabels();
  return <Badge tone="neutral">{labels.assignmentType(type)}</Badge>;
}

const ROLE_TONES: Record<UserRole, Tone> = {
  Admin: "primary",
  Teacher: "info",
  Student: "neutral",
};

export function RoleBadge({ role }: { role: UserRole }) {
  const labels = useLabels();
  return <Badge tone={ROLE_TONES[role]}>{labels.role(role)}</Badge>;
}

/** Green when there is time left, amber when close, red once it has passed. */
export function DeadlineBadge({
  deadline,
  isPastDeadline,
}: {
  deadline: string;
  isPastDeadline: boolean;
}) {
  if (isPastDeadline) return <Badge tone="danger">সময় শেষ</Badge>;

  if (hoursUntil(deadline) <= 48)
    return <Badge tone="warning">সময় ফুরিয়ে আসছে</Badge>;
  return <Badge tone="success">চলমান</Badge>;
}
