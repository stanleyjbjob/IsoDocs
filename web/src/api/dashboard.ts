import { apiClient } from './client';

export interface TodoItem {
  caseNodeId: string;
  caseId: string;
  caseNumber: string;
  caseTitle: string;
  nodeName: string;
  nodeOrder: number;
  status: string;
  expectedAt: string | null;
}

export interface CaseSummary {
  id: string;
  caseNumber: string;
  title: string;
  status: string;
  initiatedAt: string;
  expectedCompletionAt: string | null;
  assigneeDisplayName: string | null;
}

export const dashboardApi = {
  getMyTodos: () =>
    apiClient.get<TodoItem[]>('/me/todos').then((r) => r.data),

  getMyInitiatedCases: () =>
    apiClient.get<CaseSummary[]>('/me/initiated-cases').then((r) => r.data),

  getAdminCases: (params?: { status?: string; page?: number; pageSize?: number }) =>
    apiClient.get<CaseSummary[]>('/admin/cases', { params }).then((r) => r.data),
};
