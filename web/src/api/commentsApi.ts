import { apiClient } from './client';

export interface CommentDto {
  id: string;
  caseId: string;
  authorUserId: string;
  body: string;
  parentCommentId: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface AddCommentRequest {
  body: string;
  parentCommentId?: string;
}

export const commentsApi = {
  list: (caseId: string) =>
    apiClient.get<CommentDto[]>(`/cases/${caseId}/comments`).then((r) => r.data),

  add: (caseId: string, request: AddCommentRequest) =>
    apiClient.post<CommentDto>(`/cases/${caseId}/comments`, request).then((r) => r.data),
};
