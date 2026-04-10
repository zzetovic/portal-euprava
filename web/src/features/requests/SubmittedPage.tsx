import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { requestsApi } from './api';
import { Button, LoadingSkeleton } from '@/shared/components';

export function SubmittedPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const [copied, setCopied] = useState(false);

  const { data: request, isLoading } = useQuery({
    queryKey: ['request', id],
    queryFn: () => requestsApi.getRequest(id!),
    enabled: !!id,
  });

  if (isLoading || !request) return <LoadingSkeleton lines={4} />;

  const handleCopy = () => {
    navigator.clipboard.writeText(request.referenceNumber);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="max-w-md mx-auto text-center py-8">
      {/* Checkmark */}
      <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-green-100 flex items-center justify-center">
        <svg className="w-8 h-8 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
        </svg>
      </div>

      <h1 className="text-2xl font-bold text-text-primary mb-2">{t('requests.submitted.title')}</h1>

      <p className="text-sm text-text-secondary mb-4">{t('requests.submitted.referenceNumber')}</p>
      <div className="flex items-center justify-center gap-2 mb-6">
        <span className="text-2xl font-bold text-primary">{request.referenceNumber}</span>
        <button
          onClick={handleCopy}
          className="p-2 text-text-secondary hover:text-text-primary transition-colors"
          title={t('common.copyToClipboard')}
        >
          {copied ? (
            <svg className="w-5 h-5 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          ) : (
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
            </svg>
          )}
        </button>
      </div>

      <p className="text-sm text-text-secondary mb-2">{t('requests.submitted.emailSent')}</p>
      <p className="text-sm text-text-secondary mb-8">
        {t('requests.submitted.processingInfo', { days: 5 })}
      </p>

      <div className="flex flex-col sm:flex-row gap-3 justify-center">
        <Link to="/requests">
          <Button variant="secondary">{t('requests.submitted.viewMyRequests')}</Button>
        </Link>
        <Link to="/">
          <Button>{t('requests.submitted.goHome')}</Button>
        </Link>
      </div>
    </div>
  );
}
