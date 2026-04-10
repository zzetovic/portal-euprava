import { apiClient } from '@/shared/api/client';
import type { FieldDefinition } from '@/shared/components';

export interface InboxItem {
  id: string;
  referenceNumber: string;
  status: string;
  requestTypeName: Record<string, string>;
  applicantName: string;
  applicantOib: string;
  attachmentsCount: number;
  submittedAt: string;
  viewedFirstAt: string | null;
  outboxStatus?: string | null;
}

export interface InboxResponse {
  items: InboxItem[];
  total: number;
}

export interface OfficerRequestDetail {
  id: string;
  referenceNumber: string;
  status: string;
  formData: Record<string, unknown>;
  formSchemaSnapshot: { fields: FieldDefinition[] };
  requestTypeName: Record<string, string>;
  aktId?: number | null;
  rejectionReasonCode?: string | null;
  rejectionInternalNote?: string | null;
  submittedAt: string;
  reviewedAt?: string | null;
  viewedFirstAt?: string | null;
  viewedFirstByUserId?: string | null;
  createdAt: string;
  outboxStatus?: string | null;
  applicant: {
    fullName: string;
    oib: string;
    email: string;
    phone?: string | null;
  };
  attachments: Array<{
    id: string;
    attachmentKey: string;
    originalFilename: string;
    mimeType: string;
    sizeBytes: number;
  }>;
  history: Array<{
    id: string;
    fromStatus: string | null;
    toStatus: string;
    changedBySource: string;
    comment?: string | null;
    changedAt: string;
  }>;
}

export interface RejectionReason {
  code: string;
  labelI18n: Record<string, string>;
}

export interface AcceptResult {
  aktId?: number;
  status: string;
  outboxId?: string;
}

export const officeApi = {
  getInbox: (params: {
    tab?: string;
    requestTypeId?: string;
    search?: string;
    dateFrom?: string;
    dateTo?: string;
    sort?: string;
    page?: number;
    size?: number;
  }) => apiClient.get<InboxResponse>('/office/inbox', { params }).then((r) => r.data),

  getUnreadCount: () =>
    apiClient.get<{ count: number }>('/office/inbox/unread-count').then((r) => r.data),

  getRequest: (id: string) =>
    apiClient.get<OfficerRequestDetail>(`/office/requests/${id}`).then((r) => r.data),

  previewAttachment: (requestId: string, attachmentId: string) =>
    apiClient.get(`/office/requests/${requestId}/attachments/${attachmentId}/preview`, { responseType: 'blob' }),

  downloadAttachment: (requestId: string, attachmentId: string) =>
    apiClient.get(`/office/requests/${requestId}/attachments/${attachmentId}/download`, { responseType: 'blob' }),

  acceptRequest: (id: string) =>
    apiClient.post<AcceptResult>(`/office/requests/${id}/accept`).then((r) => r.data),

  rejectRequest: (id: string, data: { rejectionReasonCode: string; internalNote?: string }) =>
    apiClient.post(`/office/requests/${id}/reject`, data),

  retryAccept: (id: string) =>
    apiClient.post(`/office/requests/${id}/retry-accept`),

  getRejectionReasons: () =>
    apiClient.get<RejectionReason[]>('/office/rejection-reasons').then((r) => r.data),
};
