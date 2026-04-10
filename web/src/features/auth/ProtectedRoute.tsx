import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthProvider';
import { LoadingSkeleton } from '@/shared/components';

interface ProtectedRouteProps {
  children: React.ReactNode;
  allowedRoles?: Array<'citizen' | 'jls_officer' | 'jls_admin'>;
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen p-8">
        <LoadingSkeleton lines={4} className="w-full max-w-md" />
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/auth/login" state={{ from: location }} replace />;
  }

  if (user.mustChangePassword && location.pathname !== '/auth/change-password') {
    return <Navigate to="/auth/change-password" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.userType)) {
    const home = user.userType === 'jls_admin' ? '/admin/request-types'
      : user.userType === 'jls_officer' ? '/office/inbox' : '/';
    if (location.pathname !== home) {
      return <Navigate to={home} replace />;
    }
  }

  return <>{children}</>;
}
