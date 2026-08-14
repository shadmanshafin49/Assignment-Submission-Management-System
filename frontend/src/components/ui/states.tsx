import { cn } from "@/lib/utils";
import { AlertTriangle, Inbox } from "lucide-react";

export function Skeleton({ className }: { className?: string }) {
  return (
    <div
      className={cn("animate-pulse rounded-md bg-surface-muted", className)}
      aria-hidden
    />
  );
}

/** Placeholder rows sized to match the table they replace, to avoid layout jump. */
export function TableSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="flex flex-col gap-2 p-4" aria-busy="true" aria-live="polite">
      <span className="sr-only">লোড হচ্ছে…</span>
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} className="h-11 w-full" />
      ))}
    </div>
  );
}

export function EmptyState({
  title,
  description,
  action,
  icon,
}: {
  title: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-6 py-14 text-center">
      <div className="text-muted">{icon ?? <Inbox className="size-7" />}</div>
      <p className="font-medium">{title}</p>
      {description && (
        <p className="max-w-sm text-sm text-muted">{description}</p>
      )}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

export function ErrorState({
  error,
  onRetry,
}: {
  error: unknown;
  onRetry?: () => void;
}) {
  const message =
    error instanceof Error ? error.message : "কিছু একটা ভুল হয়েছে।";

  return (
    <div className="flex flex-col items-center justify-center gap-2 px-6 py-12 text-center">
      <AlertTriangle className="size-7 text-danger" />
      <p className="font-medium">তথ্য লোড করা যায়নি</p>
      <p className="max-w-sm text-sm text-muted">{message}</p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="mt-2 text-sm font-medium text-primary hover:underline"
        >
          আবার চেষ্টা করুন
        </button>
      )}
    </div>
  );
}
