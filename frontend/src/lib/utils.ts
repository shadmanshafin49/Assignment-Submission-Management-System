import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/** The locale every date, number and weekday name in this app is rendered in. */
export const LOCALE = "bn-BD";

/**
 * Bangla digits. `Intl` handles this for dates and numbers, but marks, roll
 * numbers and counts are interpolated into strings all over the UI, and a page
 * that mixes ২০ and 20 looks like a bug to the people using it.
 */
export function bn(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return "—";
  return String(value).replace(/\d/g, (d) => "০১২৩৪৫৬৭৮৯"[Number(d)]!);
}

/**
 * The API stores and returns UTC (`timestamptz`); rendering in the viewer's
 * local zone is deliberately the frontend's job.
 */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString(LOCALE, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export function formatDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString(LOCALE, { dateStyle: "medium" });
}

/** "৩ দিনে" / "২ ঘণ্টা আগে" — used on deadline badges. */
export function formatRelative(iso: string | null | undefined): string {
  if (!iso) return "—";
  const diffMs = new Date(iso).getTime() - Date.now();
  const rtf = new Intl.RelativeTimeFormat(LOCALE, { numeric: "auto" });

  const units: [Intl.RelativeTimeFormatUnit, number][] = [
    ["year", 1000 * 60 * 60 * 24 * 365],
    ["month", 1000 * 60 * 60 * 24 * 30],
    ["day", 1000 * 60 * 60 * 24],
    ["hour", 1000 * 60 * 60],
    ["minute", 1000 * 60],
  ];

  for (const [unit, ms] of units) {
    if (Math.abs(diffMs) >= ms) return rtf.format(Math.round(diffMs / ms), unit);
  }
  return rtf.format(Math.round(diffMs / 1000), "second");
}

/** `০৮:০০` — bell times arrive from the API as plain "HH:mm" strings. */
export function formatClock(hhmm: string): string {
  return bn(hhmm);
}

/**
 * Hours between now and the given instant, negative once it has passed.
 *
 * Reading the clock is impure, which is why it lives here rather than inline in
 * a component: the deadline badge is a coarse three-state indicator, so a value
 * that shifts between renders is harmless, but React's purity rule is right to
 * object to a component doing it in its own body.
 */
export function hoursUntil(iso: string): number {
  return (new Date(iso).getTime() - Date.now()) / 3_600_000;
}

/** `2026-08-19T14:30` for `<input type="datetime-local">`, in local time. */
export function toDateTimeLocalValue(iso: string | null | undefined): string {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(
    d.getHours(),
  )}:${pad(d.getMinutes())}`;
}

/** Inverse of the above — hand the API a proper UTC instant. */
export function fromDateTimeLocalValue(value: string): string {
  return new Date(value).toISOString();
}

/** First letter of the name, for the avatar chip. Works for Bangla and Latin. */
export function initials(fullName: string): string {
  const parts = fullName.split(" ").filter(Boolean);
  // Bangla names have no case and no useful two-letter contraction, so one
  // meaningful letter beats two: "মোঃ" as an initial would label half the school.
  const first = parts.find((p) => p !== "মোঃ" && p !== "মো." && p !== "Md") ?? parts[0] ?? "";
  return [...first].slice(0, 1).join("").toUpperCase();
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bn(bytes)} বাইট`;
  if (bytes < 1024 * 1024) return `${bn((bytes / 1024).toFixed(0))} কিলোবাইট`;
  return `${bn((bytes / (1024 * 1024)).toFixed(1))} মেগাবাইট`;
}

/**
 * Board subject code → one of the fourteen accent tones defined in globals.css.
 * Keyed by code rather than by name so a subject keeps its colour even if an
 * admin renames it, and unknown codes fall through to the neutral tone.
 */
const SUBJECT_TONES: Record<string, number> = {
  "101": 1,  // বাংলা ১ম পত্র
  "102": 2,  // বাংলা ২য় পত্র
  "107": 3,  // ইংরেজি ১ম পত্র
  "108": 4,  // ইংরেজি ২য় পত্র
  "109": 5,  // গণিত
  "127": 6,  // বিজ্ঞান
  "150": 7,  // বাংলাদেশ ও বিশ্বপরিচয়
  "154": 8,  // তথ্য ও যোগাযোগ প্রযুক্তি
  "111": 9,  // ইসলাম ও নৈতিক শিক্ষা
  "112": 9,  // হিন্দুধর্ম ও নৈতিক শিক্ষা
  "113": 9,
  "114": 9,
  "134": 10, // কৃষিশিক্ষা
  "147": 11, // শারীরিক শিক্ষা ও স্বাস্থ্য
  "148": 12, // চারু ও কারুকলা
  "155": 13, // কর্ম ও জীবনমুখী শিক্ষা
};

/**
 * Accepts either a bare subject code ("109") or a course code ("C06-109"),
 * because the two arrive from different endpoints and both label the same card.
 */
export function subjectTone(code: string | null | undefined) {
  const subjectCode = (code ?? "").split("-").pop() ?? "";
  const tone = SUBJECT_TONES[subjectCode] ?? 14;
  return {
    color: `var(--tone-${tone})`,
    background: `var(--tone-${tone}-soft)`,
  };
}

/**
 * "৪র্থ পিরিয়ড". Bangla ordinals are irregular for exactly the range the routine
 * needs, so `${bn(n)}ম` is wrong for four of the six periods — it reads "৪ম",
 * which is not a word.
 */
export function periodLabel(index: number): string {
  const ordinals: Record<number, string> = {
    1: "১ম",
    2: "২য়",
    3: "৩য়",
    4: "৪র্থ",
    5: "৫ম",
    6: "৬ষ্ঠ",
  };
  return `${ordinals[index] ?? `${bn(index)}তম`} পিরিয়ড`;
}

/** "C06-109" → "৬ষ্ঠ" style short class label for dense chips. */
export function classShortLabel(level: number): string {
  const names: Record<number, string> = {
    6: "৬ষ্ঠ",
    7: "৭ম",
    8: "৮ম",
    9: "৯ম",
    10: "১০ম",
  };
  return names[level] ?? `${bn(level)}`;
}
