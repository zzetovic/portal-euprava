import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { requestsApi } from './api';
import { useAuth } from '@/features/auth/AuthProvider';
import { Button, LoadingSkeleton, useToast } from '@/shared/components';

function getLocalizedText(i18n: Record<string, string> | null | undefined, lang: string): string {
  if (!i18n) return '';
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(0)} MB`;
  return `${(bytes / 1024).toFixed(0)} KB`;
}

export function PreflightPage() {
  const { t, i18n } = useTranslation();
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { toast } = useToast();
  const lang = i18n.language;

  const emailVerified = !!user?.emailVerifiedAt;

  const { data: preview, isLoading } = useQuery({
    queryKey: ['request-type-preview', code],
    queryFn: () => requestsApi.getTypePreview(code!),
    enabled: !!code,
  });

  const createMutation = useMutation({
    mutationFn: () => requestsApi.createRequest(preview!.id),
    onSuccess: (data) => navigate(`/requests/${data.id}/edit`),
    onError: (err: unknown) => {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 429) {
        toast('error', t('requests.dailyLimitReached'));
      } else {
        toast('error', t('common.error'));
      }
    },
  });

  if (isLoading || !preview) {
    return <LoadingSkeleton lines={8} />;
  }

  const requiredFields = preview.fields.filter((f) => f.isRequired);
  const requiredAttachments = preview.attachments.filter((a) => a.isRequired);
  const optionalAttachments = preview.attachments.filter((a) => !a.isRequired);

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold text-text-primary mb-2">
        {getLocalizedText(preview.nameI18n, lang)}
      </h1>
      {preview.descriptionI18n && (
        <p className="text-text-secondary mb-6">{getLocalizedText(preview.descriptionI18n, lang)}</p>
      )}

      {/* What you'll need */}
      <section className="mb-6">
        <h2 className="text-lg font-semibold text-text-primary mb-3">{t('requests.preflight.whatYouNeed')}</h2>

        {requiredFields.length > 0 && (
          <div className="mb-4">
            <h3 className="text-sm font-medium text-text-secondary mb-2">{t('requests.preflight.requiredFields')}</h3>
            <ul className="space-y-1">
              {requiredFields.map((f) => (
                <li key={f.fieldKey} className="flex items-center gap-2 text-sm text-text-primary">
                  <span className="text-error">*</span>
                  {getLocalizedText(f.labelI18n, lang)}
                </li>
              ))}
            </ul>
          </div>
        )}

        {requiredAttachments.length > 0 && (
          <div className="mb-4">
            <h3 className="text-sm font-medium text-text-secondary mb-2">{t('requests.preflight.requiredAttachments')}</h3>
            <ul className="space-y-2">
              {requiredAttachments.map((a) => (
                <li key={a.attachmentKey} className="p-3 bg-gray-50 rounded-lg">
                  <div className="font-medium text-sm text-text-primary">{getLocalizedText(a.labelI18n, lang)}</div>
                  {a.descriptionI18n && (
                    <div className="text-xs text-text-secondary mt-0.5">{getLocalizedText(a.descriptionI18n, lang)}</div>
                  )}
                  <div className="text-xs text-text-secondary mt-1">
                    {t('requests.preflight.format')}: {a.allowedMimeTypes.join(', ')} &middot; {t('requests.preflight.maxSize')}: {formatFileSize(a.maxSizeBytes)}
                  </div>
                </li>
              ))}
            </ul>
          </div>
        )}

        {optionalAttachments.length > 0 && (
          <div>
            <h3 className="text-sm font-medium text-text-secondary mb-2">{t('requests.preflight.optionalAttachments')}</h3>
            <ul className="space-y-2">
              {optionalAttachments.map((a) => (
                <li key={a.attachmentKey} className="p-3 bg-gray-50 rounded-lg">
                  <div className="font-medium text-sm text-text-primary">{getLocalizedText(a.labelI18n, lang)}</div>
                  {a.descriptionI18n && (
                    <div className="text-xs text-text-secondary mt-0.5">{getLocalizedText(a.descriptionI18n, lang)}</div>
                  )}
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>

      {/* What happens next */}
      <section className="mb-8">
        <h2 className="text-lg font-semibold text-text-primary mb-2">{t('requests.preflight.whatHappensNext')}</h2>
        <p className="text-sm text-text-secondary">
          {t('requests.preflight.processingTime', { days: preview.estimatedProcessingDays })}
        </p>
      </section>

      {/* Start button */}
      {!emailVerified && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-4">
          <p className="text-sm text-warning">{t('requests.preflight.emailNotVerified')}</p>
        </div>
      )}

      <div className="sticky bottom-0 bg-gray-50 py-4 -mx-4 px-4 border-t border-border mt-4">
        <Button
          className="w-full"
          size="lg"
          disabled={!emailVerified || createMutation.isPending}
          onClick={() => createMutation.mutate()}
        >
          {createMutation.isPending ? t('common.loading') : t('requests.preflight.startRequest')}
        </Button>
      </div>
    </div>
  );
}
