import { lazy, Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ProtectedRoute } from '@/features/auth/ProtectedRoute';
import { CitizenLayout } from '@/features/citizen/CitizenLayout';
import { DashboardPage } from '@/features/citizen/DashboardPage';
import { LoadingSkeleton } from '@/shared/components';

const AuthRoutes = lazy(() => import('@/features/auth/routes'));
const RequestRoutes = lazy(() => import('@/features/requests/routes'));
const FinanceRoutes = lazy(() => import('@/features/finance/routes'));
const OfficeRoutes = lazy(() => import('@/features/office/routes'));
const AdminRoutes = lazy(() => import('@/features/admin/routes'));

function LoadingFallback() {
  const { t } = useTranslation();
  return (
    <div className="flex items-center justify-center min-h-screen p-8">
      <div className="w-full max-w-md text-center">
        <p className="text-text-secondary mb-4">{t('common.loading')}</p>
        <LoadingSkeleton lines={3} />
      </div>
    </div>
  );
}

export function AppRouter() {
  return (
    <Suspense fallback={<LoadingFallback />}>
      <Routes>
        {/* Public auth routes */}
        <Route path="/auth/*" element={<AuthRoutes />} />

        {/* Citizen routes */}
        <Route
          element={
            <ProtectedRoute allowedRoles={['citizen']}>
              <CitizenLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/" element={<DashboardPage />} />
          <Route path="/requests/*" element={<RequestRoutes />} />
          <Route path="/finance/*" element={<FinanceRoutes />} />
        </Route>

        {/* Officer routes */}
        <Route
          path="/office/*"
          element={
            <ProtectedRoute allowedRoles={['jls_officer', 'jls_admin']}>
              <OfficeRoutes />
            </ProtectedRoute>
          }
        />

        {/* Admin routes */}
        <Route
          path="/admin/*"
          element={
            <ProtectedRoute allowedRoles={['jls_admin']}>
              <AdminRoutes />
            </ProtectedRoute>
          }
        />
      </Routes>
    </Suspense>
  );
}
