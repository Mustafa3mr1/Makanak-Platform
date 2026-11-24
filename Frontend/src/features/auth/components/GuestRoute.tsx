import React from 'react';
import { Navigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

interface GuestRouteProps {
  children: React.ReactNode;
}

export const GuestRoute: React.FC<GuestRouteProps> = ({ children }) => {
  const { isAuthenticated, user } = useAuthStore();
  const [searchParams] = useSearchParams();

  if (isAuthenticated && user) {
    const returnUrl = searchParams.get('returnUrl');
    if (returnUrl) {
      return <Navigate to={decodeURIComponent(returnUrl)} replace />;
    }
    const defaultRoute = user.userType === 'Owner' ? '/owner' : '/seeker';
    return <Navigate to={defaultRoute} replace />;
  }

  return <>{children}</>;
};
