/**
 * Mirrors the DTOs returned by the ASP.NET Core API.
 * Enums travel as names (the API registers a JsonStringEnumConverter), so these
 * are string unions rather than numeric enums.
 */

export type UserRole = "Admin" | "Teacher" | "Student";
export type AssignmentStatus = "Draft" | "Published";
export type SubmissionStatus =
  | "Submitted"
  | "Late"
  | "Graded"
  | "ReturnedForRevision";

/** The four religion streams a student can be taught in. */
export type FaithGroup = "Islam" | "Hindu" | "Buddhism" | "Christianity";

export type WeekDay =
  | "Saturday"
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday";

/**
 * NCTB question and coursework types. A subject only accepts the ones listed
 * for it, which is why an assignment form has to read `allowedAssignmentTypes`
 * off the course rather than offering all eighteen.
 */
export type AssignmentType =
  | "CreativeQuestion"
  | "MultipleChoice"
  | "ShortAnswer"
  | "DescriptiveQuestion"
  | "ThemeExpansion"
  | "Precis"
  | "Letter"
  | "Paragraph"
  | "Composition"
  | "Grammar"
  | "ReadingTest"
  | "WritingTest"
  | "MathProblem"
  | "PracticalWork"
  | "Project"
  | "Report"
  | "Drawing"
  | "ClassWork";

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface UserDto {
  id: string;
  fullName: string;
  fullNameEn: string;
  email: string;
  role: UserRole;
  designation: string | null;
  faith: FaithGroup | null;
  isActive: boolean;
  createdAt: string;
}

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  user: UserDto;
}

export interface ClassRoomDto {
  id: string;
  name: string;
  nameEn: string;
  code: string;
  level: number;
  section: string | null;
  academicYear: string;
  isActive: boolean;
  studentCount: number;
  courseCount: number;
}

export interface SubjectDto {
  id: string;
  name: string;
  nameEn: string;
  /** Board subject code — 101 is বাংলা ১ম পত্র, 109 গণিত, and so on. */
  code: string;
  textbookName: string | null;
  fullMarks: number;
  weeklyPeriods: number;
  faithGroup: FaithGroup | null;
  isOptionalGroup: boolean;
  displayOrder: number;
  isActive: boolean;
  allowedAssignmentTypes: AssignmentType[];
}

/** One subject taught to one class by one teacher — e.g. C06-109, গণিত for ষষ্ঠ শ্রেণি. */
export interface CourseDto {
  id: string;
  code: string;
  classRoomId: string;
  classRoomName: string;
  classLevel: number;
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  textbookName: string | null;
  fullMarks: number;
  weeklyPeriods: number;
  faithGroup: FaithGroup | null;
  isOptionalGroup: boolean;
  teacherId: string | null;
  teacherName: string | null;
  teacherDesignation: string | null;
  academicYear: string;
  isActive: boolean;
  studentCount: number;
  assignmentCount: number;
  publishedAssignmentCount: number;
  allowedAssignmentTypes: AssignmentType[];
}

export interface EnrollmentDto {
  id: string;
  studentId: string;
  studentName: string;
  studentNameEn: string;
  studentEmail: string;
  faith: FaithGroup | null;
  classRoomId: string;
  classRoomName: string;
  rollNumber: number;
}

export interface AppSettingDto {
  key: string;
  value: string;
  description: string;
  valueType: string;
  category: string;
  isEditable: boolean;
  displayOrder: number;
}

export interface AttachmentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface AssignmentCommentDto {
  id: string;
  assignmentId: string;
  authorId: string;
  authorName: string;
  authorRole: UserRole;
  body: string;
  createdAt: string;
  updatedAt: string | null;
  isDeleted: boolean;
  /** Server-computed: the author, or the course's teacher moderating the thread. */
  canDelete: boolean;
}

/** Teacher/Admin view of an assignment — includes draft state and roll-up counts. */
export interface AssignmentDto {
  id: string;
  title: string;
  description: string;
  courseId: string;
  courseCode: string;
  classRoomId: string;
  classRoomName: string;
  subjectId: string;
  subjectName: string;
  type: AssignmentType;
  chapterOrLesson: string | null;
  createdByTeacherId: string;
  createdByTeacherName: string;
  weekNumber: number;
  assignedOn: string;
  deadline: string;
  maxMarks: number;
  status: AssignmentStatus;
  allowLateSubmission: boolean;
  allowResubmission: boolean;
  allowComments: boolean;
  publishedAt: string | null;
  createdAt: string;
  submissionCount: number;
  gradedCount: number;
  /** How many students the work was set for — religion courses have a smaller cohort. */
  expectedSubmissionCount: number;
  attachments: AttachmentDto[];
  commentCount: number;
}

export interface SubmissionDto {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  rollNumber: number | null;
  answerText: string;
  status: SubmissionStatus;
  submittedAt: string;
  updatedAt: string | null;
  marks: number | null;
  maxMarks: number;
  feedback: string | null;
  gradedByTeacherName: string | null;
  gradedAt: string | null;
  /** Server-computed: whether this student may still edit. Do not re-derive. */
  canEdit: boolean;
  attachments: AttachmentDto[];
}

/**
 * Student view of an assignment. The API pre-computes `canSubmit` /
 * `isPastDeadline` from the business rules, so the UI never re-implements them.
 */
