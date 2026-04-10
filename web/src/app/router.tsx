import { lazy, Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const AuthRoutes = lazy(() => import('@/features/auth/routes'));
const RequestRoutes = lazy(() => import('@/features/requests/routes'));
const FinanceRoutes = lazy(() => import('@/features/finance/routes'));
const OfficeRoutes = lazy(() => import('@/features/office/routes'));
const AdminRoutes = lazy(() => import('@/features/admin/routes'));

function LoadingFallback() {
  const { t } = useTranslation();
  return <div className="flex items-center justify-center min-h-screen">{t('common.loading')}</div>;
}

export function AppRouter() {
  return (
    <Suspense fallback={<LoadingFallback />}>
      <Routes>
        <Route path="/auth/*" element={<AuthRoutes />} />
        <Route path="/requests/*" element={<RequestRoutes />} />
        <Route path="/finance/*" element={<FinanceRoutes />} />
        <Route path="/office/*" element={<OfficeRoutes />} />
        <Route path="/admin/*" element={<AdminRoutes />} />
        <Route path="/" element={<div>Portal eUprava</div>} />
      </Routes>
    </Suspense>
  );
}
