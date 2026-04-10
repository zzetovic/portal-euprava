import { apiClient } from '@/shared/api/client';
import type { FieldDefinition } from '@/shared/components';

export interface RequestTypePreview {
  id: string;
  code: string;
  nameI18n: Record<string, string>;
  descriptionI18n?: Record<string, string> | null;
  estimatedProcessingDays: number;
  fields: Array<{
    fieldKey: string;
    labelI18n: Record<string, string>;
    isRequired: boolean;
  }>;
  attachments: Array<{
    attachmentKey: string;
    labelI18n: Record<string, string>;
    descriptionI18n?: Record<string, string> | null;
    isRequired: boolean;
    maxSizeBytes: number;
    allowedMimeTypes: string[];
  }>;
}

export interface RequestTypeSchema {
  fields: FieldDefinition[];
}

export interface RequestListItem {
  id: string;
  referenceNumber: string;
  status: string;
  requestTypeName: Record<string, string>;
  aktId?: number | null;
  createdAt: string;
  submittedAt?: string | null;
}

export interface RequestDetail {
  id: string;
  referenceNumber: string;
  status: string;
  formData: Record<string, unknown>;
  formSchemaSnapshot: RequestTypeSchema;
  requestTypeName: Record<string, string>;
  aktId?: number | null;
  rejectionReasonCode?: string | null;
  submittedAt?: string | null;
  reviewedAt?: string | null;
  createdAt: string;
  etag: string;
  expiresAt?: string | null;
  attachments: RequestAttachment[];
  history: StatusHistoryItem[];
}

export interface RequestAttachment {
  id: string;
  attachmentKey: string;
  originalFilename: string;
  mimeType: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface StatusHistoryItem {
  id: string;
  fromStatus: string | null;
  toStatus: string;
  changedBySource: string;
  comment?: string | null;
  changedAt: string;
}

export interface RequestTypeListItem {
  id: string;
  code: string;
  nameI18n: Record<string, string>;
  descriptionI18n?: Record<string, string> | null;
}

export const requestsApi = {
  listTypes: () =>
    apiClient.get<RequestTypeListItem[]>('/request-types').then((r) => r.data),

  getTypePreview: (code: string) =>
    apiClient.get<RequestTypePreview>(`/request-types/${code}`).then((r) => r.data),

  getTypeSchema: (id: string) =>
    apiClient.get<RequestTypeSchema>(`/request-types/${id}/schema`).then((r) => r.data),

  listRequests: (params?: { status?: string; page?: number; size?: number }) =>
    apiClient.get<{ items: RequestListItem[]; total: number }>('/requests', { params }).then((r) => r.data),

  createRequest: (requestTypeId: string) =>
    apiClient.post<RequestDetail>('/requests', { requestTypeId }).then((r) => r.data),

  getRequest: (id: string) =>
    apiClient.get<RequestDetail>(`/requests/${id}`).then((r) => r.data),

  patchRequest: (id: string, formData: Record<string, unknown>, etag: string) =>
    apiClient.patch<RequestDetail>(`/requests/${id}`, { formData }, {
      headers: { 'If-Match': etag },
    }).then((r) => r.data),

  deleteRequest: (id: string) =>
    apiClient.delete(`/requests/${id}`),

  uploadAttachment: (requestId: string, file: File, attachmentKey: string, onProgress?: (pct: number) => void) => {
    const form = new FormData();
    form.append('file', file);
    form.append('attachmentKey', attachmentKey);
    return apiClient.post<RequestAttachment>(`/requests/${requestId}/attachments`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (e) => {
        if (onProgress && e.total) onProgress(Math.round((e.loaded * 100) / e.total));
      },
    }).then((r) => r.data);
  },

  deleteAttachment: (requestId: string, attachmentId: string) =>
    apiClient.delete(`/requests/${requestId}/attachments/${attachmentId}`),

  submitRequest: (id: string) =>
    apiClient.post(`/requests/${id}/submit`),

  downloadAttachment: (requestId: string, attachmentId: string) =>
    apiClient.get(`/requests/${requestId}/attachments/${attachmentId}/download`, { responseType: 'blob' }),

  getHistory: (requestId: string) =>
    apiClient.get<StatusHistoryItem[]>(`/requests/${requestId}/history`).then((r) => r.data),
};
