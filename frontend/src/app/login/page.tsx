"use client";

import { SCHOOL_NAME } from "@/components/app-shell";
import { Button } from "@/components/ui/button";
import { Field, Input } from "@/components/ui/field";
import { ROLE_HOME } from "@/lib/jwt";
import type { ProblemDetails, UserDto } from "@/lib/types";
import { zodResolver } from "@hookform/resolvers/zod";
import { GraduationCap } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";

const schema = z.object({
  email: z.string().min(1, "ইমেইল দিন").email("সঠিক ইমেইল দিন"),
  password: z.string().min(1, "পাসওয়ার্ড দিন"),
});

type FormValues = z.infer<typeof schema>;

/**
 * Seeded accounts — one click to fill, so an evaluator never types these.
 * রেজাউল করিম teaches গণিত to all three classes and বাংলাদেশ ও বিশ্বপরিচয় besides,
 * so his account has the fullest teacher view; c6r01 is roll ১ of ষষ্ঠ শ্রেণি.
 */
const DEMO_ACCOUNTS = [
  {
    label: "প্রধান শিক্ষক",
    email: "admin@gcbhs.edu.bd",
    password: "Admin@123",
  },
  {
    label: "শিক্ষক",
    email: "rejaul.karim@gcbhs.edu.bd",
    password: "Teacher@123",
  },
  {
    label: "শিক্ষার্থী",
    email: "c6r01@student.gcbhs.edu.bd",
    password: "Student@123",
  },
] as const;

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(values),
    });

    if (!res.ok) {
      let problem: ProblemDetails | null = null;
      try {
        problem = (await res.json()) as ProblemDetails;
      } catch {
        // fall through to the generic message
      }
      setFormError(
        problem?.detail ||
          problem?.title ||
          (res.status === 401
            ? "ইমেইল বা পাসওয়ার্ড ভুল।"
            : "সাইন ইন করা যায়নি। আবার চেষ্টা করুন।"),
      );
      return;
    }

    const { user } = (await res.json()) as { user: UserDto };
    const next = searchParams.get("next");

    // Only honour `next` if the user's role can actually see it, otherwise the
    // route guard would immediately bounce them again.
    const home = ROLE_HOME[user.role];
    router.replace(next?.startsWith(home) ? next : home);
    router.refresh();
  });

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      {/* A quiet wash of school colour behind the card — cheerful, not busy. */}
      <div
        aria-hidden
        className="pointer-events-none fixed inset-x-0 top-0 h-64 bg-gradient-to-b from-primary-soft to-transparent"
      />

      <div className="relative w-full max-w-sm">
        <div className="mb-6 flex flex-col items-center gap-2 text-center">
          <div className="flex size-12 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-sm">
            <GraduationCap className="size-6" />
          </div>
          <h1 className="text-lg font-semibold leading-snug">{SCHOOL_NAME}</h1>
          <p className="text-sm text-muted">
            অ্যাসাইনমেন্ট ব্যবস্থাপনা — সাইন ইন করুন
          </p>
        </div>

        <form
          onSubmit={onSubmit}
          noValidate
          className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-5 shadow-sm"
        >
          {formError && (
            <p
              role="alert"
              className="rounded-lg bg-danger-soft px-3 py-2 text-sm font-medium text-danger"
            >
              {formError}
            </p>
          )}

          <Field
            label="ইমেইল"
            htmlFor="email"
            required
            error={errors.email?.message}
          >
            <Input
              id="email"
              type="email"
              autoComplete="email"
              dir="ltr"
              placeholder="name@gcbhs.edu.bd"
              invalid={!!errors.email}
              {...register("email")}
            />
          </Field>

          <Field
            label="পাসওয়ার্ড"
            htmlFor="password"
            required
            error={errors.password?.message}
          >
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              dir="ltr"
              placeholder="••••••••"
              invalid={!!errors.password}
              {...register("password")}
            />
          </Field>

          <Button type="submit" loading={isSubmitting} className="mt-1 w-full">
            সাইন ইন
          </Button>
        </form>

        <div className="mt-5">
          <p className="mb-2 text-center text-xs font-medium tracking-wide text-muted">
            ডেমো অ্যাকাউন্ট
          </p>
          <div className="grid grid-cols-3 gap-2">
            {DEMO_ACCOUNTS.map((account) => (
              <button
                key={account.label}
                type="button"
                onClick={() => {
                  setValue("email", account.email, { shouldValidate: true });
                  setValue("password", account.password, {
                    shouldValidate: true,
                  });
                }}
                className="rounded-lg border border-border bg-surface px-2 py-2 text-xs font-medium hover:bg-surface-muted"
              >
                {account.label}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  // useSearchParams needs a Suspense boundary to keep the route prerenderable.
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
