import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AuthLayout } from './AuthLayout';
import { useAuth } from './AuthProvider';
import { apiClient } from '@/shared/api/client';
import { Button, Input } from '@/shared/components';
import { useToast } from '@/shared/components';

export function MustChangePasswordPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { refreshUser, user } = useAuth();
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const schema = z
    .object({
      currentPassword: z.string().min(1, t('common.required')),
      newPassword: z.string().min(8, t('auth.passwordTooShort')),
      newPasswordConfirm: z.string().min(1),
    })
    .refine((data) => data.newPassword === data.newPasswordConfirm, {
      message: t('auth.passwordMismatch'),
      path: ['newPasswordConfirm'],
    });

  type ChangeForm = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ChangeForm>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: ChangeForm) => {
    setIsSubmitting(true);
    try {
      await apiClient.post('/auth/password/change', {
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      });
      await refreshUser();
      const role = user?.userType ?? 'citizen';
      const home = role === 'jls_admin' ? '/admin/request-types'
        : role === 'jls_officer' ? '/office/inbox' : '/';
      navigate(home);
    } catch {
      toast('error', t('common.error'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <h2 className="text-xl font-semibold text-text-primary mb-2">{t('auth.mustChangePassword')}</h2>
      <p className="text-sm text-text-secondary mb-6">{t('auth.mustChangePasswordDescription')}</p>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          label={t('auth.currentPassword')}
          type="password"
          autoComplete="current-password"
          required
          error={errors.currentPassword?.message}
          {...register('currentPassword')}
        />
        <Input
          label={t('auth.newPassword')}
          type="password"
          autoComplete="new-password"
          required
          error={errors.newPassword?.message}
          {...register('newPassword')}
        />
        <Input
          label={t('auth.newPasswordConfirm')}
          type="password"
          autoComplete="new-password"
          required
          error={errors.newPasswordConfirm?.message}
          {...register('newPasswordConfirm')}
        />
        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? t('common.loading') : t('common.confirm')}
        </Button>
      </form>
    </AuthLayout>
  );
}
