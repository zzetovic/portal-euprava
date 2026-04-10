import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminApi, type RequestTypeListItem } from './api';
import { Button, Modal, EmptyState, useToast, TableSkeleton } from '@/shared/components';

type Filter = 'active' | 'all' | 'archived';

function getLocalizedName(i18n: Record<string, string>, lang: string): string {
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

export function RequestTypeListPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const lang = i18n.language;

  const [filter, setFilter] = useState<Filter>('active');
  const [search, setSearch] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<RequestTypeListItem | null>(null);
  const [duplicateTarget, setDuplicateTarget] = useState<RequestTypeListItem | null>(null);

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['admin', 'request-types', filter],
    queryFn: () => adminApi.listRequestTypes(filter),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminApi.deleteRequestType(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'request-types'] });
      toast('success', t('common.success'));
      setDeleteTarget(null);
    },
    onError: () => toast('error', t('admin.requestTypes.deleteBlocked')),
  });

  const duplicateMutation = useMutation({
    mutationFn: (id: string) => adminApi.duplicateRequestType(id),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'request-types'] });
      toast('success', t('common.success'));
      setDuplicateTarget(null);
      navigate(`/admin/request-types/${data.id}`);
    },
    onError: () => toast('error', t('common.error')),
  });

  const toggleMutation = useMutation({
    mutationFn: (item: RequestTypeListItem) =>
      item.isActive ? adminApi.deactivateRequestType(item.id) : adminApi.activateRequestType(item.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'request-types'] });
      toast('success', t('common.success'));
    },
    onError: () => toast('error', t('common.error')),
  });

  const filtered = items.filter((item) => {
    if (!search) return true;
    const name = getLocalizedName(item.nameI18n, lang).toLowerCase();
    return name.includes(search.toLowerCase()) || item.code.toLowerCase().includes(search.toLowerCase());
  });

  const filters: { key: Filter; label: string }[] = [
    { key: 'active', label: t('admin.requestTypes.filter.active') },
    { key: 'all', label: t('admin.requestTypes.filter.all') },
    { key: 'archived', label: t('admin.requestTypes.filter.archived') },
  ];

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <h1 className="text-xl font-bold text-text-primary">{t('admin.requestTypes.title')}</h1>
        <Link to="/admin/request-types/new">
          <Button>{t('admin.requestTypes.newType')}</Button>
        </Link>
      </div>

      {/* Filters + search */}
      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="flex gap-1 bg-gray-100 rounded-lg p-1">
          {filters.map((f) => (
            <button
              key={f.key}
              onClick={() => setFilter(f.key)}
              className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                filter === f.key ? 'bg-surface text-text-primary shadow-sm' : 'text-text-secondary hover:text-text-primary'
              }`}
            >
              {f.label}
            </button>
          ))}
        </div>
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('common.search') + '...'}
          className="px-3 py-2 border border-border rounded-lg text-sm flex-1 max-w-xs focus:outline-none focus:ring-2 focus:ring-primary"
        />
      </div>

      {/* Content */}
      {isLoading ? (
        <TableSkeleton rows={5} cols={5} />
      ) : filtered.length === 0 ? (
        <EmptyState title={t('common.noResults')} />
      ) : (
        <>
          {/* Desktop table */}
          <div className="hidden md:block bg-surface rounded-lg border border-border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-border">
                <tr>
                  <th className="text-left px-4 py-3 font-medium text-text-secondary">{t('admin.requestTypes.columns.name')}</th>
                  <th className="text-left px-4 py-3 font-medium text-text-secondary">{t('admin.requestTypes.columns.status')}</th>
                  <th className="text-center px-4 py-3 font-medium text-text-secondary">{t('admin.requestTypes.columns.fields')}</th>
                  <th className="text-center px-4 py-3 font-medium text-text-secondary">{t('admin.requestTypes.columns.attachments')}</th>
                  <th className="text-right px-4 py-3 font-medium text-text-secondary">{t('admin.requestTypes.columns.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {filtered.map((item) => (
                  <tr key={item.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <Link to={`/admin/request-types/${item.id}`} className="font-medium text-text-primary hover:text-primary">
                        {getLocalizedName(item.nameI18n, lang)}
                      </Link>
                      <div className="text-xs text-text-secondary">{item.code}</div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${
                        item.isArchived
                          ? 'bg-gray-100 text-gray-600'
                          : item.isActive
                          ? 'bg-green-100 text-success'
                          : 'bg-yellow-100 text-warning'
                      }`}>
                        {item.isArchived ? t('common.archived') : item.isActive ? t('common.active') : t('common.inactive')}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center text-text-secondary">{item.fieldsCount}</td>
                    <td className="px-4 py-3 text-center text-text-secondary">{item.attachmentsCount}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <Link to={`/admin/request-types/${item.id}`}>
                          <Button variant="secondary" size="sm">{t('common.edit')}</Button>
                        </Link>
                        {!item.isArchived && (
                          <Button
                            variant="secondary"
                            size="sm"
                            onClick={() => toggleMutation.mutate(item)}
                          >
                            {item.isActive ? t('admin.requestTypes.deactivate') : t('admin.requestTypes.activate')}
                          </Button>
                        )}
                        <Button variant="secondary" size="sm" onClick={() => setDuplicateTarget(item)}>
                          {t('admin.requestTypes.duplicate')}
                        </Button>
                        {!item.isArchived && (
                          <Button variant="danger" size="sm" onClick={() => setDeleteTarget(item)}>
                            {t('admin.requestTypes.archive')}
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile cards */}
          <div className="md:hidden space-y-3">
            {filtered.map((item) => (
              <div key={item.id} className="bg-surface rounded-lg border border-border p-4">
                <div className="flex items-start justify-between mb-2">
                  <Link to={`/admin/request-types/${item.id}`} className="font-medium text-text-primary hover:text-primary">
                    {getLocalizedName(item.nameI18n, lang)}
                  </Link>
                  <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${
                    item.isArchived
                      ? 'bg-gray-100 text-gray-600'
                      : item.isActive
                      ? 'bg-green-100 text-success'
                      : 'bg-yellow-100 text-warning'
                  }`}>
                    {item.isArchived ? t('common.archived') : item.isActive ? t('common.active') : t('common.inactive')}
                  </span>
                </div>
                <div className="text-xs text-text-secondary mb-3">{item.code}</div>
                <div className="flex gap-4 text-xs text-text-secondary mb-3">
                  <span>{t('admin.requestTypes.columns.fields')}: {item.fieldsCount}</span>
                  <span>{t('admin.requestTypes.columns.attachments')}: {item.attachmentsCount}</span>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Link to={`/admin/request-types/${item.id}`}>
                    <Button variant="secondary" size="sm">{t('common.edit')}</Button>
                  </Link>
                  <Button variant="secondary" size="sm" onClick={() => setDuplicateTarget(item)}>
                    {t('admin.requestTypes.duplicate')}
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      {/* Delete modal */}
      <Modal isOpen={!!deleteTarget} onClose={() => setDeleteTarget(null)} title={t('admin.requestTypes.archive')}>
        <p className="text-sm text-text-secondary mb-4">{t('admin.requestTypes.deleteWarning')}</p>
        <div className="flex justify-end gap-3">
          <Button variant="secondary" onClick={() => setDeleteTarget(null)}>{t('common.cancel')}</Button>
          <Button variant="danger" onClick={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}>
            {t('admin.requestTypes.archive')}
          </Button>
        </div>
      </Modal>

      {/* Duplicate modal */}
      <Modal isOpen={!!duplicateTarget} onClose={() => setDuplicateTarget(null)} title={t('admin.requestTypes.duplicate')}>
        <p className="text-sm text-text-secondary mb-4">
          {t('admin.requestTypes.duplicateConfirm', { name: duplicateTarget ? getLocalizedName(duplicateTarget.nameI18n, lang) : '' })}
        </p>
        <div className="flex justify-end gap-3">
          <Button variant="secondary" onClick={() => setDuplicateTarget(null)}>{t('common.cancel')}</Button>
          <Button onClick={() => duplicateTarget && duplicateMutation.mutate(duplicateTarget.id)}>
            {t('admin.requestTypes.duplicate')}
          </Button>
        </div>
      </Modal>
    </div>
  );
}
