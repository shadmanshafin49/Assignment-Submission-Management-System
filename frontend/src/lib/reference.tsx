"use client";

import { createContext, useContext, useMemo } from "react";
import type {
  AssignmentStatus,
  AssignmentType,
  EnumOptionDto,
  FaithGroup,
  ReferenceDataDto,
  SubmissionStatus,
  UserRole,
  WeekDay,
} from "./types";

/**
 * Domain vocabulary — "সৃজনশীল প্রশ্ন", "খসড়া", "৩য় পিরিয়ড" — comes from the API's
 * /api/reference endpoint rather than a table hard-coded here. Those strings name
 * NCTB question types and enum members, so they belong with the enums that define
 * them; a second copy in the frontend is a copy that drifts.
 *
 * It is fetched once in the server layout and handed down through this provider,
 * so every badge and dropdown can look a label up synchronously with no loading
 * state of its own.
 */
const ReferenceContext = createContext<ReferenceDataDto | null>(null);

export function ReferenceProvider({
  value,
  children,
}: {
  value: ReferenceDataDto;
  children: React.ReactNode;
}) {
  return (
    <ReferenceContext.Provider value={value}>
      {children}
    </ReferenceContext.Provider>
  );
}

function useReference(): ReferenceDataDto {
  const ctx = useContext(ReferenceContext);
  if (!ctx) {
    throw new Error("useReference must be used inside ReferenceProvider");
  }
  return ctx;
}

/** The school-day constants: period count, bell times, break position. */
export function useSchoolDay() {
  const ref = useReference();
  return {
    periods: ref.periods,
    periodsPerDay: ref.periodsPerDay,
    teachingDaysPerWeek: ref.teachingDaysPerWeek,
    periodsPerWeek: ref.periodsPerWeek,
    assemblyStart: ref.assemblyStart,
    assemblyEnd: ref.assemblyEnd,
    breakAfterPeriod: ref.breakAfterPeriod,
    breakStart: ref.breakStart,
    breakEnd: ref.breakEnd,
    weekDays: ref.weekDays,
  };
}

function lookup(options: EnumOptionDto[]) {
  const map = new Map(options.map((o) => [o.value, o.label]));
  // Falls back to the raw enum name: a label the API has not been taught yet is
  // better rendered as "PracticalWork" than as an empty chip.
  return (value: string | null | undefined) =>
    value ? map.get(value) ?? value : "—";
}

/**
 * Bangla labels for every enum the UI renders, plus the option lists that fill
 * the dropdowns. Both come from the same payload, so a `<select>` can never
 * offer a value the badge cannot name.
 */
export function useLabels() {
  const ref = useReference();

  return useMemo(
    () => ({
      assignmentType: lookup(ref.assignmentTypes) as (
        v: AssignmentType | null | undefined,
      ) => string,
      assignmentStatus: lookup(ref.assignmentStatuses) as (
        v: AssignmentStatus | null | undefined,
      ) => string,
      submissionStatus: lookup(ref.submissionStatuses) as (
        v: SubmissionStatus | null | undefined,
      ) => string,
      role: lookup(ref.roles) as (v: UserRole | null | undefined) => string,
      faith: lookup(ref.faithGroups) as (
        v: FaithGroup | null | undefined,
      ) => string,
      day: lookup(ref.weekDays) as (v: WeekDay | null | undefined) => string,

      options: {
        assignmentTypes: ref.assignmentTypes,
        assignmentStatuses: ref.assignmentStatuses,
        submissionStatuses: ref.submissionStatuses,
        roles: ref.roles,
        faithGroups: ref.faithGroups,
        weekDays: ref.weekDays,
      },
    }),
    [ref],
  );
}
