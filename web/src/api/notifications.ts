import { apiClient } from './client';

export type NotificationDto = {
  id: string;
  recipientUserId: string;
  caseId: string | null;
  type: string;
  channel: string;
  subject: string;
  body: string;
  isRead: boolean;
  readAt: string | null;
  sentAt: string | null;
  createdAt: string;
};

export type ListNotificationsResult = {
  items: NotificationDto[];
  unreadCount: number;
};

export async function listNotifications(
  unreadOnly = false,
  page = 1,
  pageSize = 20,
): Promise<ListNotificationsResult> {
  const { data } = await apiClient.get<ListNotificationsResult>('/notifications', {
    params: { unreadOnly, page, pageSize },
  });
  return data;
}

export async function markNotificationRead(id: string): Promise<void> {
  await apiClient.patch(`/notifications/${id}/read`);
}

export async function markAllNotificationsRead(): Promise<void> {
  await apiClient.post('/notifications/mark-all-read');
}
