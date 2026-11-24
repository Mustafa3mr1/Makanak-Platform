import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import type { UserType } from '@/shared/api/types';

interface ProtectedRouteProps {
  children: React.ReactNode;
  allowedRoles?: UserType[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, allowedRoles }) => {
  const { isAuthenticated, user } = useAuthStore();
  const location = useLocation();

  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?returnUrl=${returnUrl}`} replace />;
  }

  if (allowedRoles && allowedRoles.length > 0 && user?.userType) {
    if (!allowedRoles.includes(user.userType)) {
      // Redirect to their respective authorized home dashboard
      const fallback = user.userType === 'Owner' ? '/owner' : '/seeker';
      return <Navigate to={fallback} replace />;
    }
  }

  return <>{children}</>;
};
