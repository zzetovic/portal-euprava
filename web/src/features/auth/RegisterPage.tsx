import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AuthLayout } from './AuthLayout';
import { useAuth } from './AuthProvider';
import { Button, Input } from '@/shared/components';
import { useToast } from '@/shared/components';

function makeRegisterSchema(t: (key: string) => string) {
  return z
    .object({
      email: z.string().email(t('validation.email')),
      password: z.string().min(8, t('auth.passwordTooShort')),
      passwordConfirm: z.string().min(1),
      firstName: z.string().min(1, t('common.required')),
      lastName: z.string().min(1, t('common.required')),
      oib: z.string().optional(),
      phone: z.string().optional(),
    })
    .refine((data) => data.password === data.passwordConfirm, {
      message: t('auth.passwordMismatch'),
      path: ['passwordConfirm'],
    });
}

type RegisterForm = z.infer<ReturnType<typeof makeRegisterSchema>>;

export function RegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { register: authRegister } = useAuth();
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const schema = makeRegisterSchema(t);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterForm>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: RegisterForm) => {
    setIsSubmitting(true);
    try {
      await authRegister({
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
        oib: data.oib || undefined,
        phone: data.phone || undefined,
      });
      toast('success', t('auth.verifyEmail'));
      navigate('/auth/login');
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 409) {
        toast('error', t('auth.emailAlreadyExists'));
      } else {
        toast('error', t('common.error'));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <h2 className="text-xl font-semibold text-text-primary mb-6">{t('auth.registerTitle')}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label={t('auth.firstName')}
            autoComplete="given-name"
            required
            error={errors.firstName?.message}
            {...register('firstName')}
          />
          <Input
            label={t('auth.lastName')}
            autoComplete="family-name"
            required
            error={errors.lastName?.message}
            {...register('lastName')}
          />
        </div>
        <Input
          label={t('auth.email')}
          type="email"
          autoComplete="email"
          required
          error={errors.email?.message}
          {...register('email')}
        />
        <Input
          label={t('auth.password')}
          type="password"
          autoComplete="new-password"
          required
          error={errors.password?.message}
          {...register('password')}
        />
        <Input
          label={t('auth.passwordConfirm')}
          type="password"
          autoComplete="new-password"
          required
          error={errors.passwordConfirm?.message}
          {...register('passwordConfirm')}
        />
        <Input
          label={t('auth.oibOptional')}
          error={errors.oib?.message}
          {...register('oib')}
        />
        <Input
          label={t('auth.phoneOptional')}
          type="tel"
          autoComplete="tel"
          error={errors.phone?.message}
          {...register('phone')}
        />
        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? t('common.loading') : t('auth.registerButton')}
        </Button>
      </form>
      <p className="mt-4 text-center text-sm text-text-secondary">
        {t('auth.hasAccount')}{' '}
        <Link to="/auth/login" className="text-primary hover:underline">
          {t('auth.login')}
        </Link>
      </p>
    </AuthLayout>
  );
}
