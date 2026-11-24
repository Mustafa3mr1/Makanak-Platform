import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Building2,
  Inbox,
  PlusCircle,
  Home,
  LogOut,
  Layers,
} from 'lucide-react';
import { useAuthStore } from '@/features/auth/store/authStore';

export interface NavItem {
  to: string;
  label: string;
  icon: React.ReactNode;
}

export const Sidebar: React.FC = () => {
  const { t } = useTranslation();
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const isOwner = user?.userType === 'Owner';
  const isSeeker = user?.userType === 'Seeker';
  const isAdmin = user?.userType === 'Admin';

  const navItems: NavItem[] = [
    ...(isOwner
      ? [
          {
            to: '/dashboard/owner',
            label: t('dashboard.myProperties', 'My Properties'),
            icon: <Home className="w-5 h-5" />,
          },
          {
            to: '/properties/new',
            label: t('dashboard.addNewProperty', 'Add Property'),
            icon: <PlusCircle className="w-5 h-5" />,
          },
        ]
      : []),
    ...(isSeeker
      ? [
          {
            to: '/dashboard/seeker',
            label: t('dashboard.offerHistory', 'Offer History'),
            icon: <Inbox className="w-5 h-5" />,
          },
          {
            to: '/properties',
            label: t('dashboard.exploreProperties', 'Explore'),
            icon: <Building2 className="w-5 h-5" />,
          },
        ]
      : []),
    ...(isAdmin
      ? [
          {
            to: '/admin/offers',
            label: t('dashboard.allOffers', 'All Offers Audit'),
            icon: <Layers className="w-5 h-5" />,
          },
        ]
      : []),
  ];

  return (
    <aside className="w-64 bg-white border-e border-slate-100 flex flex-col h-full min-h-screen">
      {/* Brand */}
      <div className="p-6 border-b border-slate-100 flex items-center gap-3">
        <div className="w-9 h-9 rounded-xl bg-brand-primary flex items-center justify-center text-white shadow-soft">
          <Building2 className="w-5 h-5" />
        </div>
        <div>
          <span className="font-extrabold text-lg text-slate-900 block leading-tight">
            Makanak
          </span>
          <span className="text-[11px] font-medium text-slate-400">
            {user?.userType ?? 'Portal'}
          </span>
        </div>
      </div>

      {/* Nav List */}
      <nav className="flex-1 p-4 space-y-1.5 overflow-y-auto">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end
            className={({ isActive }) =>
              `flex items-center gap-3 px-3.5 py-2.5 rounded-xl font-medium text-sm transition-colors ${
                isActive
                  ? 'bg-brand-primary text-white shadow-soft'
                  : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
              }`
            }
          >
            <span>{item.icon}</span>
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      {/* User info & Logout */}
      <div className="p-4 border-t border-slate-100">
        <div className="flex items-center gap-3 p-2 rounded-xl bg-slate-50 mb-3">
          <div className="w-9 h-9 rounded-full bg-brand-primary/10 text-brand-primary flex items-center justify-center font-bold text-sm">
            {user?.fullName?.charAt(0) || user?.email?.charAt(0) || 'U'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-xs font-bold text-slate-900 truncate">
              {user?.fullName || 'User'}
            </p>
            <p className="text-[10px] text-slate-500 truncate">{user?.email}</p>
          </div>
        </div>

        <button
          onClick={handleLogout}
          className="w-full flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-semibold text-red-600 hover:bg-red-50 transition-colors"
        >
          <LogOut className="w-4 h-4" />
          <span>{t('nav.logout', 'Sign Out')}</span>
        </button>
      </div>
    </aside>
  );
};
