"use client";

import type { ClassRoutineDto, RoutineSlotDto, WeekDay } from "@/lib/types";
import { bn, cn, periodLabel, subjectTone } from "@/lib/utils";
import { Fragment } from "react";
import { BreakBand, BreakColumnHeader, breakColumnAfter } from "./routine-break";

/**
 * The weekly routine: six teaching days × six periods, শনিবার to বৃহস্পতিবার.
 *
 * Tiffin is drawn as a column of its own rather than a note on the period beside
 * it, because the note read as a lie: every day still had a class sitting under
 * the word "টিফিন". A column with no cells in it cannot say that.
 *
 * A slot normally holds one course. The ধর্ম ও নৈতিক শিক্ষা period is the real
 * exception — the Muslim and Hindu halves of a class are taught at the same
 * time in different rooms — so a slot renders every entry it carries rather
 * than assuming there is exactly one.
 */
export function RoutineGrid({
  routine,
  onSlotClick,
  activeSlot,
}: {
  routine: ClassRoutineDto;
  onSlotClick?: (day: WeekDay, periodIndex: number) => void;
  activeSlot?: { day: WeekDay; periodIndex: number } | null;
}) {
  const periods = routine.days[0]?.periods ?? [];
  const breakAfter = breakColumnAfter(periods, routine.breakAfterPeriod);

  return (
    <div className="overflow-x-auto">
      {/* Eight columns now that tiffin has its own, so the minimum widens —
          squeezing the subject cells to fit the break would be a poor trade. */}
      <table className="w-full min-w-4xl border-collapse text-sm">
        <caption className="sr-only">
          {routine.classRoomName} শ্রেণির সাপ্তাহিক রুটিন — {bn(routine.breakStart)}
          –{bn(routine.breakEnd)} টিফিন বিরতি, ঐ সময়ে কোনো ক্লাস নেই।
        </caption>
        <thead>
          <tr>
            <th
              scope="col"
              className="w-24 border-b border-border px-3 py-2 text-left text-xs font-semibold text-muted"
            >
              দিন
            </th>
            {periods.map((period) => (
              <Fragment key={period.periodIndex}>
                <th
                  scope="col"
                  className="border-b border-border px-2 py-2 text-center text-xs font-semibold text-muted"
                >
                  <span className="block">{periodLabel(period.periodIndex)}</span>
                  <span className="block font-normal">
                    {bn(period.startTime)}–{bn(period.endTime)}
                  </span>
                </th>
                {period.periodIndex === breakAfter && (
                  <BreakColumnHeader
                    start={routine.breakStart}
                    end={routine.breakEnd}
                  />
                )}
              </Fragment>
            ))}
          </tr>
        </thead>
        <tbody>
          {routine.days.map((day, dayIndex) => (
            <tr key={day.day}>
              <th
                scope="row"
                className="border-b border-border px-3 py-2 text-left font-medium"
              >
                {day.dayName}
              </th>
              {day.periods.map((slot) => (
                <Fragment key={slot.periodIndex}>
                  <td className="border-b border-border p-1 align-top">
                    <SlotCell
                      slot={slot}
                      onClick={
                        onSlotClick
                          ? () => onSlotClick(day.day, slot.periodIndex)
                          : undefined
                      }
                      active={
                        activeSlot?.day === day.day &&
                        activeSlot?.periodIndex === slot.periodIndex
                      }
                    />
                  </td>
                  {/* One cell spanning every day: the break belongs to the school
                      day, not to any one row of it. */}
                  {slot.periodIndex === breakAfter && dayIndex === 0 && (
                    <BreakBand
                      rows={routine.days.length}
                      start={routine.breakStart}
                      end={routine.breakEnd}
                    />
                  )}
                </Fragment>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SlotCell({
  slot,
  onClick,
  active,
}: {
  slot: RoutineSlotDto;
  onClick?: () => void;
  active?: boolean;
}) {
  const content =
    slot.entries.length === 0 ? (
      <span className="text-xs text-muted">—</span>
    ) : (
      <span className="flex flex-col gap-1">
        {slot.entries.map((entry) => {
          const tone = subjectTone(entry.courseCode);
          return (
            <span
              key={entry.courseId}
              className="block rounded-md px-2 py-1 text-xs leading-tight"
              style={{ color: tone.color, backgroundColor: tone.background }}
              title={entry.teacherName ?? undefined}
            >
              <span className="block font-medium">{entry.subjectName}</span>
              {entry.teacherName && (
                <span className="block opacity-80">{entry.teacherName}</span>
              )}
            </span>
          );
        })}
      </span>
    );

  if (!onClick) {
    return (
      <div className="flex min-h-14 items-center justify-center text-center">
        {content}
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex min-h-14 w-full items-center justify-center rounded-md border border-transparent p-0.5 text-center transition-colors",
        active ? "border-primary bg-primary-soft" : "hover:bg-surface-muted",
      )}
    >
      {content}
    </button>
  );
}
