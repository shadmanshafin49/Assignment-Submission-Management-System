"use client";

import { PageHeader } from "@/components/app-shell";
import { Badge, SubjectChip } from "@/components/ui/badge";
import { Card, CardBody } from "@/components/ui/card";
import { EmptyState, ErrorState, TableSkeleton } from "@/components/ui/states";
import { useMyCourses } from "@/hooks/use-teacher";
import { useLabels } from "@/lib/reference";
import { bn, subjectTone } from "@/lib/utils";
import { BookMarked, ClipboardList, Users } from "lucide-react";
import Link from "next/link";

/**
 * The teacher's own courses. This is also the answer to "why can't I set work
 * for class 8?" — a teacher may only assign to a course an admin put them on,
 * and this page is the list of those.
 */
export default function TeacherCoursesPage() {
  const labels = useLabels();
  const { data, isPending, error, refetch } = useMyCourses();

  if (isPending) {
    return (
      <Card>
        <TableSkeleton />
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

  return (
    <>
      <PageHeader
        title="আমার কোর্স"
        description="আপনি যেসব শ্রেণিতে যে বিষয় পড়ান — কেবল এগুলোতেই কাজ দিতে পারবেন।"
      />

      {data.length === 0 ? (
        <Card>
          <EmptyState
            title="কোনো কোর্সে নিয়োগ নেই"
            description="প্রধান শিক্ষক আপনাকে কোর্সে নিয়োগ দিলে সেটি এখানে দেখা যাবে।"
          />
        </Card>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {data.map((course) => {
            const tone = subjectTone(course.code);
            return (
              <Card key={course.id} className="overflow-hidden">
                <span
                  aria-hidden
                  className="block h-1.5 w-full"
                  style={{ backgroundColor: tone.color }}
                />
                <CardBody className="flex flex-col gap-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <h2 className="font-semibold">{course.subjectName}</h2>
                      <p className="text-sm text-muted">
                        {course.classRoomName} · {course.code}
                      </p>
                    </div>
                    <SubjectChip code={course.code} name={course.subjectCode} />
                  </div>

                  {course.textbookName && (
                    <p className="flex items-center gap-1.5 text-sm text-muted">
                      <BookMarked className="size-4 shrink-0" />
                      {course.textbookName}
                    </p>
                  )}

                  <div className="flex flex-wrap gap-3 text-sm text-muted">
                    <span className="inline-flex items-center gap-1.5">
                      <Users className="size-4" />
                      {bn(course.studentCount)} জন
                    </span>
                    <span className="inline-flex items-center gap-1.5">
                      <ClipboardList className="size-4" />
                      {bn(course.publishedAssignmentCount)}/
                      {bn(course.assignmentCount)} প্রকাশিত
                    </span>
                    <span>সপ্তাহে {bn(course.weeklyPeriods)} পিরিয়ড</span>
                  </div>

                  {/* The types NCTB allows for this subject — the same list the
                      assignment form offers, shown here so it is knowable before
                      opening the form. */}
                  <div className="flex flex-wrap gap-1">
                    {course.allowedAssignmentTypes.map((type) => (
                      <Badge key={type} tone="neutral">
                        {labels.assignmentType(type)}
                      </Badge>
                    ))}
                  </div>

                  <Link
                    href={`/teacher?courseId=${course.id}`}
                    className="text-sm font-medium text-primary hover:underline"
                  >
                    এই কোর্সের অ্যাসাইনমেন্ট দেখুন →
                  </Link>
                </CardBody>
              </Card>
            );
          })}
        </div>
      )}
    </>
  );
}
