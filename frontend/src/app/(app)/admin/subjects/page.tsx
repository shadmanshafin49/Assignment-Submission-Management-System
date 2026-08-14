"use client";

import { PageHeader } from "@/components/app-shell";
import { Badge, SubjectChip } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Field, Input, Select } from "@/components/ui/field";
import { ConfirmDialog, Modal } from "@/components/ui/modal";
import {
  useCreateSubject,
  useDeleteSubject,
  useSubjects,
  useUpdateSubject,
} from "@/hooks/use-admin";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { AssignmentType, FaithGroup, SubjectDto } from "@/lib/types";
import { bn } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

export default function AdminSubjectsPage() {
  const labels = useLabels();
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<SubjectDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState<SubjectDto | null>(null);

  const { data, isPending, error, refetch } = useSubjects({ page, pageSize: 30 });
  const remove = useDeleteSubject();

  const columns: Column<SubjectDto>[] = [
    {
      id: "name",
      header: "বিষয়",
      primary: true,
      cell: (row) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{row.name}</span>
          {row.textbookName && (
            <span className="text-xs text-muted">{row.textbookName}</span>
          )}
        </div>
      ),
    },
    {
      id: "code",
      header: "কোড",
      cell: (row) => <SubjectChip code={row.code} name={row.code} />,
    },
    {
      id: "marks",
      header: "পূর্ণমান",
      align: "right",
      cell: (row) => (
        <span className="tabular-nums">{bn(row.fullMarks)}</span>
      ),
    },
    {
      id: "periods",
      header: "সাপ্তাহিক পিরিয়ড",
      align: "right",
      hideOnMobile: true,
      cell: (row) => (
        <span className="tabular-nums">{bn(row.weeklyPeriods)}</span>
      ),
    },
    {
      id: "group",
      header: "ধরন",
      cell: (row) => (
        <div className="flex flex-col items-start gap-1">
          {row.faithGroup && (
            <Badge tone="info">{labels.faith(row.faithGroup)}</Badge>
          )}
          {row.isOptionalGroup && <Badge tone="neutral">ঐচ্ছিক গ্রুপ</Badge>}
          {!row.isActive && <Badge tone="neutral">নিষ্ক্রিয়</Badge>}
        </div>
      ),
    },
    {
      id: "types",
      header: "কাজের ধরন",
      hideOnMobile: true,
      cell: (row) => (
        <span className="text-xs text-muted">
          {bn(row.allowedAssignmentTypes.length)} ধরনের
        </span>
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
      toast.success("বিষয় মুছে ফেলা হয়েছে");
      setDeleting(null);
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <>
      <PageHeader
        title="বিষয়"
        description="এনসিটিবির বিষয় ও বোর্ড কোড, সাপ্তাহিক পিরিয়ড এবং যে ধরনের কাজ এই বিষয়ে দেওয়া যায়।"
        action={
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            নতুন বিষয়
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
          emptyTitle="কোনো বিষয় নেই"
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
        <SubjectModal
          subject={editing}
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
        title="বিষয়টি মুছে ফেলবেন?"
        message={`"${deleting?.name ?? ""}" মুছে যাবে। এই বিষয়ে কোর্স থাকলে সার্ভার মুছতে দেবে না।`}
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
    .min(1, "বোর্ড কোড দিন")
    .max(20)
    .regex(/^[A-Za-z0-9-]+$/, "কেবল ইংরেজি অক্ষর, সংখ্যা ও হাইফেন"),
  textbookName: z.string().trim().max(150).optional(),
  fullMarks: z.coerce.number().int().min(1, "পূর্ণমান অন্তত ১").max(1000),
  weeklyPeriods: z.coerce.number().int().min(0).max(12),
  faithGroup: z.string().optional(),
  isOptionalGroup: z.boolean(),
  displayOrder: z.coerce.number().int().min(0).max(100),
  isActive: z.boolean(),
});

function SubjectModal({
  subject,
  onClose,
}: {
  subject: SubjectDto | null;
  onClose: () => void;
}) {
  const isEditing = !!subject;
  const labels = useLabels();
  const create = useCreateSubject();
  const update = useUpdateSubject();

  // The allowed-type list is a set, not a field — a checkbox grid rather than
  // an input, and the API refuses to withdraw a type that existing work uses.
  const [types, setTypes] = useState<AssignmentType[]>(
    subject?.allowedAssignmentTypes ?? [],
  );

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<z.input<typeof schema>, unknown, z.output<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: subject?.name ?? "",
      nameEn: subject?.nameEn ?? "",
      code: subject?.code ?? "",
      textbookName: subject?.textbookName ?? "",
      fullMarks: subject?.fullMarks ?? 100,
      weeklyPeriods: subject?.weeklyPeriods ?? 3,
      faithGroup: subject?.faithGroup ?? "",
      isOptionalGroup: subject?.isOptionalGroup ?? false,
      displayOrder: subject?.displayOrder ?? 0,
      isActive: subject?.isActive ?? true,
    },
  });

  function toggleType(type: AssignmentType) {
    setTypes((prev) =>
      prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type],
    );
  }

  const onSubmit = handleSubmit(async (values) => {
    if (types.length === 0) {
      toast.error("অন্তত একটি কাজের ধরন নির্বাচন করুন");
      return;
    }

    const body = {
      name: values.name,
      nameEn: values.nameEn,
      code: values.code,
      textbookName: values.textbookName || null,
      fullMarks: values.fullMarks,
      weeklyPeriods: values.weeklyPeriods,
      faithGroup: (values.faithGroup || null) as FaithGroup | null,
      isOptionalGroup: values.isOptionalGroup,
      displayOrder: values.displayOrder,
      allowedAssignmentTypes: types,
    };

    try {
      if (isEditing) {
        await update.mutateAsync({
          id: subject.id,
          body: { ...body, isActive: values.isActive },
        });
        toast.success("বিষয় হালনাগাদ হয়েছে");
      } else {
        await create.mutateAsync(body);
        toast.success("বিষয় তৈরি হয়েছে");
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
      size="lg"
      title={isEditing ? "বিষয় সম্পাদনা" : "নতুন বিষয়"}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button
            type="submit"
            form="subject-form"
            loading={create.isPending || update.isPending}
          >
            {isEditing ? "পরিবর্তন সংরক্ষণ" : "তৈরি করুন"}
          </Button>
        </>
      }
    >
      <form
        id="subject-form"
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
              placeholder="গণিত"
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
              placeholder="Mathematics"
              invalid={!!errors.nameEn}
              {...register("nameEn")}
            />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-3">
          <Field
            label="বোর্ড কোড"
            htmlFor="code"
            required
            error={errors.code?.message}
            hint="যেমন ১০৯ = গণিত"
          >
            <Input
              id="code"
              dir="ltr"
              placeholder="109"
              invalid={!!errors.code}
              {...register("code")}
            />
          </Field>

          <Field
            label="পূর্ণমান"
            htmlFor="fullMarks"
            required
            error={errors.fullMarks?.message}
          >
            <Input
              id="fullMarks"
              type="number"
              min={1}
              max={1000}
              invalid={!!errors.fullMarks}
              {...register("fullMarks")}
            />
          </Field>

          <Field
            label="সাপ্তাহিক পিরিয়ড"
            htmlFor="weeklyPeriods"
            required
            error={errors.weeklyPeriods?.message}
          >
            <Input
              id="weeklyPeriods"
              type="number"
              min={0}
              max={12}
              invalid={!!errors.weeklyPeriods}
              {...register("weeklyPeriods")}
            />
          </Field>
        </div>

        <Field
          label="পাঠ্যবই"
          htmlFor="textbookName"
          error={errors.textbookName?.message}
          hint="যেমন: চারুপাঠ (ষষ্ঠ শ্রেণি)"
        >
          <Input id="textbookName" {...register("textbookName")} />
        </Field>

        <div className="grid gap-4 sm:grid-cols-3">
          <Field
            label="ধর্ম গ্রুপ"
            htmlFor="faithGroup"
            hint="ধর্ম শিক্ষার বিষয় হলে নির্বাচন করুন।"
          >
            <Select id="faithGroup" {...register("faithGroup")}>
              <option value="">প্রযোজ্য নয়</option>
              {labels.options.faithGroups.map((f) => (
                <option key={f.value} value={f.value}>
                  {f.label}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="ঐচ্ছিক গ্রুপ" htmlFor="isOptionalGroup">
            <Select
              id="isOptionalGroup"
              {...register("isOptionalGroup", {
                setValueAs: (v) => v === "true",
              })}
            >
              <option value="false">না</option>
              <option value="true">হ্যাঁ</option>
            </Select>
          </Field>

          <Field
            label="ক্রম"
            htmlFor="displayOrder"
            error={errors.displayOrder?.message}
            hint="তালিকায় সাজানোর ক্রম।"
          >
            <Input
              id="displayOrder"
              type="number"
              min={0}
              max={100}
              {...register("displayOrder")}
            />
          </Field>
        </div>

        <fieldset className="flex flex-col gap-2">
          <legend className="text-sm font-medium">
            যেসব ধরনের কাজ এই বিষয়ে দেওয়া যাবে
          </legend>
          <p className="text-xs text-muted">
            শিক্ষক এখানকার বাইরের কোনো ধরন বেছে নিতে পারবেন না — এনসিটিবির
            বিষয়ভিত্তিক প্রশ্নের ধরন অনুসারে ঠিক করুন।
          </p>
          <div className="grid gap-1.5 sm:grid-cols-2">
            {labels.options.assignmentTypes.map((option) => {
              const value = option.value as AssignmentType;
              return (
                <label
                  key={option.value}
                  className="flex cursor-pointer items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm hover:bg-surface-muted"
                >
                  <input
                    type="checkbox"
                    className="size-4 accent-[var(--primary)]"
                    checked={types.includes(value)}
                    onChange={() => toggleType(value)}
                  />
                  {option.label}
                </label>
              );
            })}
          </div>
        </fieldset>

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
