"use client";

import { PageHeader } from "@/components/app-shell";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Field, Input, Select } from "@/components/ui/field";
import { ConfirmDialog, Modal } from "@/components/ui/modal";
import {
  useAllClasses,
  useCreateEnrollment,
  useDeleteEnrollment,
  useEnrollments,
  useUsersByRole,
} from "@/hooks/use-admin";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { EnrollmentDto } from "@/lib/types";
import { bn } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

export default function AdminEnrollmentsPage() {
  const labels = useLabels();
  const [page, setPage] = useState(1);
  const [classRoomId, setClassRoomId] = useState("");
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState<EnrollmentDto | null>(null);

  const classes = useAllClasses();
  const { data, isPending, error, refetch } = useEnrollments({
    page,
    pageSize: 30,
    classRoomId: classRoomId || undefined,
  });
  const remove = useDeleteEnrollment();

  const columns: Column<EnrollmentDto>[] = [
    {
      id: "roll",
      header: "রোল",
      cell: (row) => (
        <span className="flex size-8 items-center justify-center rounded-full bg-surface-muted text-sm font-semibold tabular-nums">
          {bn(row.rollNumber)}
        </span>
      ),
    },
    {
      id: "student",
      header: "শিক্ষার্থী",
      primary: true,
      cell: (row) => (
        <div className="flex flex-col">
          <span className="font-medium">{row.studentName}</span>
          <span className="text-xs text-muted" dir="ltr">
            {row.studentEmail}
          </span>
        </div>
      ),
    },
    { id: "class", header: "শ্রেণি", cell: (row) => row.classRoomName },
    {
      id: "faith",
      header: "ধর্ম",
      hideOnMobile: true,
      cell: (row) => (
        <span className="text-muted">{labels.faith(row.faith)}</span>
      ),
    },
    {
      id: "actions",
      header: "",
      align: "right",
      cell: (row) => (
        <Button size="sm" variant="ghost" onClick={() => setDeleting(row)}>
          বাতিল
        </Button>
      ),
    },
  ];

  async function confirmDelete() {
    if (!deleting) return;
    try {
      await remove.mutateAsync(deleting.id);
      toast.success("ভর্তি বাতিল হয়েছে");
      setDeleting(null);
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <>
      <PageHeader
        title="ভর্তি"
        description="কোন শিক্ষার্থী কোন শ্রেণিতে, কোন রোলে। অ্যাসাইনমেন্ট এই সূত্রেই শিক্ষার্থীর কাছে পৌঁছায়।"
        action={
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            শিক্ষার্থী ভর্তি
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
            className="w-full sm:w-64"
          >
            <option value="">সব শ্রেণি</option>
            {classes.data?.items.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
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
          emptyTitle="কোনো ভর্তি নেই"
          emptyDescription="শিক্ষার্থীকে শ্রেণিতে ভর্তি করলে সে ঐ শ্রেণির কাজ দেখতে পাবে।"
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

      {creating && <EnrollModal onClose={() => setCreating(false)} />}

      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={confirmDelete}
        loading={remove.isPending}
        destructive
        title="ভর্তি বাতিল করবেন?"
        message={`${deleting?.studentName ?? "এই শিক্ষার্থী"} ${deleting?.classRoomName ?? "এই শ্রেণির"} কাজ আর দেখতে পাবে না। এই শ্রেণিতে জমা দেওয়া থাকলে সার্ভার বাতিল করতে দেবে না।`}
        confirmLabel="বাতিল করুন"
      />
    </>
  );
}

const schema = z.object({
  studentId: z.string().min(1, "শিক্ষার্থী নির্বাচন করুন"),
  classRoomId: z.string().min(1, "শ্রেণি নির্বাচন করুন"),
  rollNumber: z.coerce
    .number()
    .int("রোল পূর্ণসংখ্যা হতে হবে")
    .min(1, "রোল অন্তত ১")
    .max(999, "রোল অনেক বড়"),
});

function EnrollModal({ onClose }: { onClose: () => void }) {
  const students = useUsersByRole("Student");
  const classes = useAllClasses();
  const create = useCreateEnrollment();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<z.input<typeof schema>, unknown, z.output<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: { studentId: "", classRoomId: "", rollNumber: 1 },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await create.mutateAsync(values);
      toast.success("ভর্তি সম্পন্ন হয়েছে");
      onClose();
    } catch (err) {
      // A duplicate (student, class) pair or a taken roll number is rejected
      // by a unique index; the API's own wording says which.
      toast.error(describeError(err));
    }
  });

  return (
    <Modal
      open
      onClose={onClose}
      title="শিক্ষার্থী ভর্তি"
      description="শিক্ষার্থীর ধর্ম নির্ধারিত না থাকলে ভর্তি করা যাবে না — ধর্ম ও নৈতিক শিক্ষার কোর্স সেটির উপরই নির্ভর করে।"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button type="submit" form="enroll-form" loading={create.isPending}>
            ভর্তি করুন
          </Button>
        </>
      }
    >
      <form
        id="enroll-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <Field
          label="শিক্ষার্থী"
          htmlFor="studentId"
          required
          error={errors.studentId?.message}
        >
          <Select
            id="studentId"
            invalid={!!errors.studentId}
            disabled={students.isPending}
            {...register("studentId")}
          >
            <option value="">
              {students.isPending ? "লোড হচ্ছে…" : "শিক্ষার্থী নির্বাচন করুন"}
            </option>
            {students.data?.items.map((s) => (
              <option key={s.id} value={s.id}>
                {s.fullName} — {s.email}
              </option>
            ))}
          </Select>
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
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
              <option value="">
                {classes.isPending ? "লোড হচ্ছে…" : "শ্রেণি নির্বাচন করুন"}
              </option>
              {classes.data?.items.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </Select>
          </Field>

          <Field
            label="রোল নম্বর"
            htmlFor="rollNumber"
            required
            error={errors.rollNumber?.message}
            hint="একই শ্রেণিতে দুজনের রোল এক হতে পারে না।"
          >
            <Input
              id="rollNumber"
              type="number"
              min={1}
              max={999}
              invalid={!!errors.rollNumber}
              {...register("rollNumber")}
            />
          </Field>
        </div>
      </form>
    </Modal>
  );
}
