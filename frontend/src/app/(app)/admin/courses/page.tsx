"use client";

import { PageHeader } from "@/components/app-shell";
import { Badge, SubjectChip } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Field, Select } from "@/components/ui/field";
import { Modal } from "@/components/ui/modal";
import {
  useAllClasses,
  useAllSubjects,
  useAssignCourseTeacher,
  useCourses,
  useCreateCourse,
  useUsersByRole,
} from "@/hooks/use-admin";
import { describeError } from "@/lib/api-client";
import type { CourseDto } from "@/lib/types";
import { bn } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

/**
 * Courses — one subject taught to one class by one teacher, coded C06-109.
 * This replaced the old "teaching grant" list: a grant only said who was
 * allowed to teach, while a course is the thing assignments, the routine and
 * the student's subject list all actually hang on.
 */
export default function AdminCoursesPage() {
  const [page, setPage] = useState(1);
  const [classRoomId, setClassRoomId] = useState("");
  const [teacherId, setTeacherId] = useState("");
  const [creating, setCreating] = useState(false);
  const [staffing, setStaffing] = useState<CourseDto | null>(null);

  const classes = useAllClasses();
  const teachers = useUsersByRole("Teacher");
  const { data, isPending, error, refetch } = useCourses({
    page,
    pageSize: 50,
    classRoomId: classRoomId || undefined,
    teacherId: teacherId || undefined,
  });

  const columns: Column<CourseDto>[] = [
    {
      id: "code",
      header: "কোর্স",
      primary: true,
      cell: (row) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium tabular-nums">{row.code}</span>
          <SubjectChip code={row.code} name={row.subjectName} />
        </div>
      ),
    },
    {
      id: "class",
      header: "শ্রেণি",
      cell: (row) => (
        <div className="flex flex-col">
          <span>{row.classRoomName}</span>
          <span className="text-xs text-muted">
            {bn(row.studentCount)} জন · সপ্তাহে {bn(row.weeklyPeriods)} পিরিয়ড
          </span>
        </div>
      ),
    },
    {
      id: "teacher",
      header: "শিক্ষক",
      cell: (row) =>
        row.teacherName ? (
          <div className="flex flex-col">
            <span>{row.teacherName}</span>
            {row.teacherDesignation && (
              <span className="text-xs text-muted">
                {row.teacherDesignation}
              </span>
            )}
          </div>
        ) : (
          <Badge tone="warning">শিক্ষক নিয়োগ হয়নি</Badge>
        ),
    },
    {
      id: "assignments",
      header: "অ্যাসাইনমেন্ট",
      align: "right",
      hideOnMobile: true,
      cell: (row) => (
        <span className="tabular-nums text-muted">
          {bn(row.publishedAssignmentCount)}/{bn(row.assignmentCount)}
        </span>
      ),
    },
    {
      id: "actions",
      header: "",
      align: "right",
      cell: (row) => (
        <Button size="sm" variant="secondary" onClick={() => setStaffing(row)}>
          শিক্ষক বদল
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="কোর্স ও শিক্ষক"
        description="কোন শ্রেণিতে কোন বিষয় কে পড়াবেন। শিক্ষক কেবল নিজের কোর্সেই কাজ দিতে পারেন।"
        action={
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            নতুন কোর্স
          </Button>
        }
      />

      <Card>
        <div className="flex flex-wrap items-center gap-3 border-b border-border p-3">
          <Select
            aria-label="শ্রেণি অনুযায়ী ছাঁকুন"
            value={classRoomId}
            onChange={(e) => {
              setClassRoomId(e.target.value);
              setPage(1);
            }}
            className="w-full sm:w-56"
          >
            <option value="">সব শ্রেণি</option>
            {classes.data?.items.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>

          <Select
            aria-label="শিক্ষক অনুযায়ী ছাঁকুন"
            value={teacherId}
            onChange={(e) => {
              setTeacherId(e.target.value);
              setPage(1);
            }}
            className="w-full sm:w-64"
          >
            <option value="">সব শিক্ষক</option>
            {teachers.data?.items.map((t) => (
              <option key={t.id} value={t.id}>
                {t.fullName}
              </option>
            ))}
          </Select>
        </div>

        <DataTable
          columns={columns}
          rows={data?.items}
          keyOf={(row) => row.id}
          loading={isPending}
          error={error}
          onRetry={refetch}
          emptyTitle="কোনো কোর্স নেই"
          emptyDescription="শ্রেণি ও বিষয় মিলিয়ে কোর্স তৈরি করুন, তারপর শিক্ষক নিয়োগ দিন।"
        />

        {data && data.totalPages > 1 && (
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            hasPrevious={data.hasPrevious}
            hasNext={data.hasNext}
            onPageChange={setPage}
          />
        )}
      </Card>

      {creating && <CreateCourseModal onClose={() => setCreating(false)} />}

      {staffing && (
        <StaffCourseModal course={staffing} onClose={() => setStaffing(null)} />
      )}
    </>
  );
}

const createSchema = z.object({
  classRoomId: z.string().min(1, "শ্রেণি নির্বাচন করুন"),
  subjectId: z.string().min(1, "বিষয় নির্বাচন করুন"),
  teacherId: z.string(),
});

function CreateCourseModal({ onClose }: { onClose: () => void }) {
  const classes = useAllClasses();
  const subjects = useAllSubjects();
  const teachers = useUsersByRole("Teacher");
  const create = useCreateCourse();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<z.infer<typeof createSchema>>({
    resolver: zodResolver(createSchema),
    defaultValues: { classRoomId: "", subjectId: "", teacherId: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      const course = await create.mutateAsync({
        classRoomId: values.classRoomId,
        subjectId: values.subjectId,
        teacherId: values.teacherId || null,
      });
      toast.success(`${course.code} কোর্সটি তৈরি হয়েছে`);
      onClose();
    } catch (err) {
      toast.error(describeError(err));
    }
  });

  return (
    <Modal
      open
      onClose={onClose}
      title="নতুন কোর্স"
      description="কোর্স কোড শ্রেণি ও বোর্ড বিষয় কোড থেকে নিজে থেকেই তৈরি হয় — যেমন C06-109।"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button type="submit" form="create-course" loading={create.isPending}>
            তৈরি করুন
          </Button>
        </>
      }
    >
      <form
        id="create-course"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <Field
          label="শ্রেণি"
          htmlFor="classRoomId"
          required
          error={errors.classRoomId?.message}
        >
          <Select
            id="classRoomId"
            invalid={!!errors.classRoomId}
            disabled={classes.isPending}
            {...register("classRoomId")}
          >
            <option value="">শ্রেণি নির্বাচন করুন</option>
            {classes.data?.items.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label="বিষয়"
          htmlFor="subjectId"
          required
          error={errors.subjectId?.message}
        >
          <Select
            id="subjectId"
            invalid={!!errors.subjectId}
            disabled={subjects.isPending}
            {...register("subjectId")}
          >
            <option value="">বিষয় নির্বাচন করুন</option>
            {subjects.data?.items.map((s) => (
              <option key={s.id} value={s.id}>
                {s.code} — {s.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label="শিক্ষক"
          htmlFor="teacherId"
          hint="পরেও নিয়োগ দেওয়া যাবে।"
          error={errors.teacherId?.message}
        >
          <Select
            id="teacherId"
            disabled={teachers.isPending}
            {...register("teacherId")}
          >
            <option value="">এখন নয়</option>
            {teachers.data?.items.map((t) => (
              <option key={t.id} value={t.id}>
                {t.fullName}
                {t.designation ? ` — ${t.designation}` : ""}
              </option>
            ))}
          </Select>
        </Field>
      </form>
    </Modal>
  );
}

function StaffCourseModal({
  course,
  onClose,
}: {
  course: CourseDto;
  onClose: () => void;
}) {
  const teachers = useUsersByRole("Teacher");
  const assign = useAssignCourseTeacher();
  const [teacherId, setTeacherId] = useState(course.teacherId ?? "");

  async function save() {
    try {
      await assign.mutateAsync({
        id: course.id,
        body: { teacherId: teacherId || null },
      });
      toast.success("শিক্ষক হালনাগাদ হয়েছে");
      onClose();
    } catch (err) {
      // 409 when the outgoing teacher already set work on the course.
      toast.error(describeError(err));
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      title={`${course.code} — শিক্ষক নিয়োগ`}
      description={`${course.classRoomName} · ${course.subjectName}`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button onClick={save} loading={assign.isPending}>
            সংরক্ষণ
          </Button>
        </>
      }
    >
      <Field
        label="শিক্ষক"
        htmlFor="staff-teacher"
        hint="বর্তমান শিক্ষক এই কোর্সে অ্যাসাইনমেন্ট দিয়ে থাকলে পরিবর্তন করা যাবে না — তাহলে তিনি নিজের দেওয়া কাজের মূল্যায়ন করতে পারতেন না।"
      >
        <Select
          id="staff-teacher"
          value={teacherId}
          onChange={(e) => setTeacherId(e.target.value)}
          disabled={teachers.isPending}
        >
          <option value="">কেউ নয়</option>
          {teachers.data?.items.map((t) => (
            <option key={t.id} value={t.id}>
              {t.fullName}
              {t.designation ? ` — ${t.designation}` : ""}
            </option>
          ))}
        </Select>
      </Field>
    </Modal>
  );
}
