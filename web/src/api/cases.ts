import { apiClient } from './client';
import type { CaseSummaryDto, PagedResult, ListCasesParams, SearchCasesParams } from '../types/case';

export async function listCases(params?: ListCasesParams): Promise<PagedResult<CaseSummaryDto>> {
  const { data } = await apiClient.get<PagedResult<CaseSummaryDto>>('/cases', { params });
  return data;
}

export async function searchCases(params: SearchCasesParams): Promise<PagedResult<CaseSummaryDto>> {
  const { data } = await apiClient.get<PagedResult<CaseSummaryDto>>('/cases/search', { params });
  return data;
}
