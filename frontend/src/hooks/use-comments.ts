"use client";

import { api } from "@/lib/api-client";
import type { AssignmentCommentDto, CreateCommentRequest } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

/**
 * The class conversation under an assignment post. One set of hooks serves both
 * the student and the teacher view: the API decides who may read the thread,
 * who may post, and which comments the caller may delete, and returns
 * `canDelete` per comment so neither UI re-derives that rule.
 */
const key = (assignmentId: string) =>
  ["assignment", assignmentId, "comments"] as const;

export function useAssignmentComments(assignmentId: string, enabled = true) {
  return useQuery({
    queryKey: key(assignmentId),
    queryFn: () =>
      api.get<AssignmentCommentDto[]>(`/assignments/${assignmentId}/comments`),
    enabled: enabled && !!assignmentId,
  });
}

export function useAddComment(assignmentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateCommentRequest) =>
      api.post<AssignmentCommentDto>(
        `/assignments/${assignmentId}/comments`,
        body,
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key(assignmentId) });
      // The comment count rides along on the assignment payloads.
      qc.invalidateQueries({ queryKey: ["assignment", assignmentId] });
      qc.invalidateQueries({ queryKey: ["student"] });
    },
  });
}

export function useDeleteComment(assignmentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (commentId: string) =>
      api.delete<void>(`/assignments/${assignmentId}/comments/${commentId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key(assignmentId) });
      qc.invalidateQueries({ queryKey: ["assignment", assignmentId] });
      qc.invalidateQueries({ queryKey: ["student"] });
    },
  });
}
