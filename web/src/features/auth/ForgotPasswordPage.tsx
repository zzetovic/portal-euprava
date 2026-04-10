import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AuthLayout } from './AuthLayout';
import { apiClient } from '@/shared/api/client';
import { Button, Input } from '@/shared/components';

const schema = z.object({
  email: z.string().email(),
});

type ForgotForm = z.infer<typeof schema>;

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [sent, setSent] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotForm>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: ForgotForm) => {
    setIsSubmitting(true);
    try {
      await apiClient.post('/auth/password/forgot', { email: data.email });
    } finally {
      setSent(true);
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <h2 className="text-xl font-semibold text-text-primary mb-2">{t('auth.forgotPasswordTitle')}</h2>
      {!sent ? (
        <>
          <p className="text-sm text-text-secondary mb-6">{t('auth.forgotPasswordDescription')}</p>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input
              label={t('auth.email')}
              type="email"
              autoComplete="email"
              error={errors.email?.message}
              {...register('email')}
            />
            <Button type="submit" disabled={isSubmitting} className="w-full">
              {isSubmitting ? t('common.loading') : t('auth.forgotPasswordButton')}
            </Button>
          </form>
        </>
      ) : (
        <div>
          <div className="flex items-center justify-center mb-4 mt-4">
            <div className="w-12 h-12 rounded-full bg-green-100 flex items-center justify-center">
              <svg className="w-6 h-6 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
          </div>
          <p className="text-text-primary text-center mb-4">{t('auth.forgotPasswordSuccess')}</p>
        </div>
      )}
      <p className="mt-4 text-center text-sm text-text-secondary">
        <Link to="/auth/login" className="text-primary hover:underline">
          {t('auth.login')}
        </Link>
      </p>
    </AuthLayout>
  );
}
