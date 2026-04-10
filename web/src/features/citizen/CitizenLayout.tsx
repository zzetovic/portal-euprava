import { useState } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/features/auth/AuthProvider';

const navItems = [
  { key: 'nav.requests', path: '/requests' },
  { key: 'nav.finance', path: '/finance' },
  { key: 'nav.notifications', path: '/notifications' },
] as const;

export function CitizenLayout() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <header className="bg-surface border-b border-border sticky top-0 z-40">
        <div className="max-w-5xl mx-auto px-4 h-14 flex items-center justify-between">
          <Link to="/" className="font-bold text-lg text-primary">
            Portal eUprava
          </Link>

          {/* Desktop nav */}
          <nav className="hidden md:flex items-center gap-6">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`text-sm font-medium transition-colors ${
                  location.pathname.startsWith(item.path)
                    ? 'text-primary'
                    : 'text-text-secondary hover:text-text-primary'
                }`}
              >
                {t(item.key)}
              </Link>
            ))}
          </nav>

          <div className="hidden md:flex items-center gap-4">
            <span className="text-sm text-text-secondary">
              {user?.firstName} {user?.lastName}
            </span>
            <button
              onClick={logout}
              className="text-sm text-text-secondary hover:text-text-primary transition-colors"
            >
              {t('auth.logout')}
            </button>
          </div>

          {/* Mobile hamburger */}
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            className="md:hidden p-2 min-w-touch min-h-touch flex items-center justify-center"
            aria-label="Menu"
          >
            <svg className="w-6 h-6 text-text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              {menuOpen ? (
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              ) : (
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              )}
            </svg>
          </button>
        </div>

        {/* Mobile menu */}
        {menuOpen && (
          <nav className="md:hidden border-t border-border bg-surface px-4 pb-4">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                onClick={() => setMenuOpen(false)}
                className={`block py-3 text-sm font-medium ${
                  location.pathname.startsWith(item.path)
                    ? 'text-primary'
                    : 'text-text-secondary'
                }`}
              >
                {t(item.key)}
              </Link>
            ))}
            <div className="pt-3 border-t border-border mt-2">
              <span className="block text-sm text-text-secondary mb-2">
                {user?.firstName} {user?.lastName}
              </span>
              <button
                onClick={() => { setMenuOpen(false); logout(); }}
                className="text-sm text-error"
              >
                {t('auth.logout')}
              </button>
            </div>
          </nav>
        )}
      </header>

      <main className="flex-1 w-full max-w-5xl mx-auto px-4 py-6">
        <Outlet />
      </main>

      <footer className="bg-surface border-t border-border py-4 text-center text-xs text-text-secondary">
        Portal eUprava &copy; {new Date().getFullYear()}
      </footer>
    </div>
  );
}
