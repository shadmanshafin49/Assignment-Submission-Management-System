"use client";

import { PageHeader } from "@/components/app-shell";
import {
  BreakBand,
  BreakColumnHeader,
  breakColumnAfter,
} from "@/components/routine-break";
import { Card, CardBody } from "@/components/ui/card";
import { EmptyState, ErrorState, TableSkeleton } from "@/components/ui/states";
import { useMyRoutine } from "@/hooks/use-teacher";
import { useLabels, useSchoolDay } from "@/lib/reference";
import type { TeacherRoutineSlotDto, WeekDay } from "@/lib/types";
import { bn, cn, periodLabel, subjectTone } from "@/lib/utils";
import { Fragment } from "react";

/**
 * The teacher's own week. Unlike the class routine this is sparse — a teacher
 * has free periods — so it renders the same six-by-six grid with the empty
 * cells left visibly empty, which is exactly the question being asked of it:
 * "when am I free?"
 */
export default function TeacherRoutinePage() {
  const { periods, weekDays, breakAfterPeriod, breakStart, breakEnd } =
    useSchoolDay();
  const labels = useLabels();
  const { data, isPending, error, refetch } = useMyRoutine();
  const breakAfter = breakColumnAfter(periods, breakAfterPeriod);

  if (isPending) {
    return (
      <Card>
        <TableSkeleton rows={7} />
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <ErrorState error={error} onRetry={refetch} />
      </Card>
    );
  }

  const slots = new Map<string, TeacherRoutineSlotDto>(
    data.map((slot) => [`${slot.day}:${slot.periodIndex}`, slot]),
  );

  return (
    <>
      <PageHeader
        title="আমার রুটিন"
        description={`সপ্তাহে ${bn(data.length)}টি পিরিয়ড`}
      />

      {data.length === 0 ? (
        <Card>
          <EmptyState
            title="রুটিনে কোনো পিরিয়ড নেই"
            description="প্রধান শিক্ষক রুটিনে আপনার কোর্স বসালে সেটি এখানে দেখা যাবে।"
          />
        </Card>
      ) : (
        <Card>
          <CardBody className="p-3 sm:p-4">
            <div className="overflow-x-auto">
              <table className="w-full min-w-4xl border-collapse text-sm">
                <caption className="sr-only">
                  আমার সাপ্তাহিক পাঠদান রুটিন — {bn(breakStart)}–{bn(breakEnd)}{" "}
                  টিফিন বিরতি।
                </caption>
                <thead>
                  <tr>
                    <th className="w-24 border-b border-border px-3 py-2 text-left text-xs font-semibold text-muted">
                      দিন
                    </th>
                    {periods.map((period) => (
                      <Fragment key={period.periodIndex}>
                        <th
                          scope="col"
                          className="border-b border-border px-2 py-2 text-center text-xs font-semibold text-muted"
                        >
                          <span className="block">
                            {periodLabel(period.periodIndex)}
                          </span>
                          <span className="block font-normal">
                            {bn(period.startTime)}–{bn(period.endTime)}
                          </span>
                        </th>
                        {period.periodIndex === breakAfter && (
                          <BreakColumnHeader
                            start={breakStart}
                            end={breakEnd}
                          />
                        )}
                      </Fragment>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {weekDays.map((day, dayIndex) => (
                    <tr key={day.value}>
                      <th
                        scope="row"
                        className="border-b border-border px-3 py-2 text-left font-medium"
                      >
                        {day.label}
                      </th>
                      {periods.map((period) => {
                        const slot = slots.get(
                          `${day.value as WeekDay}:${period.periodIndex}`,
                        );
                        const tone = slot ? subjectTone(slot.courseCode) : null;

                        return (
                          <Fragment key={period.periodIndex}>
                            <td className="border-b border-border p-1 align-top">
                              <div
                                className={cn(
                                  "flex min-h-14 items-center justify-center rounded-md px-2 py-1 text-center text-xs leading-tight",
                                  !slot && "text-muted",
                                )}
                                style={
                                  tone
                                    ? {
                                        color: tone.color,
                                        backgroundColor: tone.background,
                                      }
                                    : undefined
                                }
                              >
                                {slot ? (
                                  <span>
                                    <span className="block font-medium">
                                      {slot.subjectName}
                                    </span>
                                    <span className="block opacity-80">
                                      {slot.classRoomName}
                                    </span>
                                  </span>
                                ) : (
                                  "ফাঁকা"
                                )}
                              </div>
                            </td>
                            {period.periodIndex === breakAfter &&
                              dayIndex === 0 && (
                                <BreakBand
                                  rows={weekDays.length}
                                  start={breakStart}
                                  end={breakEnd}
                                />
                              )}
                          </Fragment>
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardBody>
        </Card>
      )}

      <p className="mt-3 text-xs text-muted">
        {labels.day("Saturday")} থেকে {labels.day("Thursday")} — শুক্রবার সাপ্তাহিক
        ছুটি।
      </p>
    </>
  );
}
