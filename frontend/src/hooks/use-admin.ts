"use client";

import { api, qs } from "@/lib/api-client";
import type {
  AppSettingDto,
  AssignCourseTeacherRequest,
  ClassRoomDto,
  ClassRoutineDto,
  CourseDto,
  CreateClassRoomRequest,
  CreateCourseRequest,
  CreateEnrollmentRequest,
  CreateSubjectRequest,
  CreateUserRequest,
  EnrollmentDto,
  PagedResult,
  SetRoutinePeriodRequest,
  SubjectDto,
  UpdateAppSettingsRequest,
  UpdateClassRoomRequest,
  UpdateSubjectRequest,
  UpdateUserRequest,
  UserDto,
  UserRole,
  WeekDay,
} from "@/lib/types";
import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";

/** Every admin mutation refreshes its own list; ids are the query key roots. */
function invalidate(qc: QueryClient, ...keys: string[]) {
  for (const key of keys) qc.invalidateQueries({ queryKey: [key] });
}

// ------------------------------------------------------------------- users

export interface UserFilters {
  page?: number;
  pageSize?: number;
  role?: UserRole | "";
  search?: string;
  isActive?: boolean;
}

export function useUsers(filters: UserFilters = {}) {
  return useQuery({
    queryKey: ["users", filters],
    queryFn: () => api.get<PagedResult<UserDto>>(`/users${qs({ ...filters })}`),
  });
}

/** Convenience for the pickers that need to list only students or teachers. */
export function useUsersByRole(role: UserRole) {
  return useQuery({
    queryKey: ["users", "by-role", role],
    queryFn: () =>
      api.get<PagedResult<UserDto>>(
        `/users${qs({ role, pageSize: 200, isActive: "true" })}`,
      ),
    staleTime: 60_000,
  });
}

export function useCreateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateUserRequest) => api.post<UserDto>("/users", body),
    onSuccess: () => invalidate(qc, "users"),
  });
}

export function useUpdateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateUserRequest }) =>
      api.put<UserDto>(`/users/${id}`, body),
    onSuccess: () => invalidate(qc, "users"),
  });
}

/** DELETE /users/{id} deactivates rather than destroying — history stays intact. */
export function useDeactivateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/users/${id}`),
    onSuccess: () => invalidate(qc, "users"),
  });
}

// ----------------------------------------------------------------- classes

export interface AcademicFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export function useClasses(filters: AcademicFilters = {}) {
  return useQuery({
    queryKey: ["classes", filters],
    queryFn: () =>
      api.get<PagedResult<ClassRoomDto>>(`/classes${qs({ ...filters })}`),
  });
}

export function useAllClasses() {
  return useQuery({
    queryKey: ["classes", "all"],
    queryFn: () =>
      api.get<PagedResult<ClassRoomDto>>(`/classes${qs({ pageSize: 100 })}`),
    staleTime: 60_000,
  });
}

export function useCreateClass() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateClassRoomRequest) =>
      api.post<ClassRoomDto>("/classes", body),
    onSuccess: () => invalidate(qc, "classes"),
  });
}

export function useUpdateClass() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateClassRoomRequest }) =>
      api.put<ClassRoomDto>(`/classes/${id}`, body),
    onSuccess: () => invalidate(qc, "classes"),
  });
}

export function useDeleteClass() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/classes/${id}`),
    onSuccess: () => invalidate(qc, "classes"),
  });
}

/** The roll sheet — teachers of the class may read it too, not only admins. */
export function useClassRoster(classRoomId: string) {
  return useQuery({
    queryKey: ["classes", classRoomId, "roster"],
    queryFn: () => api.get<EnrollmentDto[]>(`/classes/${classRoomId}/roster`),
    enabled: !!classRoomId,
  });
}

// ---------------------------------------------------------------- subjects

export function useSubjects(filters: AcademicFilters = {}) {
  return useQuery({
    queryKey: ["subjects", filters],
    queryFn: () =>
      api.get<PagedResult<SubjectDto>>(`/subjects${qs({ ...filters })}`),
  });
}

export function useAllSubjects() {
  return useQuery({
    queryKey: ["subjects", "all"],
    queryFn: () =>
      api.get<PagedResult<SubjectDto>>(`/subjects${qs({ pageSize: 100 })}`),
    staleTime: 60_000,
  });
}

