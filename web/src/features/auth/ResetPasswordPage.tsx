import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams, Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AuthLayout } from './AuthLayout';
import { apiClient } from '@/shared/api/client';
import { Button, Input } from '@/shared/components';
import { useToast } from '@/shared/components';

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const token = searchParams.get('token');

  const schema = z
    .object({
      password: z.string().min(8, t('auth.passwordTooShort')),
      passwordConfirm: z.string().min(1),
    })
    .refine((data) => data.password === data.passwordConfirm, {
      message: t('auth.passwordMismatch'),
      path: ['passwordConfirm'],
    });

  type ResetForm = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetForm>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: ResetForm) => {
    if (!token) return;
    setIsSubmitting(true);
    try {
      await apiClient.post('/auth/password/reset', {
        token,
        newPassword: data.password,
      });
      toast('success', t('auth.resetPasswordSuccess'));
      navigate('/auth/login');
    } catch {
      toast('error', t('common.error'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!token) {
    return (
      <AuthLayout>
        <p className="text-error text-center">{t('auth.verifyEmailError')}</p>
        <Link to="/auth/login" className="block text-center text-primary hover:underline mt-4">
          {t('auth.login')}
        </Link>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <h2 className="text-xl font-semibold text-text-primary mb-2">{t('auth.resetPasswordTitle')}</h2>
      <p className="text-sm text-text-secondary mb-6">{t('auth.resetPasswordDescription')}</p>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          label={t('auth.newPassword')}
          type="password"
          autoComplete="new-password"
          required
          error={errors.password?.message}
          {...register('password')}
        />
        <Input
          label={t('auth.newPasswordConfirm')}
          type="password"
          autoComplete="new-password"
          required
          error={errors.passwordConfirm?.message}
          {...register('passwordConfirm')}
        />
        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? t('common.loading') : t('auth.resetPasswordButton')}
        </Button>
      </form>
    </AuthLayout>
  );
}
