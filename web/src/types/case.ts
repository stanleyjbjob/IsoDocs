export const CaseStatusMap = {
  1: '進行中',
  2: '已結案',
  3: '已作廢',
} as const;

export type CaseStatusValue = keyof typeof CaseStatusMap;

export interface CaseSummaryDto {
  id: string;
  caseNumber: string;
  title: string;
  status: CaseStatusValue;
  documentTypeId: string;
  documentTypeName: string | null;
  customerId: string | null;
  customerName: string | null;
  initiatedAt: string;
  expectedCompletionAt: string | null;
  closedAt: string | null;
  voidedAt: string | null;
  customVersionNumber: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ListCasesParams {
  status?: number;
  documentTypeId?: string;
  initiatedFrom?: string;
  initiatedTo?: string;
  customerId?: string;
  caseNumberPrefix?: string;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface SearchCasesParams extends Omit<ListCasesParams, 'caseNumberPrefix'> {
  keyword: string;
}
