import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { requestsApi, type RequestAttachment } from './api';
import { Button, useToast } from '@/shared/components';

interface AttachmentUploaderProps {
  requestId: string;
  attachments: RequestAttachment[];
  schemaAttachments: Array<{
    attachmentKey: string;
    labelI18n: Record<string, string>;
    isRequired: boolean;
    maxSizeBytes: number;
    allowedMimeTypes: string[];
  }>;
  onUploaded: () => void;
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${(bytes / 1024).toFixed(0)} KB`;
}

function getFileIcon(mimeType: string): string {
  if (mimeType.startsWith('image/')) return 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z';
  if (mimeType === 'application/pdf') return 'M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z';
  return 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z';
}

export function AttachmentUploader({ requestId, attachments, onUploaded }: AttachmentUploaderProps) {
  const { t } = useTranslation();
  const { toast } = useToast();
  const [uploading, setUploading] = useState<Record<string, number>>({});

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>, attachmentKey: string) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploading((prev) => ({ ...prev, [attachmentKey]: 0 }));
    try {
      await requestsApi.uploadAttachment(requestId, file, attachmentKey, (pct) => {
        setUploading((prev) => ({ ...prev, [attachmentKey]: pct }));
      });
      onUploaded();
      toast('success', t('common.success'));
    } catch {
      toast('error', t('requests.form.uploadFailed'));
    } finally {
      setUploading((prev) => {
        const next = { ...prev };
        delete next[attachmentKey];
        return next;
      });
      e.target.value = '';
    }
  };

  const handleDelete = async (attachmentId: string) => {
    try {
      await requestsApi.deleteAttachment(requestId, attachmentId);
      onUploaded();
    } catch {
      toast('error', t('common.error'));
    }
  };

  return (
    <div className="space-y-3">
      {attachments.map((att) => (
        <div key={att.id} className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-border">
          <svg className="w-8 h-8 text-text-secondary flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d={getFileIcon(att.mimeType)} />
          </svg>
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium text-text-primary truncate">{att.originalFilename}</div>
            <div className="text-xs text-text-secondary">{formatFileSize(att.sizeBytes)}</div>
          </div>
          <Button variant="danger" size="sm" onClick={() => handleDelete(att.id)}>
            {t('requests.form.removeFile')}
          </Button>
        </div>
      ))}

      {/* Upload button */}
      <label className="flex items-center justify-center gap-2 p-4 border-2 border-dashed border-border rounded-lg cursor-pointer hover:border-primary hover:bg-primary/5 transition-colors">
        <svg className="w-5 h-5 text-text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
        </svg>
        <span className="text-sm text-text-secondary">{t('requests.form.uploadFile')}</span>
        <input
          type="file"
          className="hidden"
          onChange={(e) => handleFileSelect(e, 'general')}
        />
      </label>

      {/* Upload progress */}
      {Object.entries(uploading).map(([key, pct]) => (
        <div key={key} className="flex items-center gap-3 p-3 bg-blue-50 rounded-lg">
          <span className="text-sm text-primary">{t('requests.form.uploading')}</span>
          <div className="flex-1 bg-blue-200 rounded-full h-2">
            <div className="bg-primary h-2 rounded-full transition-all" style={{ width: `${pct}%` }} />
          </div>
          <span className="text-xs text-primary">{pct}%</span>
        </div>
      ))}
    </div>
  );
}
