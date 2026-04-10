import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams, Link } from 'react-router-dom';
import { AuthLayout } from './AuthLayout';
import { apiClient } from '@/shared/api/client';
import { LoadingSkeleton } from '@/shared/components';

export function VerifyEmailPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');

  useEffect(() => {
    const token = searchParams.get('token');
    if (!token) {
      setStatus('error');
      return;
    }

    apiClient
      .post('/auth/verify-email', { token })
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'));
  }, [searchParams]);

  return (
    <AuthLayout>
      <h2 className="text-xl font-semibold text-text-primary mb-6">{t('auth.verifyEmail')}</h2>
      {status === 'loading' && (
        <div>
          <p className="text-text-secondary mb-4">{t('auth.verifyEmailLoading')}</p>
          <LoadingSkeleton lines={2} />
        </div>
      )}
      {status === 'success' && (
        <div>
          <div className="flex items-center justify-center mb-4">
            <div className="w-12 h-12 rounded-full bg-green-100 flex items-center justify-center">
              <svg className="w-6 h-6 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
          </div>
          <p className="text-text-primary text-center mb-4">{t('auth.verifyEmailSuccess')}</p>
          <Link
            to="/auth/login"
            className="block text-center text-primary hover:underline"
          >
            {t('auth.login')}
          </Link>
        </div>
      )}
      {status === 'error' && (
        <div>
          <div className="flex items-center justify-center mb-4">
            <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center">
              <svg className="w-6 h-6 text-error" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
          </div>
          <p className="text-text-primary text-center mb-4">{t('auth.verifyEmailError')}</p>
          <Link
            to="/auth/login"
            className="block text-center text-primary hover:underline"
          >
            {t('auth.login')}
          </Link>
        </div>
      )}
    </AuthLayout>
  );
}
