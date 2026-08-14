"use client";

import {
  useAddComment,
  useAssignmentComments,
  useDeleteComment,
} from "@/hooks/use-comments";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { UserRole } from "@/lib/types";
import { cn, formatRelative, initials } from "@/lib/utils";
import { MessageCircle, Send, Trash2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "./ui/button";
import { Textarea } from "./ui/field";
import { Skeleton } from "./ui/states";

/**
 * The class conversation under an assignment, in the shape students already know
 * from Google Classroom: everyone in the course sees the same thread, and asking
 * "স্যার, ৩ নম্বরটা বুঝিনি" is public so the answer reaches the whole class.
 *
 * Admins are refused by the API — they observe the school, they are not in the
 * lesson — so the composer is hidden rather than left to fail on submit.
 */
export function CommentThread({
  assignmentId,
  allowComments,
  role,
}: {
  assignmentId: string;
  allowComments: boolean;
  role: UserRole;
}) {
  const labels = useLabels();
  const { data: comments, isPending } = useAssignmentComments(assignmentId);
  const add = useAddComment(assignmentId);
  const remove = useDeleteComment(assignmentId);
  const [body, setBody] = useState("");

  const canPost = allowComments && role !== "Admin";

  async function post() {
    const text = body.trim();
    if (!text) return;

    try {
      await add.mutateAsync({ body: text });
      setBody("");
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  const visible = (comments ?? []).filter((c) => !c.isDeleted);

  return (
    <section className="rounded-xl border border-border bg-surface p-4">
      <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold">
        <MessageCircle className="size-4 text-muted" />
        শ্রেণি আলোচনা
        {visible.length > 0 && (
          <span className="text-muted">({visible.length})</span>
        )}
      </h2>

      {isPending ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      ) : visible.length === 0 ? (
        <p className="py-2 text-sm text-muted">
          {allowComments
            ? "এখনো কোনো মন্তব্য নেই। প্রশ্ন থাকলে জিজ্ঞেস করো।"
            : "এই অ্যাসাইনমেন্টে মন্তব্য বন্ধ রাখা হয়েছে।"}
        </p>
      ) : (
        <ul className="flex flex-col gap-3">
          {visible.map((comment) => (
            <li key={comment.id} className="flex gap-3">
              <span
                className={cn(
                  "flex size-8 shrink-0 items-center justify-center rounded-full text-sm font-semibold",
                  comment.authorRole === "Teacher"
                    ? "bg-primary text-primary-foreground"
                    : "bg-surface-muted text-muted",
                )}
                aria-hidden
              >
                {initials(comment.authorName)}
              </span>

              <div className="min-w-0 flex-1">
                <p className="flex flex-wrap items-center gap-x-2 text-sm">
                  <span className="font-medium">{comment.authorName}</span>
                  {comment.authorRole === "Teacher" && (
                    <span className="text-xs text-primary">
                      {labels.role("Teacher")}
                    </span>
                  )}
                  <span className="text-xs text-muted">
                    {formatRelative(comment.createdAt)}
                  </span>
                </p>
                <p className="whitespace-pre-wrap text-sm text-foreground/90">
                  {comment.body}
                </p>
              </div>

              {comment.canDelete && (
                <button
                  type="button"
                  onClick={() => remove.mutate(comment.id)}
                  disabled={remove.isPending}
                  aria-label="মন্তব্য মুছুন"
                  className="h-fit rounded p-1 text-muted hover:text-danger disabled:opacity-60"
                >
                  <Trash2 className="size-4" />
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canPost && (
        <div className="mt-4 flex flex-col gap-2 border-t border-border pt-3">
          <Textarea
            value={body}
            onChange={(e) => setBody(e.target.value)}
            placeholder="শ্রেণির সবার জন্য মন্তব্য লিখুন…"
            className="min-h-20"
            maxLength={2000}
          />
          <div className="flex justify-end">
            <Button
              size="sm"
              onClick={post}
              disabled={add.isPending || body.trim().length === 0}
            >
              <Send className="size-4" />
              {add.isPending ? "পাঠানো হচ্ছে…" : "মন্তব্য করুন"}
            </Button>
          </div>
        </div>
      )}
    </section>
  );
}
