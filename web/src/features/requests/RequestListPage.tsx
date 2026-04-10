import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { requestsApi } from './api';
import { Button, StatusBadge, EmptyState, CardSkeleton } from '@/shared/components';

type FilterKey = 'all' | 'draft' | 'submitted' | 'received_in_registry' | 'rejected_by_officer';

const filters: { key: FilterKey; i18nKey: string }[] = [
  { key: 'all', i18nKey: 'requests.filter.all' },
  { key: 'draft', i18nKey: 'requests.filter.drafts' },
  { key: 'submitted', i18nKey: 'requests.filter.inProgress' },
  { key: 'received_in_registry', i18nKey: 'requests.filter.received' },
  { key: 'rejected_by_officer', i18nKey: 'requests.filter.rejected' },
];

function getLocalizedText(i18n: Record<string, string>, lang: string): string {
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

export function RequestListPage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const [activeFilter, setActiveFilter] = useState<FilterKey>('all');

  const { data, isLoading } = useQuery({
    queryKey: ['requests', activeFilter],
    queryFn: () => requestsApi.listRequests({
      status: activeFilter === 'all' ? undefined : activeFilter,
    }),
  });

  const items = data?.items ?? [];

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-text-primary">{t('requests.title')}</h1>
        <Link to="/requests/new">
          <Button>{t('requests.newRequest')}</Button>
        </Link>
      </div>

      {/* Filter chips */}
      <div className="flex gap-2 mb-4 overflow-x-auto pb-1">
        {filters.map((f) => (
          <button
            key={f.key}
            onClick={() => setActiveFilter(f.key)}
            className={`px-3 py-1.5 rounded-full text-sm font-medium whitespace-nowrap transition-colors ${
              activeFilter === f.key
                ? 'bg-primary text-white'
                : 'bg-gray-100 text-text-secondary hover:bg-gray-200'
            }`}
          >
            {t(f.i18nKey)}
          </button>
        ))}
      </div>

      {/* List */}
      {isLoading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }, (_, i) => <CardSkeleton key={i} />)}
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          title={t('requests.empty.title')}
          description={t('requests.empty.description')}
          action={
            <Link to="/requests/new">
              <Button>{t('requests.empty.cta')}</Button>
            </Link>
          }
        />
      ) : (
        <div className="space-y-3">
          {items.map((item) => (
            <Link
              key={item.id}
              to={`/requests/${item.id}`}
              className="block bg-surface border border-border rounded-lg p-4 hover:shadow-sm transition-shadow"
            >
              <div className="flex items-start justify-between gap-2 mb-1">
                <div className="font-medium text-text-primary text-sm">
                  {getLocalizedText(item.requestTypeName, lang)}
                </div>
                <StatusBadge status={item.status as Parameters<typeof StatusBadge>[0]['status']} />
              </div>
              <div className="text-xs text-text-secondary">
                {item.referenceNumber}
                {item.aktId && (
                  <span className="ml-2">{t('requests.detail.aktId')}: {item.aktId}</span>
                )}
              </div>
              <div className="text-xs text-text-secondary mt-1">
                {new Date(item.createdAt).toLocaleDateString()}
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
