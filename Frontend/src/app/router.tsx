import React from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { PublicLayout, DashboardLayout } from '@/shared/ui/Layout/AppShell';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { GuestRoute } from '@/features/auth/components/GuestRoute';
import { UiKitDemoPage } from '@/features/dev/UiKitDemoPage';
import { ErrorDemoPage } from '@/features/dev/ErrorDemoPage';

// Placeholder screen component for subsequent phases
const PlaceholderScreen: React.FC<{ title: string; phase: string }> = ({ title, phase }) => (
  <div className="max-w-4xl mx-auto py-16 px-4 text-center">
    <div className="inline-block p-4 bg-blue-50 text-brand-primary rounded-2xl mb-4 font-bold text-sm">
      Phase {phase} Scope
    </div>
    <h2 className="text-2xl font-extrabold text-slate-900 mb-2">{title}</h2>
    <p className="text-sm text-slate-500 max-w-md mx-auto">
      This page will be implemented in its designated phase per the approved roadmap.
    </p>
  </div>
);

export const router = createBrowserRouter([
  {
    path: '/',
    element: <PublicLayout />,
    children: [
      {
        index: true,
        element: <PlaceholderScreen title="Home: Hero Search & Featured Properties" phase="2" />,
      },
      {
        path: 'properties',
        element: <PlaceholderScreen title="Browse Properties & Filters" phase="2" />,
      },
      {
        path: 'properties/:publicId',
        element: <PlaceholderScreen title="Property Details & Offer Modal" phase="2" />,
      },
      {
        path: '_kit',
        element: <UiKitDemoPage />,
      },
      {
        path: '_kit/error-demo',
        element: <ErrorDemoPage />,
      },
      {
        path: 'login',
        element: (
          <GuestRoute>
            <PlaceholderScreen title="Sign In Screen" phase="1/2" />
          </GuestRoute>
        ),
      },
      {
        path: 'register',
        element: (
          <GuestRoute>
            <PlaceholderScreen title="Sign Up Screen (Role Choice: Seeker or Owner)" phase="1/2" />
          </GuestRoute>
        ),
      },
    ],
  },
  {
    path: '/',
    element: <DashboardLayout />,
    children: [
      {
        path: 'dashboard/seeker',
        element: (
          <ProtectedRoute allowedRoles={['Seeker']}>
            <PlaceholderScreen title="Seeker Dashboard: Offer History" phase="3" />
          </ProtectedRoute>
        ),
      },
      {
        path: 'dashboard/owner',
        element: (
          <ProtectedRoute allowedRoles={['Owner']}>
            <PlaceholderScreen title="Owner Dashboard: My Properties & Active Counters" phase="4" />
          </ProtectedRoute>
        ),
      },
      {
        path: 'properties/new',
        element: (
          <ProtectedRoute allowedRoles={['Owner']}>
            <PlaceholderScreen title="Create Property Form" phase="4" />
          </ProtectedRoute>
        ),
      },
      {
        path: 'admin/offers',
        element: (
          <ProtectedRoute allowedRoles={['Admin']}>
            <PlaceholderScreen title="Admin Audit: All Offers" phase="4" />
          </ProtectedRoute>
        ),
      },
    ],
  },
  {
    path: '*',
    element: (
      <div className="min-h-screen flex flex-col items-center justify-center p-6 text-center">
        <h1 className="text-6xl font-black text-slate-300 mb-2">404</h1>
        <p className="text-lg font-bold text-slate-800 mb-4">Page Not Found</p>
        <a href="/" className="text-sm font-semibold text-brand-primary hover:underline">
          Return to Home
        </a>
      </div>
    ),
  },
]);
