import { apiClient } from '@/shared/api/client';

export interface RequestTypeListItem {
  id: string;
  code: string;
  nameI18n: Record<string, string>;
  isActive: boolean;
  isArchived: boolean;
  fieldsCount: number;
  attachmentsCount: number;
  version: number;
  updatedAt: string;
}

export interface RequestTypeField {
  id: string;
  fieldKey: string;
  fieldType: string;
  labelI18n: Record<string, string>;
  helpTextI18n?: Record<string, string> | null;
  isRequired: boolean;
  validationRules?: Record<string, unknown> | null;
  options?: Array<{ value: string; labelI18n: Record<string, string> }> | null;
  sortOrder: number;
}

export interface RequestTypeAttachment {
  id: string;
  attachmentKey: string;
  labelI18n: Record<string, string>;
  descriptionI18n?: Record<string, string> | null;
  isRequired: boolean;
  maxSizeBytes: number;
  allowedMimeTypes: string[];
  sortOrder: number;
}

export interface RequestTypeDetail {
  id: string;
  code: string;
  nameI18n: Record<string, string>;
  descriptionI18n?: Record<string, string> | null;
  isActive: boolean;
  isArchived: boolean;
  estimatedProcessingDays?: number | null;
  sortOrder: number;
  version: number;
  fields: RequestTypeField[];
  attachments: RequestTypeAttachment[];
}

export interface RequestTypeUsage {
  drafts: number;
  submitted: number;
  received: number;
  rejected: number;
  total: number;
}

export interface SaveRequestTypePayload {
  nameI18n: Record<string, string>;
  descriptionI18n?: Record<string, string>;
  code: string;
  isActive: boolean;
  estimatedProcessingDays?: number | null;
  sortOrder: number;
  fields: Omit<RequestTypeField, 'id'>[];
  attachments: Omit<RequestTypeAttachment, 'id'>[];
  confirmVersionBump?: boolean;
}

export const adminApi = {
  listRequestTypes: (filter?: string) =>
    apiClient.get<RequestTypeListItem[]>('/admin/request-types', { params: { filter } }).then((r) => r.data),

  getRequestType: (id: string) =>
    apiClient.get<RequestTypeDetail>(`/admin/request-types/${id}`).then((r) => r.data),

  createRequestType: (payload: SaveRequestTypePayload) =>
    apiClient.post<RequestTypeDetail>('/admin/request-types', payload).then((r) => r.data),

  updateRequestType: (id: string, payload: SaveRequestTypePayload) =>
    apiClient.put<RequestTypeDetail>(`/admin/request-types/${id}`, payload).then((r) => r.data),

  deleteRequestType: (id: string) =>
    apiClient.delete(`/admin/request-types/${id}`),

  activateRequestType: (id: string) =>
    apiClient.post(`/admin/request-types/${id}/activate`),

  deactivateRequestType: (id: string) =>
    apiClient.post(`/admin/request-types/${id}/deactivate`),

  duplicateRequestType: (id: string) =>
    apiClient.post<RequestTypeDetail>(`/admin/request-types/${id}/duplicate`).then((r) => r.data),

  getRequestTypeUsage: (id: string) =>
    apiClient.get<RequestTypeUsage>(`/admin/request-types/${id}/usage`).then((r) => r.data),
};
