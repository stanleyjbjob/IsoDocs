import { apiClient } from './client';

export async function downloadCasePdf(caseId: string): Promise<void> {
  const response = await apiClient.get<Blob>(`/cases/${caseId}/export/pdf`, {
    responseType: 'blob',
  });

  const url = URL.createObjectURL(response.data);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = `case-${caseId}.pdf`;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}
