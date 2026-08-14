"use client";

import { PageHeader } from "@/components/app-shell";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Field, Input, Select } from "@/components/ui/field";
import { ConfirmDialog, Modal } from "@/components/ui/modal";
import {
  useClasses,
  useCreateClass,
  useDeleteClass,
  useUpdateClass,
} from "@/hooks/use-admin";
import { describeError } from "@/lib/api-client";
import type { ClassRoomDto } from "@/lib/types";
import { bn } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

export default function AdminClassesPage() {
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<ClassRoomDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState<ClassRoomDto | null>(null);

  const { data, isPending, error, refetch } = useClasses({ page });
  const remove = useDeleteClass();

  const columns: Column<ClassRoomDto>[] = [
    {
      id: "name",
      header: "শ্রেণি",
      primary: true,
      cell: (row) => (
        <div className="flex flex-col">
          <span className="font-medium">{row.name}</span>
          <span className="text-xs text-muted" dir="ltr">
            {row.nameEn}
          </span>
        </div>
      ),
    },
    {
      id: "code",
      header: "কোড",
      cell: (row) => (
        <span className="rounded bg-surface-muted px-1.5 py-0.5 font-mono text-xs">
          {row.code}
          {row.section ? `-${row.section}` : ""}
        </span>
      ),
    },
    {
      id: "year",
      header: "শিক্ষাবর্ষ",
      cell: (row) => bn(row.academicYear),
    },
    {
      id: "students",
      header: "শিক্ষার্থী",
      align: "right",
      cell: (row) => (
        <span className="tabular-nums">{bn(row.studentCount)}</span>
      ),
    },
    {
      id: "courses",
      header: "কোর্স",
      align: "right",
      hideOnMobile: true,
      cell: (row) => <span className="tabular-nums">{bn(row.courseCount)}</span>,
    },
    {
      id: "active",
      header: "অবস্থা",
      cell: (row) => (
        <Badge tone={row.isActive ? "success" : "neutral"}>
          {row.isActive ? "সক্রিয়" : "নিষ্ক্রিয়"}
        </Badge>
      ),
    },
    {
      id: "actions",
      header: "",
      align: "right",
      cell: (row) => (
        <div className="flex justify-end gap-2">
          <Button size="sm" variant="secondary" onClick={() => setEditing(row)}>
            সম্পাদনা
          </Button>
          <Button size="sm" variant="ghost" onClick={() => setDeleting(row)}>
            মুছুন
          </Button>
        </div>
      ),
    },
  ];

  async function confirmDelete() {
    if (!deleting) return;
    try {
      await remove.mutateAsync(deleting.id);
      toast.success("শ্রেণি মুছে ফেলা হয়েছে");
      setDeleting(null);
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <>
      <PageHeader
        title="শ্রেণি"
        description="ষষ্ঠ থেকে অষ্টম — যেসব শ্রেণিতে শিক্ষার্থী ভর্তি হয় ও কোর্স চলে।"
        action={
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            নতুন শ্রেণি
          </Button>
        }
      />

      <Card>
        <DataTable
          columns={columns}
          rows={data?.items}
          keyOf={(row) => row.id}
          loading={isPending}
          error={error}
          onRetry={refetch}
          emptyTitle="কোনো শ্রেণি নেই"
          emptyDescription="শিক্ষার্থী ভর্তির আগে শ্রেণি তৈরি করুন।"
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

      {(creating || editing) && (
        <ClassModal
          classRoom={editing}
          onClose={() => {
            setCreating(false);
            setEditing(null);
          }}
        />
      )}

      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={confirmDelete}
        loading={remove.isPending}
        destructive
        title="শ্রেণিটি মুছে ফেলবেন?"
        message={`"${deleting?.name ?? ""}" মুছে যাবে। শ্রেণিতে কোর্স থাকলে সার্ভার এটি মুছতে দেবে না।`}
        confirmLabel="মুছে ফেলুন"
      />
    </>
  );
}

const schema = z.object({
  name: z.string().trim().min(2, "নাম দিন").max(120, "নাম অনেক বড়"),
  nameEn: z.string().trim().min(2, "ইংরেজি নাম দিন").max(120),
  code: z
    .string()
    .trim()
    .min(1, "কোড দিন")
    .max(20, "কোড অনেক বড়")
    .regex(/^[A-Za-z0-9-]+$/, "কেবল ইংরেজি অক্ষর, সংখ্যা ও হাইফেন"),
  level: z.coerce
    .number()
    .int("শ্রেণি পূর্ণসংখ্যা হতে হবে")
    .min(1, "শ্রেণি অন্তত ১")
    .max(12, "শ্রেণি সর্বোচ্চ ১২"),
  section: z.string().trim().max(10).optional(),
  academicYear: z.string().trim().min(4, "শিক্ষাবর্ষ দিন").max(20),
  isActive: z.boolean(),
});

function ClassModal({
  classRoom,
  onClose,
}: {
  classRoom: ClassRoomDto | null;
  onClose: () => void;
}) {
  const isEditing = !!classRoom;
  const create = useCreateClass();
  const update = useUpdateClass();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<z.input<typeof schema>, unknown, z.output<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: classRoom?.name ?? "",
      nameEn: classRoom?.nameEn ?? "",
      code: classRoom?.code ?? "",
      level: classRoom?.level ?? 6,
      section: classRoom?.section ?? "",
      academicYear: classRoom?.academicYear ?? String(new Date().getFullYear()),
      isActive: classRoom?.isActive ?? true,
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    const body = {
      name: values.name,
      nameEn: values.nameEn,
      code: values.code,
      level: values.level,
      section: values.section || null,
      academicYear: values.academicYear,
    };

    try {
      if (isEditing) {
        await update.mutateAsync({
          id: classRoom.id,
          body: { ...body, isActive: values.isActive },
        });
        toast.success("শ্রেণি হালনাগাদ হয়েছে");
      } else {
        await create.mutateAsync(body);
        toast.success("শ্রেণি তৈরি হয়েছে");
      }
      onClose();
    } catch (err) {
      toast.error(describeError(err));
    }
  });

  return (
    <Modal
      open
      onClose={onClose}
      title={isEditing ? "শ্রেণি সম্পাদনা" : "নতুন শ্রেণি"}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button
            type="submit"
            form="class-form"
            loading={create.isPending || update.isPending}
          >
            {isEditing ? "পরিবর্তন সংরক্ষণ" : "তৈরি করুন"}
          </Button>
        </>
      }
    >
      <form
        id="class-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <Field
            label="নাম"
            htmlFor="name"
            required
            error={errors.name?.message}
          >
            <Input
              id="name"
              placeholder="ষষ্ঠ শ্রেণি"
              invalid={!!errors.name}
              {...register("name")}
            />
          </Field>

          <Field
            label="নাম (ইংরেজি)"
            htmlFor="nameEn"
            required
            error={errors.nameEn?.message}
          >
            <Input
              id="nameEn"
              dir="ltr"
              placeholder="Class 6"
              invalid={!!errors.nameEn}
              {...register("nameEn")}
            />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-3">
          <Field
            label="কোড"
            htmlFor="code"
            required
            error={errors.code?.message}
            hint="কোর্স কোডের ভিত্তি — C06"
          >
            <Input
              id="code"
              dir="ltr"
              placeholder="C06"
              invalid={!!errors.code}
              {...register("code")}
            />
          </Field>

          <Field
            label="শ্রেণি নম্বর"
            htmlFor="level"
            required
            error={errors.level?.message}
          >
            <Input
              id="level"
              type="number"
              min={1}
              max={12}
              invalid={!!errors.level}
              {...register("level")}
            />
          </Field>

          <Field label="শাখা" htmlFor="section" error={errors.section?.message}>
            <Input id="section" placeholder="ক" {...register("section")} />
          </Field>
        </div>

        <Field
          label="শিক্ষাবর্ষ"
          htmlFor="academicYear"
          required
          error={errors.academicYear?.message}
        >
          <Input
            id="academicYear"
            dir="ltr"
            placeholder="2026"
            invalid={!!errors.academicYear}
            {...register("academicYear")}
          />
        </Field>

        {isEditing && (
          <Field label="অবস্থা" htmlFor="isActive">
            <Select
              id="isActive"
              {...register("isActive", { setValueAs: (v) => v === "true" })}
            >
              <option value="true">সক্রিয়</option>
              <option value="false">নিষ্ক্রিয়</option>
            </Select>
          </Field>
        )}
      </form>
    </Modal>
  );
}
