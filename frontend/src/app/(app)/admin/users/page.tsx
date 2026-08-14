"use client";

import { PageHeader } from "@/components/app-shell";
import { Badge, RoleBadge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, Pagination, type Column } from "@/components/ui/data-table";
import { Field, Input, Select } from "@/components/ui/field";
import { ConfirmDialog, Modal } from "@/components/ui/modal";
import {
  useCreateUser,
  useDeactivateUser,
  useUpdateUser,
  useUsers,
} from "@/hooks/use-admin";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { FaithGroup, UserDto, UserRole } from "@/lib/types";
import { formatDate } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus, Search } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

export default function AdminUsersPage() {
  const labels = useLabels();
  const [page, setPage] = useState(1);
  const [role, setRole] = useState<UserRole | "">("");
  const [search, setSearch] = useState("");

  const [editing, setEditing] = useState<UserDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [deactivating, setDeactivating] = useState<UserDto | null>(null);

  const { data, isPending, error, refetch } = useUsers({
    page,
    role,
    search: search.trim() || undefined,
  });
  const deactivate = useDeactivateUser();

  const columns: Column<UserDto>[] = [
    {
      id: "name",
      header: "নাম",
      primary: true,
      cell: (row) => (
        <div className="flex flex-col">
          <span className="font-medium">{row.fullName}</span>
          <span className="text-xs text-muted" dir="ltr">
            {row.email}
          </span>
        </div>
      ),
    },
    {
      id: "role",
      header: "ভূমিকা",
      cell: (row) => (
        <div className="flex flex-col items-start gap-1">
          <RoleBadge role={row.role} />
          {row.designation && (
            <span className="text-xs text-muted">{row.designation}</span>
          )}
          {row.faith && (
            <span className="text-xs text-muted">{labels.faith(row.faith)}</span>
          )}
        </div>
      ),
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
      id: "created",
      header: "যোগ হয়েছে",
      hideOnMobile: true,
      cell: (row) => (
        <span className="text-muted">{formatDate(row.createdAt)}</span>
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
          {row.isActive && (
            <Button
              size="sm"
              variant="ghost"
              onClick={() => setDeactivating(row)}
            >
              নিষ্ক্রিয়
            </Button>
          )}
        </div>
      ),
    },
  ];

  async function confirmDeactivate() {
    if (!deactivating) return;
    try {
      await deactivate.mutateAsync(deactivating.id);
      toast.success(`${deactivating.fullName} নিষ্ক্রিয় করা হয়েছে`);
      setDeactivating(null);
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <>
      <PageHeader
        title="ব্যবহারকারী"
        description="শিক্ষক, শিক্ষার্থী ও প্রশাসকের অ্যাকাউন্ট তৈরি ও পরিচালনা।"
        action={
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            নতুন ব্যবহারকারী
          </Button>
        }
      />

      <Card>
        <div className="flex flex-wrap items-center gap-3 border-b border-border p-3">
          <Select
            aria-label="ভূমিকা অনুযায়ী ছাঁকুন"
            value={role}
            onChange={(e) => {
              setRole(e.target.value as UserRole | "");
              setPage(1);
            }}
            className="w-full sm:w-44"
          >
            <option value="">সব ভূমিকা</option>
            {labels.options.roles.map((r) => (
              <option key={r.value} value={r.value}>
                {r.label}
              </option>
            ))}
          </Select>

          <div className="relative w-full sm:w-64">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" />
            <Input
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              placeholder="নাম বা ইমেইল খুঁজুন…"
              aria-label="ব্যবহারকারী খুঁজুন"
              className="pl-9"
            />
          </div>
        </div>

        <DataTable
          columns={columns}
          rows={data?.items}
          keyOf={(row) => row.id}
          loading={isPending}
          error={error}
          onRetry={refetch}
          emptyTitle="কোনো ব্যবহারকারী পাওয়া যায়নি"
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

      {creating && <CreateUserModal onClose={() => setCreating(false)} />}
      {editing && (
        <EditUserModal user={editing} onClose={() => setEditing(null)} />
      )}

      <ConfirmDialog
        open={!!deactivating}
        onClose={() => setDeactivating(null)}
        onConfirm={confirmDeactivate}
        loading={deactivate.isPending}
        destructive
        title="অ্যাকাউন্টটি নিষ্ক্রিয় করবেন?"
        message={`${deactivating?.fullName ?? "এই ব্যবহারকারী"} আর সাইন ইন করতে পারবেন না। তাঁর পুরোনো রেকর্ড মুছে যাবে না।`}
        confirmLabel="নিষ্ক্রিয় করুন"
      />
    </>
  );
}

const createSchema = z.object({
  fullName: z.string().trim().min(2, "নাম দিন").max(150, "নাম অনেক বড়"),
  fullNameEn: z
    .string()
    .trim()
    .min(2, "ইংরেজি নাম দিন")
    .max(150, "নাম অনেক বড়"),
  email: z.string().trim().min(1, "ইমেইল দিন").email("সঠিক ইমেইল দিন"),
  password: z
    .string()
    .min(8, "পাসওয়ার্ড অন্তত ৮ অক্ষরের হতে হবে")
    .regex(/[A-Z]/, "অন্তত একটি বড় হাতের অক্ষর দিন")
    .regex(/[a-z]/, "অন্তত একটি ছোট হাতের অক্ষর দিন")
    .regex(/[0-9]/, "অন্তত একটি সংখ্যা দিন"),
  role: z.enum(["Admin", "Teacher", "Student"]),
  designation: z.string().trim().max(100).optional(),
  faith: z.string().optional(),
});

function CreateUserModal({ onClose }: { onClose: () => void }) {
  const labels = useLabels();
  const create = useCreateUser();
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<z.infer<typeof createSchema>>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      fullName: "",
      fullNameEn: "",
      email: "",
      password: "",
      role: "Student",
      designation: "",
      faith: "",
    },
  });

  const role = watch("role");

  const onSubmit = handleSubmit(async (values) => {
    try {
      await create.mutateAsync({
        fullName: values.fullName,
        fullNameEn: values.fullNameEn,
        email: values.email,
        password: values.password,
        role: values.role,
        // Designation belongs to staff, faith to students — the API refuses the
        // other combinations, so they are cleared rather than sent and rejected.
        designation: values.role === "Student" ? null : values.designation || null,
        faith:
          values.role === "Student"
            ? ((values.faith || null) as FaithGroup | null)
            : null,
      });
      toast.success("অ্যাকাউন্ট তৈরি হয়েছে");
      onClose();
    } catch (err) {
      toast.error(describeError(err));
    }
  });

  return (
    <Modal
      open
      onClose={onClose}
      title="নতুন ব্যবহারকারী"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button type="submit" form="create-user" loading={create.isPending}>
            তৈরি করুন
          </Button>
        </>
      }
    >
      <form
        id="create-user"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <Field
          label="ভূমিকা"
          htmlFor="role"
          required
          error={errors.role?.message}
        >
          <Select id="role" invalid={!!errors.role} {...register("role")}>
            {labels.options.roles.map((r) => (
              <option key={r.value} value={r.value}>
                {r.label}
              </option>
            ))}
          </Select>
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field
            label="পূর্ণ নাম (বাংলা)"
            htmlFor="fullName"
            required
            error={errors.fullName?.message}
          >
            <Input
              id="fullName"
              placeholder="মোঃ রেজাউল করিম"
              invalid={!!errors.fullName}
              {...register("fullName")}
            />
          </Field>

          <Field
            label="নাম (ইংরেজি)"
            htmlFor="fullNameEn"
            required
            error={errors.fullNameEn?.message}
            hint="সনদ ও বোর্ডের কাগজপত্রের জন্য।"
          >
            <Input
              id="fullNameEn"
              dir="ltr"
              placeholder="Md Rejaul Karim"
              invalid={!!errors.fullNameEn}
              {...register("fullNameEn")}
            />
          </Field>
        </div>

        <Field
          label="ইমেইল"
          htmlFor="email"
          required
          error={errors.email?.message}
          hint="সাইন ইনের পরিচয় — পরে পরিবর্তন করা যাবে না।"
        >
          <Input
            id="email"
            type="email"
            dir="ltr"
            invalid={!!errors.email}
            {...register("email")}
          />
        </Field>

        <Field
          label="সাময়িক পাসওয়ার্ড"
          htmlFor="password"
          required
          error={errors.password?.message}
          hint="অন্তত ৮ অক্ষর, বড় ও ছোট হাতের অক্ষর এবং সংখ্যাসহ।"
        >
          <Input
            id="password"
            type="text"
            dir="ltr"
            invalid={!!errors.password}
            {...register("password")}
          />
        </Field>

        {role === "Student" ? (
          <Field
            label="ধর্ম"
            htmlFor="faith"
            hint="ধর্ম ও নৈতিক শিক্ষার কোন কোর্সটি পড়বে তা এখান থেকেই নির্ধারিত হয়।"
            error={errors.faith?.message}
          >
            <Select id="faith" {...register("faith")}>
              <option value="">নির্বাচন করুন</option>
              {labels.options.faithGroups.map((f) => (
                <option key={f.value} value={f.value}>
                  {f.label}
                </option>
              ))}
            </Select>
          </Field>
        ) : (
          <Field
            label="পদবি"
            htmlFor="designation"
            error={errors.designation?.message}
            hint="যেমন: সহকারী শিক্ষক (গণিত)"
          >
            <Input
              id="designation"
              invalid={!!errors.designation}
              {...register("designation")}
            />
          </Field>
        )}
      </form>
    </Modal>
  );
}

