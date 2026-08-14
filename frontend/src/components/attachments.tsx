"use client";

import { api, describeError } from "@/lib/api-client";
import type { AttachmentDto } from "@/lib/types";
import { cn, formatFileSize } from "@/lib/utils";
import { Download, FileText, Image as ImageIcon, Paperclip, X } from "lucide-react";
import { useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "./ui/button";

function iconFor(contentType: string) {
  return contentType.startsWith("image/") ? ImageIcon : FileText;
}

/**
 * The files hanging off an assignment post or a submission.
 *
 * Downloads go through `api.download` rather than a plain link: the bearer token
 * lives in an httpOnly cookie that only the proxy can exchange, so a bare href
 * would arrive unauthenticated.
 */
export function AttachmentList({
  attachments,
  /** e.g. `/assignments/{id}/attachments` — the id is appended per file. */
  downloadBase,
  onRemove,
  removing,
  className,
}: {
  attachments: AttachmentDto[];
  downloadBase: string;
  onRemove?: (attachmentId: string) => void;
  removing?: boolean;
  className?: string;
}) {
  const [busyId, setBusyId] = useState<string | null>(null);

  if (attachments.length === 0) return null;

  async function download(attachment: AttachmentDto) {
    setBusyId(attachment.id);
    try {
      await api.download(
        `${downloadBase}/${attachment.id}`,
        attachment.fileName,
      );
    } catch (err) {
      toast.error(describeError(err));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <ul className={cn("flex flex-wrap gap-2", className)}>
      {attachments.map((attachment) => {
        const Icon = iconFor(attachment.contentType);
        return (
          <li key={attachment.id}>
            <div className="flex items-center gap-2 rounded-lg border border-border bg-surface px-3 py-2">
              <Icon className="size-4 shrink-0 text-muted" />
              <button
                type="button"
                onClick={() => download(attachment)}
                disabled={busyId === attachment.id}
                className="flex min-w-0 items-center gap-2 text-left text-sm hover:text-primary disabled:opacity-60"
              >
                <span className="max-w-52 truncate">{attachment.fileName}</span>
                <span className="shrink-0 text-xs text-muted">
                  {formatFileSize(attachment.sizeBytes)}
                </span>
                <Download className="size-3.5 shrink-0 text-muted" />
              </button>
              {onRemove && (
                <button
                  type="button"
                  onClick={() => onRemove(attachment.id)}
                  disabled={removing}
                  aria-label={`${attachment.fileName} সরান`}
                  className="rounded p-0.5 text-muted hover:text-danger disabled:opacity-60"
                >
                  <X className="size-4" />
                </button>
              )}
            </div>
          </li>
        );
      })}
    </ul>
  );
}

/**
 * File picker for attaching work. Kept uncontrolled-with-a-ref so the same
 * component can either hand files to a parent form (submission, where text and
 * files must travel in one request) or upload immediately (assignment post,
 * where the post already exists).
 */
export function FilePicker({
  onPick,
  disabled,
  label = "ফাইল যুক্ত করুন",
  multiple = true,
  hint,
}: {
  onPick: (files: File[]) => void;
  disabled?: boolean;
  label?: string;
  multiple?: boolean;
  hint?: string;
}) {
  const inputRef = useRef<HTMLInputElement>(null);

  return (
    <div className="flex flex-wrap items-center gap-2">
      <input
        ref={inputRef}
        type="file"
        multiple={multiple}
        className="hidden"
        accept=".pdf,.doc,.docx,.txt,.jpg,.jpeg,.png"
        onChange={(e) => {
          const files = Array.from(e.target.files ?? []);
          if (files.length > 0) onPick(files);
          // Reset so picking the same file twice still fires a change event.
          e.target.value = "";
        }}
      />
      <Button
        type="button"
        variant="secondary"
        size="sm"
        disabled={disabled}
        onClick={() => inputRef.current?.click()}
      >
        <Paperclip className="size-4" />
        {label}
      </Button>
      {hint && <span className="text-xs text-muted">{hint}</span>}
    </div>
  );
}

/** Files chosen but not yet uploaded — shown before a submission is created. */
export function PendingFileList({
  files,
  onRemove,
}: {
  files: File[];
  onRemove: (index: number) => void;
}) {
  if (files.length === 0) return null;

  return (
    <ul className="flex flex-wrap gap-2">
      {files.map((file, index) => (
        <li
          key={`${file.name}-${index}`}
          className="flex items-center gap-2 rounded-lg border border-dashed border-border bg-surface-muted px-3 py-2 text-sm"
        >
          <FileText className="size-4 shrink-0 text-muted" />
          <span className="max-w-52 truncate">{file.name}</span>
          <span className="text-xs text-muted">
            {formatFileSize(file.size)}
          </span>
          <button
            type="button"
            onClick={() => onRemove(index)}
            aria-label={`${file.name} বাদ দিন`}
            className="rounded p-0.5 text-muted hover:text-danger"
          >
            <X className="size-4" />
          </button>
        </li>
      ))}
    </ul>
  );
}