export function useCreateSubject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateSubjectRequest) =>
      api.post<SubjectDto>("/subjects", body),
    onSuccess: () => invalidate(qc, "subjects"),
  });
}

export function useUpdateSubject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateSubjectRequest }) =>
      api.put<SubjectDto>(`/subjects/${id}`, body),
    onSuccess: () => invalidate(qc, "subjects"),
  });
}

export function useDeleteSubject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/subjects/${id}`),
    onSuccess: () => invalidate(qc, "subjects"),
  });
}

// ----------------------------------------------------------------- courses

export interface CourseFilters {
  page?: number;
  pageSize?: number;
  classRoomId?: string;
  subjectId?: string;
  teacherId?: string;
  unstaffed?: boolean;
  search?: string;
}

/**
 * Courses are the join of class × subject × teacher. They replaced the old
 * "teaching grant" table: a grant said who *may* teach, a course says what is
 * actually taught, which is also what the routine and every assignment hang on.
 */
export function useCourses(filters: CourseFilters = {}) {
  return useQuery({
    queryKey: ["courses", filters],
    queryFn: () =>
      api.get<PagedResult<CourseDto>>(`/courses${qs({ ...filters })}`),
  });
}

export function useCreateCourse() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateCourseRequest) =>
      api.post<CourseDto>("/courses", body),
    onSuccess: () => invalidate(qc, "courses", "classes", "routine"),
  });
}

export function useAssignCourseTeacher() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      body,
    }: {
      id: string;
      body: AssignCourseTeacherRequest;
    }) => api.put<CourseDto>(`/courses/${id}/teacher`, body),
    onSuccess: () => invalidate(qc, "courses", "routine"),
  });
}

export function useDeleteCourse() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/courses/${id}`),
    onSuccess: () => invalidate(qc, "courses", "classes", "routine"),
  });
}

// ------------------------------------------------------------- enrollments

export function useEnrollments(filters: {
  page?: number;
  pageSize?: number;
  classRoomId?: string;
  studentId?: string;
}) {
  return useQuery({
    queryKey: ["enrollments", filters],
    queryFn: () =>
      api.get<PagedResult<EnrollmentDto>>(`/enrollments${qs({ ...filters })}`),
  });
}

export function useCreateEnrollment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateEnrollmentRequest) =>
      api.post<EnrollmentDto>("/enrollments", body),
    onSuccess: () => invalidate(qc, "enrollments", "classes"),
  });
}

export function useDeleteEnrollment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/enrollments/${id}`),
    onSuccess: () => invalidate(qc, "enrollments", "classes"),
  });
}

// ----------------------------------------------------------------- routine

export function useClassRoutine(classRoomId: string) {
  return useQuery({
    queryKey: ["routine", "class", classRoomId],
    queryFn: () => api.get<ClassRoutineDto>(`/classes/${classRoomId}/routine`),
    enabled: !!classRoomId,
  });
}

/**
 * Both mutations return the whole rebuilt routine, so the response is written
 * straight into the cache instead of triggering a refetch — the grid never
 * flickers back to its old state between the write and the reload.
 */
export function useSetRoutinePeriod() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SetRoutinePeriodRequest) =>
      api.put<ClassRoutineDto>("/routine", body),
    onSuccess: (routine, body) =>
      qc.setQueryData(["routine", "class", body.classRoomId], routine),
  });
}

export function useClearRoutinePeriod() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      classRoomId,
      day,
      periodIndex,
    }: {
      classRoomId: string;
      day: WeekDay;
      periodIndex: number;
    }) =>
      api.delete<ClassRoutineDto>(
        `/routine${qs({ classRoomId, day, periodIndex })}`,
      ),
    onSuccess: (routine, { classRoomId }) =>
      qc.setQueryData(["routine", "class", classRoomId], routine),
  });
}

// ---------------------------------------------------------------- settings

export function useSettings() {
  return useQuery({
    queryKey: ["settings"],
    queryFn: () => api.get<AppSettingDto[]>("/settings"),
  });
}

export function useUpdateSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateAppSettingsRequest) =>
      api.put<AppSettingDto[]>("/settings", body),
    onSuccess: () => invalidate(qc, "settings"),
  });
}
