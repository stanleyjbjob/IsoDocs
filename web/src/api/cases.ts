import { apiClient } from './client';

export interface SignOffEntry {
  id: string;
  caseId: string;
  caseNodeId: string | null;
  nodeName: string | null;
  actorUserId: string;
  comment: string | null;
  actionAt: string;
}

export async function getSignOffTrail(caseId: string): Promise<SignOffEntry[]> {
  const res = await apiClient.get<SignOffEntry[]>(`/cases/${caseId}/sign-off-trail`);
  return res.data;
}

export async function submitSignOff(
  caseId: string,
  caseNodeId: string,
  comment?: string,
): Promise<SignOffEntry> {
  const res = await apiClient.post<SignOffEntry>(`/cases/${caseId}/actions/sign-off`, {
    caseNodeId,
    comment,
  });
  return res.data;
}
