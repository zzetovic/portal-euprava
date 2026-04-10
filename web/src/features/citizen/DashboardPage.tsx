import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider';

const cards = [
  {
    titleKey: 'dashboard.myFinance',
    descKey: 'dashboard.myFinanceDescription',
    path: '/finance',
    icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
  },
  {
    titleKey: 'dashboard.myRequests',
    descKey: 'dashboard.myRequestsDescription',
    path: '/requests',
    icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
  },
  {
    titleKey: 'dashboard.newRequest',
    descKey: 'dashboard.newRequestDescription',
    path: '/requests/new',
    icon: 'M12 4v16m8-8H4',
  },
] as const;

export function DashboardPage() {
  const { t } = useTranslation();
  const { user } = useAuth();

  return (
    <div>
      <h1 className="text-2xl font-bold text-text-primary mb-1">
        {t('dashboard.title')}, {user?.firstName}
      </h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mt-6">
        {cards.map((card) => (
          <Link
            key={card.path}
            to={card.path}
            className="bg-surface rounded-lg border border-border p-5 hover:shadow-md transition-shadow group"
          >
            <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center mb-3 group-hover:bg-primary/20 transition-colors">
              <svg className="w-5 h-5 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={card.icon} />
              </svg>
            </div>
            <h2 className="font-semibold text-text-primary mb-1">{t(card.titleKey)}</h2>
            <p className="text-sm text-text-secondary">{t(card.descKey)}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
