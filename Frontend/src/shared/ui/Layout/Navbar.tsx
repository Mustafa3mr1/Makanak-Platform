import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Globe, LogOut, Home, Building2, Menu, X } from 'lucide-react';
import { useAuthStore } from '@/features/auth/store/authStore';
import { useRtl } from '@/shared/hooks/useRtl';
import { Button } from '../Button';

export const Navbar: React.FC = () => {
  const { t } = useTranslation();
  const { user, isAuthenticated, logout } = useAuthStore();
  const { toggleLanguage, currentLanguage } = useRtl();
  const navigate = useNavigate();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = React.useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const getDashboardPath = () => {
    if (!user) return '/login';
    if (user.userType === 'Owner') return '/dashboard/owner';
    if (user.userType === 'Seeker') return '/dashboard/seeker';
    return '/admin/offers';
  };

  return (
    <header className="sticky top-0 z-40 bg-white/95 backdrop-blur-md border-b border-slate-100 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Brand Logo */}
          <Link to="/" className="flex items-center gap-2.5">
            <div className="w-10 h-10 rounded-xl bg-brand-primary flex items-center justify-center text-white shadow-soft">
              <Building2 className="w-6 h-6" />
            </div>
            <div className="flex flex-col">
              <span className="font-extrabold text-xl tracking-tight text-slate-900 leading-none">
                Makanak
              </span>
              <span className="text-[10px] font-semibold text-brand-primary uppercase tracking-widest">
                مكانك
              </span>
            </div>
          </Link>

          {/* Desktop Nav Links */}
          <nav className="hidden md:flex items-center gap-8">
            <Link
              to="/properties"
              className="text-sm font-medium text-slate-600 hover:text-brand-primary transition-colors"
            >
              {t('nav.properties', 'Properties')}
            </Link>
            <Link
              to="/_kit"
              className="text-sm font-medium text-slate-400 hover:text-slate-600 transition-colors"
            >
              UI Kit
            </Link>
          </nav>

          {/* Right actions: Lang Switcher & Auth */}
          <div className="hidden md:flex items-center gap-3">
            {/* Language Switch */}
            <button
              onClick={toggleLanguage}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-slate-600 hover:bg-slate-100 border border-slate-200 transition-colors"
              title="Toggle Language"
            >
              <Globe className="w-3.5 h-3.5" />
              <span>{currentLanguage === 'ar' ? 'English' : 'العربية'}</span>
            </button>

            {isAuthenticated && user ? (
              <div className="flex items-center gap-3">
                <Link to={getDashboardPath()}>
                  <Button variant="secondary" size="sm" leftIcon={<Home className="w-4 h-4" />}>
                    {t('nav.dashboard', 'Dashboard')}
                  </Button>
                </Link>
                <div className="flex items-center gap-2 ps-2 border-s border-slate-200">
                  <span className="text-xs font-medium text-slate-700 hidden lg:inline">
                    {user.fullName || user.email}
                  </span>
                  <button
                    onClick={handleLogout}
                    className="p-2 text-slate-400 hover:text-brand-error hover:bg-red-50 rounded-lg transition-colors"
                    title={t('nav.logout', 'Logout')}
                  >
                    <LogOut className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <Link to="/login">
                  <Button variant="text" size="sm">
                    {t('nav.login', 'Sign In')}
                  </Button>
                </Link>
                <Link to="/register">
                  <Button variant="primary" size="sm">
                    {t('nav.register', 'Sign Up')}
                  </Button>
                </Link>
              </div>
            )}
          </div>

          {/* Mobile hamburger */}
          <div className="flex md:hidden items-center gap-2">
            <button
              onClick={toggleLanguage}
              className="p-2 rounded-lg text-slate-600 hover:bg-slate-100"
            >
              <Globe className="w-4 h-4" />
            </button>
            <button
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="p-2 rounded-lg text-slate-600 hover:bg-slate-100"
            >
              {isMobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>
      </div>

      {/* Mobile drawer */}
      {isMobileMenuOpen && (
        <div className="md:hidden border-t border-slate-200 bg-white px-4 pt-2 pb-4 space-y-3">
          <Link
            to="/properties"
            onClick={() => setIsMobileMenuOpen(false)}
            className="block px-3 py-2 rounded-lg text-base font-medium text-slate-700 hover:bg-slate-50"
          >
            {t('nav.properties', 'Properties')}
          </Link>
          <Link
            to="/_kit"
            onClick={() => setIsMobileMenuOpen(false)}
            className="block px-3 py-2 rounded-lg text-base font-medium text-slate-500 hover:bg-slate-50"
          >
            UI Kit Demo
          </Link>
          <div className="pt-2 border-t border-slate-100 flex flex-col gap-2">
            {isAuthenticated ? (
              <>
                <Link to={getDashboardPath()} onClick={() => setIsMobileMenuOpen(false)}>
                  <Button variant="primary" size="md" block>
                    {t('nav.dashboard', 'Dashboard')}
                  </Button>
                </Link>
                <Button variant="danger" size="md" block onClick={handleLogout}>
                  {t('nav.logout', 'Logout')}
                </Button>
              </>
            ) : (
              <>
                <Link to="/login" onClick={() => setIsMobileMenuOpen(false)}>
                  <Button variant="secondary" size="md" block>
                    {t('nav.login', 'Sign In')}
                  </Button>
                </Link>
                <Link to="/register" onClick={() => setIsMobileMenuOpen(false)}>
                  <Button variant="primary" size="md" block>
                    {t('nav.register', 'Sign Up')}
                  </Button>
                </Link>
              </>
            )}
          </div>
        </div>
      )}
    </header>
  );
};
