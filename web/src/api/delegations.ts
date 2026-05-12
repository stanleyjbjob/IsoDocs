import { apiClient } from './client';

export interface DelegationDto {
  id: string;
  delegatorUserId: string;
  delegateUserId: string;
  startAt: string;
  endAt: string;
  note: string | null;
  isRevoked: boolean;
  isCurrentlyEffective: boolean;
}

export interface CreateDelegationRequest {
  delegateUserId: string;
  startAt: string;
  endAt: string;
  note?: string;
}

export const delegationsApi = {
  list: (delegatorUserId?: string) =>
    apiClient.get<DelegationDto[]>('/delegations', {
      params: delegatorUserId ? { delegatorUserId } : undefined,
    }),

  create: (data: CreateDelegationRequest) =>
    apiClient.post<{ id: string }>('/delegations', data),

  revoke: (id: string) =>
    apiClient.delete(`/delegations/${id}`),
};
