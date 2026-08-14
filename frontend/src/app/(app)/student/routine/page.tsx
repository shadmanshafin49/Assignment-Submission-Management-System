"use client";

import { PageHeader } from "@/components/app-shell";
import { RoutineGrid } from "@/components/routine-grid";
import { Card, CardBody } from "@/components/ui/card";
import { EmptyState, ErrorState, TableSkeleton } from "@/components/ui/states";
import { useMyClassRoutine, useMyEnrolledCourses } from "@/hooks/use-student";
import { bn, periodLabel } from "@/lib/utils";
import { Sun, Sunrise } from "lucide-react";

/**
 * The class's weekly routine. A student has exactly one class, so it is read
 * from the first course they take rather than asked for — there is no class
 * picker to get wrong.
 */
export default function StudentRoutinePage() {
  const { data: courses, isPending: coursesPending } = useMyEnrolledCourses();
  const classRoomId = courses?.[0]?.classRoomId;

  const { data, isPending, error, refetch } = useMyClassRoutine(classRoomId);

  if (coursesPending || (classRoomId && isPending)) {
    return (
      <Card>
        <TableSkeleton rows={7} />
      </Card>
    );
  }

  if (!classRoomId) {
    return (
      <Card>
        <EmptyState
          title="কোনো শ্রেণিতে ভর্তি নেই"
          description="প্রধান শিক্ষক শ্রেণিতে ভর্তি করলে রুটিন এখানে দেখা যাবে।"
        />
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

  if (!data) return null;

  return (
    <>
      <PageHeader
        title="ক্লাস রুটিন"
        description={`${data.classRoomName} · শিক্ষাবর্ষ ${bn(data.academicYear)}`}
      />

      <div className="mb-4 flex flex-wrap gap-3 text-sm">
        <span className="inline-flex items-center gap-2 rounded-lg border border-border bg-surface px-3 py-2">
          <Sunrise className="size-4 text-accent" />
          সমাবেশ {bn(data.assemblyStart)}–{bn(data.assemblyEnd)}
        </span>
        <span className="inline-flex items-center gap-2 rounded-lg border border-border bg-surface px-3 py-2">
          <Sun className="size-4 text-accent" />
          টিফিন {bn(data.breakStart)}–{bn(data.breakEnd)} (
          {periodLabel(data.breakAfterPeriod)}ের পর)
        </span>
      </div>

      <Card>
        <CardBody className="p-3 sm:p-4">
          <RoutineGrid routine={data} />
        </CardBody>
      </Card>

      <p className="mt-3 text-xs text-muted">
        ধর্ম ও নৈতিক শিক্ষার পিরিয়ডে একই সময়ে দুটি ক্লাস চলে — নিজ ধর্মের ক্লাসটিই
        তোমার।
      </p>
    </>
  );
}
