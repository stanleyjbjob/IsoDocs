import { apiClient } from './client';

export interface SetVersionNumberRequest {
  versionNumber: string;
}

export interface VersionNumberResponse {
  versionNumber: string;
}

export async function setCustomVersionNumber(
  caseId: string,
  request: SetVersionNumberRequest,
): Promise<VersionNumberResponse> {
  const { data } = await apiClient.put<VersionNumberResponse>(
    `/cases/${caseId}/version-number`,
    request,
  );
  return data;
}
