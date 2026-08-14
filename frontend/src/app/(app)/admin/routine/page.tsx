"use client";

import { PageHeader } from "@/components/app-shell";
import { RoutineGrid } from "@/components/routine-grid";
import { Button } from "@/components/ui/button";
import { Card, CardBody } from "@/components/ui/card";
import { Field, Select } from "@/components/ui/field";
import { Modal } from "@/components/ui/modal";
import { EmptyState, ErrorState, TableSkeleton } from "@/components/ui/states";
import {
  useAllClasses,
  useClassRoutine,
  useClearRoutinePeriod,
  useCourses,
  useSetRoutinePeriod,
} from "@/hooks/use-admin";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { WeekDay } from "@/lib/types";
import { bn, periodLabel } from "@/lib/utils";
import { Sun, Sunrise } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

/**
 * The routine editor. Thirty-six slots per class, and the rules that make it a
 * scheduling problem rather than a form live in the API: a teacher cannot be in
 * two classrooms in one period, and the ধর্ম ও নৈতিক শিক্ষা period deliberately
 * holds two courses at once. Every edit returns the rebuilt routine, so a
 * refused move leaves the grid exactly as it was.
 */
export default function AdminRoutinePage() {
  const labels = useLabels();
  const classes = useAllClasses();
  const [picked, setPicked] = useState("");

  // Falls back to the first class rather than opening on an empty grid. Derived
  // rather than seeded through an effect, so there is no render where the select
  // is empty and the grid below it disagrees.
  const classRoomId = picked || classes.data?.items[0]?.id || "";
  const setClassRoomId = setPicked;

  const routine = useClassRoutine(classRoomId);
  const courses = useCourses({ classRoomId: classRoomId || undefined, pageSize: 50 });
  const setPeriod = useSetRoutinePeriod();
  const clearPeriod = useClearRoutinePeriod();

  const [editing, setEditing] = useState<{
    day: WeekDay;
    periodIndex: number;
  } | null>(null);
  const [courseId, setCourseId] = useState("");

  async function save() {
    if (!editing || !courseId) return;
    try {
      await setPeriod.mutateAsync({
        classRoomId,
        day: editing.day,
        periodIndex: editing.periodIndex,
        courseId,
      });
      toast.success("রুটিন হালনাগাদ হয়েছে");
      setEditing(null);
      setCourseId("");
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  async function clear() {
    if (!editing) return;
    try {
      await clearPeriod.mutateAsync({
        classRoomId,
        day: editing.day,
        periodIndex: editing.periodIndex,
      });
      toast.success("পিরিয়ড খালি করা হয়েছে");
      setEditing(null);
      setCourseId("");
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <>
      <PageHeader
        title="ক্লাস রুটিন"
        description="সপ্তাহে ছয় দিন, দিনে ছয় পিরিয়ড — মোট ৩৬টি পিরিয়ড। ঘরে ক্লিক করে বিষয় বসান।"
      />

      <Card className="mb-4">
        <CardBody className="flex flex-wrap items-end gap-3">
          <Field label="শ্রেণি" htmlFor="classRoomId">
            <Select
              id="classRoomId"
              value={classRoomId}
              onChange={(e) => setClassRoomId(e.target.value)}
              disabled={classes.isPending}
              className="w-56"
            >
              {classes.data?.items.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </Select>
          </Field>

          {routine.data && (
            <div className="flex flex-wrap gap-3 text-sm">
              <span className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2">
                <Sunrise className="size-4 text-accent" />
                সমাবেশ {bn(routine.data.assemblyStart)}–
                {bn(routine.data.assemblyEnd)}
              </span>
              <span className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2">
                <Sun className="size-4 text-accent" />
                টিফিন {bn(routine.data.breakStart)}–{bn(routine.data.breakEnd)}
              </span>
            </div>
          )}
        </CardBody>
      </Card>

      {routine.isPending && classRoomId ? (
        <Card>
          <TableSkeleton rows={7} />
        </Card>
      ) : routine.error ? (
        <Card>
          <ErrorState error={routine.error} onRetry={routine.refetch} />
        </Card>
      ) : routine.data ? (
        <Card>
          <CardBody className="p-3 sm:p-4">
            <RoutineGrid
              routine={routine.data}
              activeSlot={editing}
              onSlotClick={(day, periodIndex) => {
                setEditing({ day, periodIndex });
                setCourseId("");
              }}
            />
          </CardBody>
        </Card>
      ) : (
        <Card>
          <EmptyState
            title="শ্রেণি নির্বাচন করুন"
            description="রুটিন দেখতে ও সম্পাদনা করতে একটি শ্রেণি বেছে নিন।"
          />
        </Card>
      )}

      <Modal
        open={!!editing}
        onClose={() => setEditing(null)}
        title={
          editing
            ? `${labels.day(editing.day)} · ${periodLabel(editing.periodIndex)}`
            : ""
        }
        description="এই ঘরে যে কোর্সটি বসবে সেটি নির্বাচন করুন।"
        footer={
          <>
            <Button
              variant="secondary"
              onClick={clear}
              loading={clearPeriod.isPending}
            >
              খালি করুন
            </Button>
            <Button onClick={save} loading={setPeriod.isPending} disabled={!courseId}>
              বসান
            </Button>
          </>
        }
      >
        <Field
          label="কোর্স"
          htmlFor="routine-course"
          hint="ধর্ম ও নৈতিক শিক্ষার দুটি কোর্স একই পিরিয়ডে পাশাপাশি বসানো যায়; অন্য যেকোনো বিষয় আগেরটিকে সরিয়ে দেবে।"
        >
          <Select
            id="routine-course"
            value={courseId}
            onChange={(e) => setCourseId(e.target.value)}
            disabled={courses.isPending}
          >
            <option value="">কোর্স নির্বাচন করুন</option>
            {courses.data?.items.map((course) => (
              <option key={course.id} value={course.id}>
                {course.code} — {course.subjectName}
                {course.teacherName ? ` · ${course.teacherName}` : ""}
              </option>
            ))}
          </Select>
        </Field>
      </Modal>
    </>
  );
}
