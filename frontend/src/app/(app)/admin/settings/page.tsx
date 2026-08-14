"use client";

import { PageHeader } from "@/components/app-shell";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { Field, Input, Select } from "@/components/ui/field";
import { ErrorState, Skeleton } from "@/components/ui/states";
import { useSettings, useUpdateSettings } from "@/hooks/use-admin";
import { describeError } from "@/lib/api-client";
import type { AppSettingDto } from "@/lib/types";
import { Lock } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

/**
 * School-wide settings. Each row carries its own category, type and editable
 * flag, so this page groups and renders from the payload rather than keeping a
 * hard-coded list that would go stale the moment a setting is added.
 *
 * Some rows are deliberately read-only: the school's EIIN is issued by the
 * board, and "six periods a day" is baked into the routine grid — editing
 * either here would only produce a screen that disagrees with reality.
 */
export default function AdminSettingsPage() {
  const { data, isPending, error, refetch } = useSettings();
  const update = useUpdateSettings();

  // The draft holds only the keys the admin has actually touched, so it needs no
  // seeding from the server payload: every field reads `draft[key] ?? value`, and
  // clearing the draft after a save is what resets "unsaved changes".
  const [draft, setDraft] = useState<Record<string, string>>({});

  if (isPending) {
    return (
      <>
        <PageHeader title="সেটিংস" />
        <Skeleton className="h-64 w-full" />
      </>
    );
  }

  if (error) return <ErrorState error={error} onRetry={refetch} />;

  const dirty = data.some((s) => s.key in draft && draft[s.key] !== s.value);

  // Category order follows the API's DisplayOrder, which is the order an admin
  // reads them in: the school first, then the year, then the defaults.
  const categories: { name: string; settings: AppSettingDto[] }[] = [];
  for (const setting of [...data].sort((a, b) => a.displayOrder - b.displayOrder)) {
    const existing = categories.find((c) => c.name === setting.category);
    if (existing) existing.settings.push(setting);
    else categories.push({ name: setting.category, settings: [setting] });
  }

  // Read-only rows are never sent — the API would reject them anyway.
  const editable = Object.fromEntries(
    data
      .filter((s) => s.isEditable)
      .map((s) => [s.key, draft[s.key] ?? s.value]),
  );

  async function save() {
    try {
      await update.mutateAsync({ settings: editable });
      setDraft({});
      toast.success("সেটিংস সংরক্ষিত হয়েছে");
    } catch (err) {
      toast.error(describeError(err));
    }
  }

  return (
    <>
      <PageHeader
        title="সেটিংস"
        description="প্রতিষ্ঠানের তথ্য এবং নতুন অ্যাসাইনমেন্ট ও জমার ক্ষেত্রে প্রযোজ্য ডিফল্ট মান।"
      />

      <div className="flex max-w-3xl flex-col gap-4">
        {categories.map((category) => (
          <Card key={category.name}>
            <CardHeader title={category.name} />
            <CardBody className="flex flex-col gap-5">
              {category.settings.map((setting) => (
                <SettingField
                  key={setting.key}
                  setting={setting}
                  value={draft[setting.key] ?? setting.value}
                  onChange={(value) =>
                    setDraft((prev) => ({ ...prev, [setting.key]: value }))
                  }
                />
              ))}
            </CardBody>
          </Card>
        ))}

        <div className="sticky bottom-4 flex items-center justify-between gap-3 rounded-xl border border-border bg-surface p-3 shadow-sm">
          <p className="text-xs text-muted">
            {dirty ? "অসংরক্ষিত পরিবর্তন রয়েছে।" : "সব পরিবর্তন সংরক্ষিত।"}
          </p>
          <Button onClick={save} loading={update.isPending} disabled={!dirty}>
            সংরক্ষণ করুন
          </Button>
        </div>
      </div>
    </>
  );
}

function SettingField({
  setting,
  value,
  onChange,
}: {
  setting: AppSettingDto;
  value: string;
  onChange: (value: string) => void;
}) {
  const id = `setting-${setting.key}`;
  const disabled = !setting.isEditable;

  return (
    <Field
      label={setting.description}
      htmlFor={id}
      hint={disabled ? "এই মানটি পরিবর্তনযোগ্য নয়।" : undefined}
    >
      {setting.valueType === "bool" ? (
        <Select
          id={id}
          value={value}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value)}
        >
          <option value="true">চালু</option>
          <option value="false">বন্ধ</option>
        </Select>
      ) : (
        <Input
          id={id}
          type={setting.valueType === "int" ? "number" : "text"}
          value={value}
          disabled={disabled}
          dir={setting.valueType === "string" ? undefined : "ltr"}
          onChange={(e) => onChange(e.target.value)}
        />
      )}
      <p className="flex items-center gap-1 font-mono text-[11px] text-muted">
        {disabled && <Lock className="size-3" />}
        {setting.key}
      </p>
    </Field>
  );
}
