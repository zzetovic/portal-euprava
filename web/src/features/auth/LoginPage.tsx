import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AuthLayout } from './AuthLayout';
import { useAuth } from './AuthProvider';
import { Button, Input } from '@/shared/components';
import { useToast } from '@/shared/components';

function homeForRole(role: string): string {
  switch (role) {
    case 'jls_admin': return '/admin/request-types';
    case 'jls_officer': return '/office/inbox';
    default: return '/';
  }
}

const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginForm) => {
    setIsSubmitting(true);
    try {
      const result = await login(data.email, data.password);
      if (result.mustChangePassword) {
        navigate('/auth/change-password', { replace: true });
      } else {
        navigate(from ?? homeForRole(result.userType), { replace: true });
      }
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        toast('error', t('auth.invalidCredentials'));
      } else if (status === 403) {
        toast('error', t('auth.emailNotVerified'));
      } else {
        toast('error', t('common.error'));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <h2 className="text-xl font-semibold text-text-primary mb-6">{t('auth.loginTitle')}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          label={t('auth.email')}
          type="email"
          autoComplete="email"
          error={errors.email?.message}
          {...register('email')}
        />
        <Input
          label={t('auth.password')}
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <div className="flex items-center justify-end">
          <Link to="/auth/forgot-password" className="text-sm text-primary hover:underline">
            {t('auth.forgotPassword')}
          </Link>
        </div>
        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? t('common.loading') : t('auth.loginButton')}
        </Button>
      </form>
      <p className="mt-4 text-center text-sm text-text-secondary">
        {t('auth.noAccount')}{' '}
        <Link to="/auth/register" className="text-primary hover:underline">
          {t('auth.register')}
        </Link>
      </p>
    </AuthLayout>
  );
}