export interface StudentAssignmentDto {
  id: string;
  title: string;
  description: string;
  courseId: string;
  courseCode: string;
  classRoomName: string;
  subjectName: string;
  type: AssignmentType;
  chapterOrLesson: string | null;
  teacherName: string;
  weekNumber: number;
  assignedOn: string;
  deadline: string;
  maxMarks: number;
  allowLateSubmission: boolean;
  allowResubmission: boolean;
  allowComments: boolean;
  isPastDeadline: boolean;
  canSubmit: boolean;
  attachments: AttachmentDto[];
  commentCount: number;
  mySubmission: SubmissionDto | null;
}

// ---------------------------------------------------------------- routine

export interface RoutineEntryDto {
  courseId: string;
  courseCode: string;
  subjectName: string;
  teacherName: string | null;
  faithGroup: FaithGroup | null;
}

/**
 * One period of one day. `entries` normally holds a single course; the
 * ধর্ম ও নৈতিক শিক্ষা period holds one per faith group, taught in parallel.
 */
export interface RoutineSlotDto {
  periodIndex: number;
  startTime: string;
  endTime: string;
  entries: RoutineEntryDto[];
}

export interface RoutineDayDto {
  day: WeekDay;
  dayName: string;
  periods: RoutineSlotDto[];
}

export interface ClassRoutineDto {
  classRoomId: string;
  classRoomName: string;
  academicYear: string;
  assemblyStart: string;
  assemblyEnd: string;
  breakAfterPeriod: number;
  breakStart: string;
  breakEnd: string;
  days: RoutineDayDto[];
}

export interface TeacherRoutineSlotDto {
  day: WeekDay;
  periodIndex: number;
  startTime: string;
  endTime: string;
  courseId: string;
  courseCode: string;
  classRoomName: string;
  subjectName: string;
}

// -------------------------------------------------------------- reference

export interface EnumOptionDto {
  value: string;
  label: string;
}

export interface PeriodSlotDto {
  periodIndex: number;
  startTime: string;
  endTime: string;
}

/**
 * Bangla labels and school-day constants, served by the API so the frontend
 * does not keep a second, drifting copy of the domain vocabulary.
 */
export interface ReferenceDataDto {
  assignmentTypes: EnumOptionDto[];
  assignmentStatuses: EnumOptionDto[];
  submissionStatuses: EnumOptionDto[];
  roles: EnumOptionDto[];
  faithGroups: EnumOptionDto[];
  weekDays: EnumOptionDto[];
  periods: PeriodSlotDto[];
  periodsPerDay: number;
  teachingDaysPerWeek: number;
  periodsPerWeek: number;
  assemblyStart: string;
  assemblyEnd: string;
  breakAfterPeriod: number;
  breakStart: string;
  breakEnd: string;
}

// ---------------------------------------------------------------- requests

export interface CreateAssignmentRequest {
  title: string;
  description: string;
  courseId: string;
  type: AssignmentType;
  chapterOrLesson?: string | null;
  /** Omit to let the server apply the school's default one-week window. */
  deadline?: string | null;
  maxMarks: number;
  /** Omit to inherit the admin-configured default from AppSettings. */
  allowLateSubmission?: boolean | null;
  allowResubmission?: boolean | null;
  allowComments?: boolean | null;
}

export interface UpdateAssignmentRequest {
  title: string;
  description: string;
  courseId: string;
  type: AssignmentType;
  chapterOrLesson?: string | null;
  deadline: string;
  maxMarks: number;
  allowLateSubmission: boolean;
  allowResubmission: boolean;
  allowComments: boolean;
}

export interface CreateUserRequest {
  fullName: string;
  fullNameEn: string;
  email: string;
  password: string;
  role: UserRole;
  designation?: string | null;
  faith?: FaithGroup | null;
}

/** Email is immutable after creation — it is the login identifier. */
export interface UpdateUserRequest {
  fullName: string;
  fullNameEn: string;
  role: UserRole;
  designation?: string | null;
  faith?: FaithGroup | null;
  isActive: boolean;
}

export interface CreateClassRoomRequest {
  name: string;
  nameEn: string;
  code: string;
  level: number;
  section?: string | null;
  academicYear: string;
}

export interface UpdateClassRoomRequest extends CreateClassRoomRequest {
  isActive: boolean;
}

export interface CreateSubjectRequest {
  name: string;
  nameEn: string;
  code: string;
  textbookName?: string | null;
  fullMarks: number;
  weeklyPeriods: number;
  faithGroup?: FaithGroup | null;
  isOptionalGroup: boolean;
  displayOrder: number;
  allowedAssignmentTypes: AssignmentType[];
}

export interface UpdateSubjectRequest extends CreateSubjectRequest {
  isActive: boolean;
}

export interface CreateCourseRequest {
  classRoomId: string;
  subjectId: string;
  teacherId?: string | null;
}

/** A null teacher leaves the course unstaffed. */
export interface AssignCourseTeacherRequest {
  teacherId: string | null;
}

export interface CreateEnrollmentRequest {
  studentId: string;
  classRoomId: string;
  rollNumber: number;
}

export interface SetRoutinePeriodRequest {
  classRoomId: string;
  day: WeekDay;
  periodIndex: number;
  courseId: string;
}

export interface UpdateAppSettingsRequest {
  settings: Record<string, string>;
}

export interface UpdateSubmissionRequest {
  answerText: string;
}

export interface GradeSubmissionRequest {
  marks: number;
  feedback?: string | null;
}

export interface UpdateSubmissionStatusRequest {
  status: SubmissionStatus;
}

export interface CreateCommentRequest {
  body: string;
}

/** RFC 7807 problem details, as emitted by the API's exception middleware. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
