"use client";

import { api, qs } from "@/lib/api-client";
import type {
  AttachmentDto,
  ClassRoutineDto,
  CourseDto,
  PagedResult,
  StudentAssignmentDto,
  SubmissionDto,
  UpdateSubmissionRequest,
} from "@/lib/types";
import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";

export interface StudentAssignmentFilters {
  page?: number;
  pageSize?: number;
  courseId?: string;
  subjectId?: string;
  search?: string;
  pendingOnly?: boolean;
  dueOnly?: boolean;
}

export const studentKeys = {
  courses: ["student", "courses"] as const,
  assignments: (f: StudentAssignmentFilters) =>
    ["student", "assignments", f] as const,
  assignment: (id: string) => ["student", "assignment", id] as const,
  submissions: (page: number) => ["student", "submissions", page] as const,
};

/** Anything that mutates a submission invalidates both lists and the detail. */
function invalidateStudent(qc: QueryClient) {
  qc.invalidateQueries({ queryKey: ["student"] });
}

/**
 * The courses the student actually takes: every course of their class, minus
 * the religion courses of other faith groups. The API applies that filter — the
 * UI never has to know which subject codes are faith-specific.
 */
export function useMyEnrolledCourses() {
  return useQuery({
    queryKey: studentKeys.courses,
    queryFn: () => api.get<CourseDto[]>("/me/enrolled-courses"),
    staleTime: 5 * 60_000,
  });
}

export function useStudentAssignments(filters: StudentAssignmentFilters = {}) {
  return useQuery({
    queryKey: studentKeys.assignments(filters),
    queryFn: () =>
      api.get<PagedResult<StudentAssignmentDto>>(
        `/student/assignments${qs({ ...filters })}`,
      ),
  });
}

export function useStudentAssignment(id: string) {
  return useQuery({
    queryKey: studentKeys.assignment(id),
    queryFn: () => api.get<StudentAssignmentDto>(`/student/assignments/${id}`),
    enabled: !!id,
  });
}

export function useMySubmissions(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: studentKeys.submissions(page),
    queryFn: () =>
      api.get<PagedResult<SubmissionDto>>(
        `/student/submissions${qs({ page, pageSize })}`,
      ),
  });
}

/** The student's own class routine, read from any course they take. */
export function useMyClassRoutine(classRoomId: string | undefined) {
  return useQuery({
    queryKey: ["routine", "class", classRoomId],
    queryFn: () => api.get<ClassRoutineDto>(`/classes/${classRoomId}/routine`),
    enabled: !!classRoomId,
  });
}

/**
 * Text and files go up in one multipart request, because the API refuses a
 * submission that has neither — sending the text first and the file second
 * would make an empty first request that is rejected on its own.
 */
export function useSubmitAssignment(assignmentId: string) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: ({
      answerText,
      files,
    }: {
      answerText: string;
      files: File[];
    }) => {
      const form = new FormData();
      form.append("answerText", answerText);
      for (const file of files) form.append("files", file);

      return api.upload<SubmissionDto>(
        `/student/assignments/${assignmentId}/submission`,
        form,
      );
    },
    onSuccess: () => invalidateStudent(qc),
  });
}

export function useUpdateSubmission(submissionId: string) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (body: UpdateSubmissionRequest) =>
      api.put<SubmissionDto>(`/student/submissions/${submissionId}`, body),
    onSuccess: () => invalidateStudent(qc),
  });
}

export function useAddSubmissionAttachment(submissionId: string) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (file: File) => {
      const form = new FormData();
      form.append("file", file);
      return api.upload<AttachmentDto>(
        `/student/submissions/${submissionId}/attachments`,
        form,
      );
    },
    onSuccess: () => invalidateStudent(qc),
  });
}

export function useRemoveSubmissionAttachment(submissionId: string) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (attachmentId: string) =>
      api.delete<void>(
        `/student/submissions/${submissionId}/attachments/${attachmentId}`,
      ),
    onSuccess: () => invalidateStudent(qc),
  });
}
