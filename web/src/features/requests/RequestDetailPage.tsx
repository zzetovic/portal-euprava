import { useTranslation } from 'react-i18next';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { requestsApi } from './api';
import { DynamicFormRenderer, Button, StatusBadge, LoadingSkeleton, useToast } from '@/shared/components';

function getLocalizedText(i18n: Record<string, string>, lang: string): string {
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${(bytes / 1024).toFixed(0)} KB`;
}

export function RequestDetailPage() {
  const { t, i18n } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const lang = i18n.language;

  const { data: request, isLoading } = useQuery({
    queryKey: ['request', id],
    queryFn: () => requestsApi.getRequest(id!),
    enabled: !!id,
  });

  const deleteMutation = useMutation({
    mutationFn: () => requestsApi.deleteRequest(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['requests'] });
      navigate('/requests');
    },
    onError: () => toast('error', t('common.error')),
  });

  const handleDownload = async (attachmentId: string, filename: string) => {
    try {
      const response = await requestsApi.downloadAttachment(id!, attachmentId);
      const url = URL.createObjectURL(response.data);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast('error', t('common.error'));
    }
  };

  if (isLoading || !request) return <LoadingSkeleton lines={8} />;

  const status = request.status as 'draft' | 'submitted' | 'processing_registry' | 'received_in_registry' | 'rejected_by_officer';

  return (
    <div className="max-w-2xl">
      {/* Status banner */}
      {status === 'draft' && (
        <div className="bg-gray-100 border border-border rounded-lg p-4 mb-6">
          <p className="text-sm text-text-primary mb-3">{t('requests.detail.draftBanner')}</p>
          <div className="flex gap-2">
            <Link to={`/requests/${id}/edit`}>
              <Button size="sm">{t('requests.detail.continueDraft')}</Button>
            </Link>
            <Button
              variant="danger"
              size="sm"
              onClick={() => deleteMutation.mutate()}
              disabled={deleteMutation.isPending}
            >
              {t('requests.detail.deleteDraft')}
            </Button>
          </div>
        </div>
      )}
      {status === 'submitted' && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
          <p className="text-sm text-blue-700">{t('requests.detail.submittedBanner')}</p>
        </div>
      )}
      {status === 'received_in_registry' && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
          <p className="text-sm text-success font-medium">{t('requests.detail.receivedBanner')}</p>
          {request.aktId && (
            <p className="text-sm text-success mt-1">{t('requests.detail.aktId')}: <strong>{request.aktId}</strong></p>
          )}
          <p className="text-xs text-text-secondary mt-2">{t('requests.detail.receivedInfo')}</p>
        </div>
      )}
      {status === 'rejected_by_officer' && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
          <p className="text-sm text-burgundy font-medium">{t('requests.detail.rejectedBanner')}</p>
          {request.rejectionReasonCode && (
            <p className="text-sm text-burgundy mt-1">
              {t('requests.detail.rejectionReason')}: {t(`office.rejectionReasons.${request.rejectionReasonCode}`)}
            </p>
          )}
          <p className="text-xs text-text-secondary mt-2">{t('requests.detail.newRequestSuggestion')}</p>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-xl font-bold text-text-primary">
            {getLocalizedText(request.requestTypeName, lang)}
          </h1>
          <div className="text-sm text-text-secondary mt-1">
            {t('requests.detail.referenceNumber')}: {request.referenceNumber}
          </div>
        </div>
        <StatusBadge status={status} />
      </div>

      {/* Form data */}
      <section className="mb-6">
        <h2 className="text-lg font-semibold text-text-primary mb-3">{t('requests.detail.formData')}</h2>
        <div className="bg-surface border border-border rounded-lg p-4">
          <DynamicFormRenderer
            schema={request.formSchemaSnapshot}
            mode="readonly"
            values={request.formData}
          />
        </div>
      </section>

      {/* Attachments */}
      {request.attachments.length > 0 && (
        <section className="mb-6">
          <h2 className="text-lg font-semibold text-text-primary mb-3">{t('requests.detail.attachments')}</h2>
          <div className="space-y-2">
            {request.attachments.map((att) => (
              <div key={att.id} className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-border">
                <svg className="w-6 h-6 text-text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
                <div className="flex-1 min-w-0">
                  <div className="text-sm text-text-primary truncate">{att.originalFilename}</div>
                  <div className="text-xs text-text-secondary">{formatFileSize(att.sizeBytes)}</div>
                </div>
                <button
                  onClick={() => handleDownload(att.id, att.originalFilename)}
                  className="text-primary hover:underline text-sm"
                >
                  {t('common.download')}
                </button>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* History */}
      {request.history.length > 0 && (
        <section>
          <h2 className="text-lg font-semibold text-text-primary mb-3">{t('requests.detail.history')}</h2>
          <div className="space-y-3">
            {request.history.map((entry) => (
              <div key={entry.id} className="flex gap-3">
                <div className="w-2 h-2 rounded-full bg-primary mt-2 flex-shrink-0" />
                <div>
                  <div className="text-sm text-text-primary">
                    {entry.fromStatus && <StatusBadge status={entry.fromStatus as Parameters<typeof StatusBadge>[0]['status']} className="mr-1" />}
                    <span className="mx-1">&rarr;</span>
                    <StatusBadge status={entry.toStatus as Parameters<typeof StatusBadge>[0]['status']} />
                  </div>
                  {entry.comment && (
                    <p className="text-xs text-text-secondary mt-0.5">{entry.comment}</p>
                  )}
                  <p className="text-xs text-text-secondary">
                    {new Date(entry.changedAt).toLocaleString()}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
