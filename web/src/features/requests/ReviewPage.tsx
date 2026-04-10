import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { requestsApi } from './api';
import { DynamicFormRenderer, Button, Checkbox, LoadingSkeleton, useToast } from '@/shared/components';

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${(bytes / 1024).toFixed(0)} KB`;
}

export function ReviewPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const [confirmed, setConfirmed] = useState(false);

  const { data: request, isLoading } = useQuery({
    queryKey: ['request', id],
    queryFn: () => requestsApi.getRequest(id!),
    enabled: !!id,
  });

  const submitMutation = useMutation({
    mutationFn: () => requestsApi.submitRequest(id!),
    onSuccess: () => navigate(`/requests/${id}/submitted`),
    onError: () => toast('error', t('common.error')),
  });

  if (isLoading || !request) return <LoadingSkeleton lines={8} />;

  return (
    <div className="max-w-2xl">
      <h1 className="text-xl font-bold text-text-primary mb-6">{t('requests.review.title')}</h1>

      {/* Form data summary */}
      <section className="mb-6">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-semibold text-text-primary">{t('requests.detail.formData')}</h2>
          <Link to={`/requests/${id}/edit`} className="text-sm text-primary hover:underline">
            {t('common.edit')}
          </Link>
        </div>
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
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Confirmation */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-4">
        <p className="text-sm text-warning mb-3">{t('requests.review.submitWarning')}</p>
        <Checkbox
          label={t('requests.review.confirmCheckbox')}
          checked={confirmed}
          onChange={(e) => setConfirmed((e.target as HTMLInputElement).checked)}
        >
          {t('requests.review.confirmCheckbox')}
        </Checkbox>
      </div>

      {/* Sticky footer */}
      <div className="sticky bottom-0 bg-gray-50 py-4 -mx-4 px-4 border-t border-border flex gap-3 justify-end">
        <Button variant="secondary" onClick={() => navigate(`/requests/${id}/edit`)}>
          {t('common.back')}
        </Button>
        <Button
          onClick={() => submitMutation.mutate()}
          disabled={!confirmed || submitMutation.isPending}
        >
          {submitMutation.isPending ? t('common.loading') : t('requests.review.submitRequest')}
        </Button>
      </div>
    </div>
  );
}
