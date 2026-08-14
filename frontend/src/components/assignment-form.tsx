"use client";

import { Button } from "@/components/ui/button";
import { Checkbox, Field, Input, Select, Textarea } from "@/components/ui/field";
import { Modal } from "@/components/ui/modal";
import {
  useCreateAssignment,
  useMyCourses,
  useUpdateAssignment,
} from "@/hooks/use-teacher";
import { describeError } from "@/lib/api-client";
import { useLabels } from "@/lib/reference";
import type { AssignmentDto, AssignmentType } from "@/lib/types";
import { bn, fromDateTimeLocalValue, toDateTimeLocalValue } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

const schema = z.object({
  title: z
    .string()
    .trim()
    .min(3, "শিরোনাম অন্তত ৩ অক্ষরের হতে হবে")
    .max(200, "শিরোনাম অনেক বড় হয়ে গেছে"),
  description: z
    .string()
    .trim()
    .min(1, "নির্দেশনা লিখুন")
    .max(5000, "নির্দেশনা অনেক বড় হয়ে গেছে"),
  courseId: z.string().min(1, "কোর্স নির্বাচন করুন"),
  type: z.string().min(1, "কাজের ধরন নির্বাচন করুন"),
  chapterOrLesson: z.string().trim().max(200).optional(),
  deadline: z.string().min(1, "সময়সীমা দিন"),
  maxMarks: z.coerce
    .number()
    .int("নম্বর পূর্ণসংখ্যা হতে হবে")
    .min(1, "পূর্ণ নম্বর অন্তত ১ হতে হবে")
    .max(1000, "পূর্ণ নম্বর ১০০০ এর বেশি হতে পারে না"),
  allowLateSubmission: z.boolean(),
  allowResubmission: z.boolean(),
  allowComments: z.boolean(),
});

type FormValues = z.input<typeof schema>;
type ParsedValues = z.output<typeof schema>;

/** A week from now, rounded to the hour — the school's standard homework window. */
function defaultDeadline(): string {
  const d = new Date();
  d.setDate(d.getDate() + 7);
  d.setMinutes(0, 0, 0);
  return toDateTimeLocalValue(d.toISOString());
}

