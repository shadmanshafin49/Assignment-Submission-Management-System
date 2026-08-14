"use client";

import { api, qs } from "@/lib/api-client";
import type {
  AssignmentDto,
  AssignmentStatus,
  AssignmentType,
  AttachmentDto,
  CourseDto,
  CreateAssignmentRequest,
  GradeSubmissionRequest,
  PagedResult,
  SubmissionDto,
  SubmissionStatus,
  TeacherRoutineSlotDto,
  UpdateAssignmentRequest,
  UpdateSubmissionStatusRequest,
} from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

export interface AssignmentFilters {
  page?: number;
  pageSize?: number;
  status?: AssignmentStatus | "";
  courseId?: string;
  classRoomId?: string;
  subjectId?: string;
  type?: AssignmentType | "";
  weekNumber?: number;
  search?: string;
  /** Admin-only — the API ignores it for a teacher, who only ever sees their own. */
  teacherId?: string;
}

export interface SubmissionFilters {
  page?: number;
  pageSize?: number;
  status?: SubmissionStatus | "";
  assignmentId?: string;
  courseId?: string;
  classRoomId?: string;
  studentId?: string;
}

export const teacherKeys = {
  courses: ["teacher", "courses"] as const,
  routine: ["teacher", "routine"] as const,
  assignments: (f: AssignmentFilters) => ["assignments", f] as const,
  assignment: (id: string) => ["assignment", id] as const,
  assignmentSubmissions: (id: string, f: SubmissionFilters) =>
    ["assignment", id, "submissions", f] as const,
  submissions: (f: SubmissionFilters) => ["submissions", f] as const,
};

/** The courses this teacher takes — the only ones they may set work for. */
export function useMyCourses() {
  return useQuery({
    queryKey: teacherKeys.courses,
    queryFn: () => api.get<CourseDto[]>("/me/courses"),
    staleTime: 5 * 60_000,
  });
}

/** The teacher's own week: which class they are with, in which period. */
export function useMyRoutine() {
  return useQuery({
    queryKey: teacherKeys.routine,
    queryFn: () => api.get<TeacherRoutineSlotDto[]>("/me/routine"),
    staleTime: 5 * 60_000,
  });
}

export function useAssignments(filters: AssignmentFilters) {
  return useQuery({
    queryKey: teacherKeys.assignments(filters),
    queryFn: () =>
      api.get<PagedResult<AssignmentDto>>(`/assignments${qs({ ...filters })}`),
  });
}

export function useAssignment(id: string) {
  return useQuery({
    queryKey: teacherKeys.assignment(id),
    queryFn: () => api.get<AssignmentDto>(`/assignments/${id}`),
    enabled: !!id,
  });
}

export function useAssignmentSubmissions(
  assignmentId: string,
  filters: SubmissionFilters = {},
) {
  return useQuery({
    queryKey: teacherKeys.assignmentSubmissions(assignmentId, filters),
    queryFn: () =>
      api.get<PagedResult<SubmissionDto>>(
        `/assignments/${assignmentId}/submissions${qs({ ...filters })}`,
      ),
    enabled: !!assignmentId,
  });
}

export function useSubmissions(filters: SubmissionFilters) {
  return useQuery({
    queryKey: teacherKeys.submissions(filters),
    queryFn: () =>
      api.get<PagedResult<SubmissionDto>>(`/submissions${qs({ ...filters })}`),
  });
}

// ------------------------------------------------------------------ mutations

export function useCreateAssignment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateAssignmentRequest) =>
      api.post<AssignmentDto>("/assignments", body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assignments"] }),
  });
}

export function useUpdateAssignment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateAssignmentRequest }) =>
      api.put<AssignmentDto>(`/assignments/${id}`, body),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: ["assignments"] });
      qc.invalidateQueries({ queryKey: teacherKeys.assignment(id) });
    },
  });
}

/** Publish and unpublish share a shape, so one hook covers both. */
export function useAssignmentPublication() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, publish }: { id: string; publish: boolean }) =>
      api.post<AssignmentDto>(
        `/assignments/${id}/${publish ? "publish" : "unpublish"}`,
      ),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: ["assignments"] });
      qc.invalidateQueries({ queryKey: teacherKeys.assignment(id) });
    },
  });
}

export function useDeleteAssignment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/assignments/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assignments"] }),
  });
}

/** Worksheets and question papers the teacher hangs off the assignment post. */
export function useAddAssignmentAttachment(assignmentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => {
      const form = new FormData();
      form.append("file", file);
      return api.upload<AttachmentDto>(
        `/assignments/${assignmentId}/attachments`,
        form,
      );
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: teacherKeys.assignment(assignmentId) });
      qc.invalidateQueries({ queryKey: ["assignments"] });
    },
  });
}

export function useRemoveAssignmentAttachment(assignmentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (attachmentId: string) =>
      api.delete<void>(
        `/assignments/${assignmentId}/attachments/${attachmentId}`,
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: teacherKeys.assignment(assignmentId) });
      qc.invalidateQueries({ queryKey: ["assignments"] });
    },
  });
}

export function useGradeSubmission() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      body,
    }: {
      id: string;
      body: GradeSubmissionRequest;
    }) => api.put<SubmissionDto>(`/submissions/${id}/grade`, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["submissions"] });
      qc.invalidateQueries({ queryKey: ["assignment"] });
      qc.invalidateQueries({ queryKey: ["assignments"] });
    },
  });
}

export function useUpdateSubmissionStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      body,
    }: {
      id: string;
      body: UpdateSubmissionStatusRequest;
    }) => api.put<SubmissionDto>(`/submissions/${id}/status`, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["submissions"] });
      qc.invalidateQueries({ queryKey: ["assignment"] });
    },
  });
}
