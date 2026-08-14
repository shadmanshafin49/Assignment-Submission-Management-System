"use client";

import { bn, cn } from "@/lib/utils";
import { Button } from "./button";
import { EmptyState, ErrorState, TableSkeleton } from "./states";

export interface Column<T> {
  /** Stable id, also used as the React key. */
  id: string;
  header: string;
  cell: (row: T) => React.ReactNode;
  /** Doubles as the card title in the mobile layout. */
  primary?: boolean;
  /** Dropped entirely from the stacked card layout. */
  hideOnMobile?: boolean;
  align?: "left" | "right";
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[] | undefined;
  keyOf: (row: T) => string;
  loading?: boolean;
  error?: unknown;
  onRetry?: () => void;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyAction?: React.ReactNode;
  onRowClick?: (row: T) => void;
}

/**
 * Renders a real `<table>` from `md` up and collapses to stacked cards below —
 * a plain overflow-scroll table is unusable on a phone, which the brief calls
 * out as a requirement.
 */
export function DataTable<T>({
  columns,
  rows,
  keyOf,
  loading,
  error,
  onRetry,
  emptyTitle = "এখানে এখনো কিছু নেই",
  emptyDescription,
  emptyAction,
  onRowClick,
}: DataTableProps<T>) {
  if (loading) return <TableSkeleton />;
  if (error) return <ErrorState error={error} onRetry={onRetry} />;
  if (!rows || rows.length === 0) {
    return (
      <EmptyState
        title={emptyTitle}
        description={emptyDescription}
        action={emptyAction}
      />
    );
  }

  const primary = columns.find((c) => c.primary) ?? columns[0];
  const secondary = columns.filter(
    (c) => c.id !== primary.id && !c.hideOnMobile,
  );

  return (
    <>
      {/* Desktop */}
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-b border-border text-left">
              {columns.map((col) => (
                <th
                  key={col.id}
                  scope="col"
                  className={cn(
                    "px-4 py-3 text-xs font-semibold uppercase tracking-wide text-muted",
                    col.align === "right" && "text-right",
                  )}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr
                key={keyOf(row)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                className={cn(
                  "border-b border-border last:border-0",
                  onRowClick && "cursor-pointer hover:bg-surface-muted",
                )}
              >
                {columns.map((col) => (
                  <td
                    key={col.id}
                    className={cn(
                      "px-4 py-3 align-middle",
                      col.align === "right" && "text-right",
                    )}
                  >
                    {col.cell(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile */}
      <ul className="flex flex-col divide-y divide-border md:hidden">
        {rows.map((row) => (
          <li
            key={keyOf(row)}
            onClick={onRowClick ? () => onRowClick(row) : undefined}
            className={cn("p-4", onRowClick && "cursor-pointer active:bg-surface-muted")}
          >
            <div className="mb-2 font-medium">{primary.cell(row)}</div>
            <dl className="flex flex-col gap-1">
              {secondary.map((col) => (
                <div key={col.id} className="flex justify-between gap-3 text-sm">
                  <dt className="text-muted">{col.header}</dt>
                  <dd className="text-right">{col.cell(row)}</dd>
                </div>
              ))}
            </dl>
          </li>
        ))}
      </ul>
    </>
  );
}

export function Pagination({
  page,
  totalPages,
  totalCount,
  hasPrevious,
  hasNext,
  onPageChange,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  hasPrevious: boolean;
  hasNext: boolean;
  onPageChange: (page: number) => void;
}) {
  if (totalCount === 0) return null;

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border px-4 py-3">
      <p className="text-xs text-muted">
        পৃষ্ঠা {bn(page)} / {bn(totalPages)} · মোট {bn(totalCount)}টি
      </p>
      <div className="flex gap-2">
        <Button
          size="sm"
          variant="secondary"
          disabled={!hasPrevious}
          onClick={() => onPageChange(page - 1)}
        >
          পূর্ববর্তী
        </Button>
        <Button
          size="sm"
          variant="secondary"
          disabled={!hasNext}
          onClick={() => onPageChange(page + 1)}
        >
          পরবর্তী
        </Button>
      </div>
    </div>
  );
}