export function AssignmentFormModal({
  open,
  onClose,
  assignment,
}: {
  open: boolean;
  onClose: () => void;
  /** Present when editing; omitted when creating. */
  assignment?: AssignmentDto | null;
}) {
  const isEditing = !!assignment;
  const labels = useLabels();
  const { data: courses, isPending: loadingCourses } = useMyCourses();

  const create = useCreateAssignment();
  const update = useUpdateAssignment();

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues, unknown, ParsedValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: assignment?.title ?? "",
      description: assignment?.description ?? "",
      courseId: assignment?.courseId ?? "",
      type: assignment?.type ?? "",
      chapterOrLesson: assignment?.chapterOrLesson ?? "",
      deadline: assignment
        ? toDateTimeLocalValue(assignment.deadline)
        : defaultDeadline(),
      maxMarks: assignment?.maxMarks ?? 20,
      allowLateSubmission: assignment?.allowLateSubmission ?? false,
      allowResubmission: assignment?.allowResubmission ?? true,
      allowComments: assignment?.allowComments ?? true,
    },
  });

  const courseId = watch("courseId");
  const selectedCourse = courses?.find((c) => c.id === courseId);

  /**
   * The type list narrows to what this subject actually sets. NCTB prescribes
   * per-subject question types — ভাবসম্প্রসারণ belongs to বাংলা ২য় পত্র and nowhere
   * else — and the API refuses anything outside the subject's list, so offering
   * all eighteen would only invite a rejected save.
   */
  const allowedTypes: AssignmentType[] = selectedCourse?.allowedAssignmentTypes ?? [];

  const deadlineValue = watch("deadline");
  const deadlineInPast =
    !!deadlineValue && new Date(deadlineValue).getTime() < Date.now();

  const onSubmit = handleSubmit(async (values) => {
    const body = {
      title: values.title,
      description: values.description,
      courseId: values.courseId,
      type: values.type as AssignmentType,
      chapterOrLesson: values.chapterOrLesson?.trim() || null,
      deadline: fromDateTimeLocalValue(values.deadline),
      maxMarks: values.maxMarks,
      allowLateSubmission: values.allowLateSubmission,
      allowResubmission: values.allowResubmission,
      allowComments: values.allowComments,
    };

    try {
      if (isEditing) {
        await update.mutateAsync({ id: assignment.id, body });
        toast.success("অ্যাসাইনমেন্ট হালনাগাদ হয়েছে");
      } else {
        await create.mutateAsync(body);
        toast.success("খসড়া তৈরি হয়েছে");
      }
      onClose();
    } catch (err) {
      toast.error(describeError(err));
    }
  });

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="lg"
      title={isEditing ? "অ্যাসাইনমেন্ট সম্পাদনা" : "নতুন অ্যাসাইনমেন্ট"}
      description={
        isEditing
          ? undefined
          : "খসড়া হিসেবে তৈরি হবে — প্রকাশ না করা পর্যন্ত শিক্ষার্থীরা দেখতে পাবে না।"
      }
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            বাতিল
          </Button>
          <Button
            type="submit"
            form="assignment-form"
            loading={isSubmitting || create.isPending || update.isPending}
          >
            {isEditing ? "পরিবর্তন সংরক্ষণ" : "খসড়া তৈরি করুন"}
          </Button>
        </>
      }
    >
      <form
        id="assignment-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <Field
          label="কোর্স"
          htmlFor="courseId"
          required
          error={errors.courseId?.message}
          hint={
            isEditing
              ? "শিক্ষার্থীরা জমা দেওয়ার পর কোর্স পরিবর্তন করা যায় না।"
              : "আপনি যেসব কোর্সে পাঠদান করেন কেবল সেগুলোই এখানে আছে।"
          }
        >
          <Select
            id="courseId"
            invalid={!!errors.courseId}
            disabled={loadingCourses}
            {...register("courseId")}
          >
            <option value="">
              {loadingCourses ? "লোড হচ্ছে…" : "কোর্স নির্বাচন করুন"}
            </option>
            {courses?.map((course) => (
              <option key={course.id} value={course.id}>
                {course.code} — {course.classRoomName} · {course.subjectName}
              </option>
            ))}
          </Select>
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field
            label="কাজের ধরন"
            htmlFor="type"
            required
            error={errors.type?.message}
            hint={
              selectedCourse
                ? `${selectedCourse.subjectName} বিষয়ে অনুমোদিত ধরনসমূহ`
                : "আগে কোর্স নির্বাচন করুন"
            }
          >
            <Select
              id="type"
              invalid={!!errors.type}
              disabled={!selectedCourse}
              {...register("type")}
            >
              <option value="">ধরন নির্বাচন করুন</option>
              {allowedTypes.map((type) => (
                <option key={type} value={type}>
                  {labels.assignmentType(type)}
                </option>
              ))}
            </Select>
          </Field>

          <Field
            label="অধ্যায় / পাঠ"
            htmlFor="chapterOrLesson"
            error={errors.chapterOrLesson?.message}
            hint="যেমন: চতুর্থ অধ্যায় — বীজগণিতীয় রাশি"
          >
            <Input
              id="chapterOrLesson"
              placeholder="অধ্যায়ের নাম"
              invalid={!!errors.chapterOrLesson}
              {...register("chapterOrLesson")}
            />
          </Field>
        </div>

        <Field
          label="শিরোনাম"
          htmlFor="title"
          required
          error={errors.title?.message}
        >
          <Input
            id="title"
            placeholder="অনুশীলনী ৪.১ সমাধান"
            invalid={!!errors.title}
            {...register("title")}
          />
        </Field>

        <Field
          label="নির্দেশনা"
          htmlFor="description"
          required
          error={errors.description?.message}
        >
          <Textarea
            id="description"
            rows={5}
            placeholder="শিক্ষার্থীদের কী করতে হবে?"
            invalid={!!errors.description}
            {...register("description")}
          />
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field
            label="সময়সীমা"
            htmlFor="deadline"
            required
            error={errors.deadline?.message}
            hint={
              deadlineInPast
                ? "এই সময় ইতিমধ্যে পেরিয়ে গেছে — এটি প্রকাশ করা যাবে না।"
                : "সাধারণত অ্যাসাইন করার এক সপ্তাহ পর।"
            }
          >
            <Input
              id="deadline"
              type="datetime-local"
              invalid={!!errors.deadline}
              {...register("deadline")}
            />
          </Field>

          <Field
            label="পূর্ণ নম্বর"
            htmlFor="maxMarks"
            required
            error={errors.maxMarks?.message}
            hint={
              selectedCourse
                ? `বিষয়ের পূর্ণমান ${bn(selectedCourse.fullMarks)}`
                : undefined
            }
          >
            <Input
              id="maxMarks"
              type="number"
              min={1}
              max={1000}
              invalid={!!errors.maxMarks}
              {...register("maxMarks")}
            />
          </Field>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <Checkbox
            label="বিলম্বে জমা"
            description="সময়সীমার পরেও জমা নেওয়া হবে, বিলম্বিত হিসেবে চিহ্নিত।"
            {...register("allowLateSubmission")}
          />
          <Checkbox
            label="জমার পর সম্পাদনা"
            description="সময়সীমার আগে শিক্ষার্থী উত্তর বদলাতে পারবে।"
            {...register("allowResubmission")}
          />
          <Checkbox
            label="শ্রেণি আলোচনা"
            description="শিক্ষার্থীরা এই কাজের নিচে প্রশ্ন করতে পারবে।"
            {...register("allowComments")}
          />
        </div>
      </form>
    </Modal>
  );
}