const editSchema = z.object({
  fullName: z.string().trim().min(2, "নাম দিন").max(150, "নাম অনেক বড়"),
  fullNameEn: z.string().trim().min(2, "ইংরেজি নাম দিন").max(150),
  role: z.enum(["Admin", "Teacher", "Student"]),
  designation: z.string().trim().max(100).optional(),
  faith: z.string().optional(),
  isActive: z.boolean(),
});

function EditUserModal({
  user,
  onClose,
}: {
  user: UserDto;
  onClose: () => void;
}) {
  const labels = useLabels();
  const update = useUpdateUser();
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<z.infer<typeof editSchema>>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      fullName: user.fullName,
      fullNameEn: user.fullNameEn,
      role: user.role,
      designation: user.designation ?? "",
      faith: user.faith ?? "",
      isActive: user.isActive,
    },
  });

  const role = watch("role");

  const onSubmit = handleSubmit(async (values) => {
    try {
      await update.mutateAsync({
        id: user.id,
        body: {
          fullName: values.fullName,
          fullNameEn: values.fullNameEn,
          role: values.role,
          designation:
            values.role === "Student" ? null : values.designation || null,
          faith:
            values.role === "Student"
              ? ((values.faith || null) as FaithGroup | null)
              : null,
          isActive: values.isActive,
        },
      });
      toast.success("হালনাগাদ হয়েছে");
      onClose();
    } catch (err) {
      toast.error(describeError(err));
    }
  });

  return (
    <Modal
      open
      onClose={onClose}
      title="ব্যবহারকারী সম্পাদনা"
      description={user.email}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button type="submit" form="edit-user" loading={update.isPending}>
            পরিবর্তন সংরক্ষণ
          </Button>
        </>
      }
    >
      <form
        id="edit-user"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <Field
            label="পূর্ণ নাম (বাংলা)"
            htmlFor="editName"
            required
            error={errors.fullName?.message}
          >
            <Input
              id="editName"
              invalid={!!errors.fullName}
              {...register("fullName")}
            />
          </Field>

          <Field
            label="নাম (ইংরেজি)"
            htmlFor="editNameEn"
            required
            error={errors.fullNameEn?.message}
          >
            <Input
              id="editNameEn"
              dir="ltr"
              invalid={!!errors.fullNameEn}
              {...register("fullNameEn")}
            />
          </Field>
        </div>

        <Field
          label="ভূমিকা"
          htmlFor="editRole"
          required
          error={errors.role?.message}
        >
          <Select id="editRole" invalid={!!errors.role} {...register("role")}>
            {labels.options.roles.map((r) => (
              <option key={r.value} value={r.value}>
                {r.label}
              </option>
            ))}
          </Select>
        </Field>

        {role === "Student" ? (
          <Field
            label="ধর্ম"
            htmlFor="editFaith"
            hint="শ্রেণিতে ভর্তি থাকা শিক্ষার্থীর ধর্ম পরিবর্তন করা যায় না।"
          >
            <Select id="editFaith" {...register("faith")}>
              <option value="">নির্বাচন করুন</option>
              {labels.options.faithGroups.map((f) => (
                <option key={f.value} value={f.value}>
                  {f.label}
                </option>
              ))}
            </Select>
          </Field>
        ) : (
          <Field label="পদবি" htmlFor="editDesignation">
            <Input id="editDesignation" {...register("designation")} />
          </Field>
        )}

        <Field label="অবস্থা" htmlFor="editActive">
          <Select
            id="editActive"
            {...register("isActive", { setValueAs: (v) => v === "true" })}
          >
            <option value="true">সক্রিয়</option>
            <option value="false">নিষ্ক্রিয়</option>
          </Select>
        </Field>
      </form>
    </Modal>
  );
}
