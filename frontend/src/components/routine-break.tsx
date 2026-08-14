import type { PeriodSlotDto } from "@/lib/types";
import { bn } from "@/lib/utils";
import { Coffee } from "lucide-react";

/**
 * টিফিন as a column of the routine table.
 *
 * The break used to be a caption pinned to the header of the period beside it,
 * which put the word "টিফিন" directly above six days' worth of scheduled
 * classes — it read as though lessons ran through the break. Giving the break a
 * column of its own makes the claim structural instead of typographic: there is
 * nowhere in that column for a class to go.
 *
 * Shared by the class grid and the teacher's own week so the two cannot drift
 * into disagreeing about when the school stops.
 */

/** Only slot times matter here, so both routine shapes fit. */
type PeriodLike = Pick<PeriodSlotDto, "periodIndex">;

/**
 * The period the break column follows, or `null` when it would fall off the end
 * of the grid — a break after the last period is just the end of the day, and
 * drawing a column for it would add an empty trailing stripe.
 */
export function breakColumnAfter(
  periods: PeriodLike[],
  breakAfterPeriod: number,
): number | null {
  const sits = periods.some((p) => p.periodIndex === breakAfterPeriod);
  const hasPeriodAfter = periods.some((p) => p.periodIndex > breakAfterPeriod);
  return sits && hasPeriodAfter ? breakAfterPeriod : null;
}

/** `১০:৪০`–`১১:৩০` → `৫০ মিনিট`. */
function durationLabel(start: string, end: string): string {
  const minutes = (hhmm: string) => {
    const [h, m] = hhmm.split(":").map(Number);
    return (h ?? 0) * 60 + (m ?? 0);
  };
  return `${bn(minutes(end) - minutes(start))} মিনিট`;
}

export function BreakColumnHeader({
  start,
  end,
}: {
  start: string;
  end: string;
}) {
  return (
    <th
      scope="col"
      className="w-24 border-b border-border px-2 py-2 text-center text-xs font-semibold text-warning"
    >
      <span className="flex items-center justify-center gap-1">
        <Coffee className="size-3.5" aria-hidden />
        টিফিন
      </span>
      <span className="block whitespace-nowrap font-normal">
        {bn(start)}–{bn(end)}
      </span>
    </th>
  );
}

/**
 * The body of the break column: one cell spanning every day, because the break
 * is a property of the school day rather than of any single row.
 */
export function BreakBand({ rows, start, end }: { rows: number; start: string; end: string }) {
  return (
    <td
      rowSpan={rows}
      className="border-b border-border bg-warning-soft p-1 text-center align-middle"
    >
      <span className="block text-xs leading-tight text-warning">
        {durationLabel(start, end)}
      </span>
      <span className="mt-1 block text-[0.68rem] leading-tight text-warning opacity-80">
        ক্লাস নেই
      </span>
    </td>
  );
}
