import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { officeApi, type InboxItem } from './api';
import { EmptyState, TableSkeleton } from '@/shared/components';

type Tab = 'pending' | 'received' | 'rejected' | 'all';

const tabs: { key: Tab; i18nKey: string }[] = [
  { key: 'pending', i18nKey: 'office.inbox.tabs.pending' },
  { key: 'received', i18nKey: 'office.inbox.tabs.received' },
  { key: 'rejected', i18nKey: 'office.inbox.tabs.rejected' },
  { key: 'all', i18nKey: 'office.inbox.tabs.all' },
];

function getLocalizedText(i18n: Record<string, string>, lang: string): string {
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  const days = Math.floor(hours / 24);
  if (days > 0) return `${days}d`;
  if (hours > 0) return `${hours}h`;
  const mins = Math.floor(diff / (1000 * 60));
  return `${mins}m`;
}

export function InboxPage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const [tab, setTab] = useState<Tab>('pending');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['office', 'inbox', tab, search, page, pageSize],
    queryFn: () => officeApi.getInbox({ tab, search: search || undefined, page, size: pageSize }),
  });

  const items = data?.items ?? [];
  const total = data?.total ?? 0;
  const totalPages = Math.ceil(total / pageSize);

  const handleSearch = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setSearch(e.target.value);
    setPage(1);
  }, []);

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-text-primary">{t('office.inbox.title')}</h1>
        <button onClick={() => refetch()} className="text-sm text-primary hover:underline">
          {t('office.inbox.refreshButton')}
        </button>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-100 rounded-lg p-1 mb-4">
        {tabs.map((tb) => (
          <button
            key={tb.key}
            onClick={() => { setTab(tb.key); setPage(1); }}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              tab === tb.key ? 'bg-surface text-text-primary shadow-sm' : 'text-text-secondary hover:text-text-primary'
            }`}
          >
            {t(tb.i18nKey)}
          </button>
        ))}
      </div>

      {/* Search */}
      <input
        type="text"
        value={search}
        onChange={handleSearch}
        placeholder={t('common.search') + '...'}
        className="w-full max-w-md px-3 py-2 border border-border rounded-lg text-sm mb-4 focus:outline-none focus:ring-2 focus:ring-primary"
      />

      {/* Table */}
      {isLoading ? (
        <TableSkeleton rows={5} cols={6} />
      ) : items.length === 0 ? (
        <EmptyState title={t('office.inbox.empty.title')} description={t('office.inbox.empty.description')} />
      ) : (
        <>
          {/* Desktop */}
          <div className="hidden md:block bg-surface rounded-lg border border-border overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-border">
                <tr>
                  <th className="text-left px-4 py-3 font-medium text-text-secondary">{t('office.inbox.columns.status')}</th>
                  <th className="text-left px-4 py-3 font-medium text-text-secondary">{t('office.inbox.columns.type')}</th>
                  <th className="text-left px-4 py-3 font-medium text-text-secondary">{t('office.inbox.columns.applicant')}</th>
                  <th className="text-left px-4 py-3 font-medium text-text-secondary">{t('office.inbox.columns.oib')}</th>
                  <th className="text-left px-4 py-3 font-medium text-text-secondary">{t('office.inbox.columns.referenceNumber')}</th>
                  <th className="text-center px-4 py-3 font-medium text-text-secondary">{t('office.inbox.columns.attachments')}</th>
                  <th className="text-right px-4 py-3 font-medium text-text-secondary">{t('office.inbox.columns.submittedAgo')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {items.map((item) => (
                  <InboxRow key={item.id} item={item} lang={lang} />
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile */}
          <div className="md:hidden space-y-3">
            {items.map((item) => (
              <Link
                key={item.id}
                to={`/office/requests/${item.id}`}
                className="block bg-surface border border-border rounded-lg p-4"
              >
                <div className="flex items-center gap-2 mb-1">
                  <span className={`w-2 h-2 rounded-full ${item.viewedFirstAt ? 'bg-gray-400' : 'bg-primary'}`} />
                  <span className="text-sm font-medium text-text-primary">{getLocalizedText(item.requestTypeName, lang)}</span>
                </div>
                <div className="text-xs text-text-secondary">{item.applicantName} &middot; {item.applicantOib}</div>
                <div className="flex justify-between mt-2 text-xs text-text-secondary">
                  <span>{item.referenceNumber}</span>
                  <span>{timeAgo(item.submittedAt)}</span>
                </div>
                {item.outboxStatus === 'processing' && (
                  <span className="inline-block mt-2 text-xs bg-yellow-100 text-warning px-2 py-0.5 rounded-full">
                    {t('office.accept.asyncStatus')}
                  </span>
                )}
                {item.outboxStatus === 'dead_letter' && (
                  <span className="inline-block mt-2 text-xs bg-red-100 text-error px-2 py-0.5 rounded-full">
                    {t('office.accept.deadLetterBanner')}
                  </span>
                )}
              </Link>
            ))}
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between mt-4">
            <div className="flex items-center gap-2">
              <select
                value={pageSize}
                onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
                className="border border-border rounded px-2 py-1 text-sm"
              >
                {[25, 50, 100].map((s) => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-2 text-sm">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1 border border-border rounded disabled:opacity-30"
              >
                {t('common.back')}
              </button>
              <span className="text-text-secondary">{page} / {totalPages || 1}</span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1 border border-border rounded disabled:opacity-30"
              >
                {t('common.next')}
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function InboxRow({ item, lang }: { item: InboxItem; lang: string }) {
  const { t } = useTranslation();
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <span className={`w-2 h-2 rounded-full flex-shrink-0 ${item.viewedFirstAt ? 'bg-gray-400' : 'bg-primary'}`} />
          <span className="text-xs text-text-secondary">{item.viewedFirstAt ? t('office.inbox.viewed') : t('office.inbox.new')}</span>
        </div>
      </td>
      <td className="px-4 py-3">
        <Link to={`/office/requests/${item.id}`} className={`text-text-primary hover:text-primary ${!item.viewedFirstAt ? 'font-semibold' : ''}`}>
          {getLocalizedText(item.requestTypeName, lang)}
        </Link>
        {item.outboxStatus === 'processing' && (
          <span className="ml-2 text-xs bg-yellow-100 text-warning px-1.5 py-0.5 rounded-full">{t('office.accept.asyncStatus')}</span>
        )}
        {item.outboxStatus === 'dead_letter' && (
          <span className="ml-2 text-xs bg-red-100 text-error px-1.5 py-0.5 rounded-full">{t('office.accept.retryButton')}</span>
        )}
      </td>
      <td className="px-4 py-3 text-text-primary">{item.applicantName}</td>
      <td className="px-4 py-3 text-text-secondary">{item.applicantOib}</td>
      <td className="px-4 py-3 text-text-secondary">{item.referenceNumber}</td>
      <td className="px-4 py-3 text-center text-text-secondary">{item.attachmentsCount}</td>
      <td className="px-4 py-3 text-right text-text-secondary">{timeAgo(item.submittedAt)}</td>
    </tr>
  );
}
