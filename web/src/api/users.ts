import { apiClient } from './client';

export interface InviteUserRequest {
  email: string;
  displayName: string;
  roleId: string;
  inviteRedirectUrl?: string;
}

export interface InviteUserResult {
  userId: string;
  email: string;
  inviteRedeemUrl: string;
}

export const usersApi = {
  inviteUser: async (request: InviteUserRequest): Promise<InviteUserResult> => {
    const response = await apiClient.post<InviteUserResult>('/users/invite', request);
    return response.data;
  },
};
