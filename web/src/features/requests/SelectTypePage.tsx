import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { requestsApi } from './api';
import { LoadingSkeleton, EmptyState } from '@/shared/components';

function getLocalizedText(i18n: Record<string, string> | null | undefined, lang: string): string {
  if (!i18n) return '';
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

export function SelectTypePage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const [search, setSearch] = useState('');

  const { data: types = [], isLoading } = useQuery({
    queryKey: ['request-types'],
    queryFn: requestsApi.listTypes,
  });

  const filtered = types.filter((rt) => {
    if (!search) return true;
    return getLocalizedText(rt.nameI18n, lang).toLowerCase().includes(search.toLowerCase());
  });

  return (
    <div>
      <h1 className="text-xl font-bold text-text-primary mb-4">{t('requests.selectType')}</h1>
      <input
        type="text"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder={t('requests.searchTypes')}
        className="w-full px-4 py-3 border border-border rounded-lg text-base mb-4 focus:outline-none focus:ring-2 focus:ring-primary"
      />
      {isLoading ? (
        <LoadingSkeleton lines={5} />
      ) : filtered.length === 0 ? (
        <EmptyState title={t('common.noResults')} />
      ) : (
        <div className="space-y-2">
          {filtered.map((rt) => (
            <Link
              key={rt.id}
              to={`/requests/new/${rt.code}`}
              className="flex items-center justify-between p-4 bg-surface border border-border rounded-lg hover:shadow-sm transition-shadow group"
            >
              <div>
                <div className="font-medium text-text-primary group-hover:text-primary">
                  {getLocalizedText(rt.nameI18n, lang)}
                </div>
                {rt.descriptionI18n && (
                  <div className="text-sm text-text-secondary mt-0.5 line-clamp-1">
                    {getLocalizedText(rt.descriptionI18n, lang)}
                  </div>
                )}
              </div>
              <svg className="w-5 h-5 text-text-secondary group-hover:text-primary flex-shrink-0 ml-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
