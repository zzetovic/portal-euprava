import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { FormProvider, useForm } from 'react-hook-form';
import { adminApi, type RequestTypeField, type RequestTypeAttachment, type RequestTypeDetail, type SaveRequestTypePayload } from './api';
import { BasicTab } from './tabs/BasicTab';
import { FieldsTab } from './tabs/FieldsTab';
import { AttachmentsTab } from './tabs/AttachmentsTab';
import { PreviewTab } from './tabs/PreviewTab';
import { Button, Modal, LoadingSkeleton, useToast } from '@/shared/components';

interface FormValues {
  nameHr: string;
  nameEn: string;
  code: string;
  descriptionHr: string;
  descriptionEn: string;
  isActive: boolean;
  estimatedProcessingDays: string;
  sortOrder: string;
}

const tabs = ['basic', 'fields', 'attachments', 'preview'] as const;
type Tab = typeof tabs[number];

export function RequestTypeEditPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const isNew = id === 'new';

  const [activeTab, setActiveTab] = useState<Tab>('basic');
  const [fields, setFields] = useState<RequestTypeField[]>([]);
  const [attachments, setAttachments] = useState<RequestTypeAttachment[]>([]);
  const [versionBumpModal, setVersionBumpModal] = useState(false);
  const [pendingPayload, setPendingPayload] = useState<SaveRequestTypePayload | null>(null);

  const methods = useForm<FormValues>({
    defaultValues: {
      nameHr: '', nameEn: '', code: '', descriptionHr: '', descriptionEn: '',
      isActive: true, estimatedProcessingDays: '', sortOrder: '0',
    },
  });

  const { data: existing, isLoading } = useQuery({
    queryKey: ['admin', 'request-types', id],
    queryFn: () => adminApi.getRequestType(id!),
    enabled: !isNew,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    placeholderData: undefined as any,
  });

  // Populate form when data loads
  const [populated, setPopulated] = useState(false);
  if (existing && !populated) {
    methods.reset({
      nameHr: existing.nameI18n.hr ?? '',
      nameEn: existing.nameI18n.en ?? '',
      code: existing.code,
      descriptionHr: existing.descriptionI18n?.hr ?? '',
      descriptionEn: existing.descriptionI18n?.en ?? '',
      isActive: existing.isActive,
      estimatedProcessingDays: existing.estimatedProcessingDays?.toString() ?? '',
      sortOrder: existing.sortOrder.toString(),
    });
    setFields(existing.fields);
    setAttachments(existing.attachments);
    setPopulated(true);
  }

  const saveMutation = useMutation({
    mutationFn: (payload: SaveRequestTypePayload) =>
      isNew ? adminApi.createRequestType(payload) : adminApi.updateRequestType(id!, payload),
    onSuccess: (data: RequestTypeDetail) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'request-types'] });
      toast('success', t('common.success'));
      if (isNew) {
        navigate(`/admin/request-types/${data.id}`, { replace: true });
      }
    },
    onError: (err: unknown) => {
      const status = (err as { response?: { status?: number; data?: { requiresVersionBump?: boolean; activeRequestCount?: number } } })?.response;
      if (status?.status === 409 && status.data?.requiresVersionBump) {
        setPendingPayload(buildPayload(methods.getValues()));
        setVersionBumpModal(true);
      } else {
        toast('error', t('common.error'));
      }
    },
  });

  const buildPayload = useCallback((values: FormValues): SaveRequestTypePayload => ({
    nameI18n: { hr: values.nameHr, ...(values.nameEn ? { en: values.nameEn } : {}) },
    descriptionI18n: { hr: values.descriptionHr, ...(values.descriptionEn ? { en: values.descriptionEn } : {}) },
    code: values.code,
    isActive: values.isActive,
    estimatedProcessingDays: values.estimatedProcessingDays ? parseInt(values.estimatedProcessingDays) : null,
    sortOrder: parseInt(values.sortOrder) || 0,
    fields: fields.map(({ id: _id, ...f }) => f),
    attachments: attachments.map(({ id: _id, ...a }) => a),
  }), [fields, attachments]);

  const handleSave = methods.handleSubmit((values) => {
    saveMutation.mutate(buildPayload(values));
  });

  const handleConfirmVersionBump = () => {
    if (pendingPayload) {
      saveMutation.mutate({ ...pendingPayload, confirmVersionBump: true });
    }
    setVersionBumpModal(false);
    setPendingPayload(null);
  };

  if (!isNew && isLoading) {
    return <LoadingSkeleton lines={8} />;
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-bold text-text-primary">
          {isNew ? t('admin.requestTypes.edit.createTitle') : t('admin.requestTypes.edit.title')}
        </h1>
        <div className="flex gap-3">
          <Button variant="secondary" onClick={() => navigate('/admin/request-types')}>
            {t('common.cancel')}
          </Button>
          <Button onClick={handleSave} disabled={saveMutation.isPending}>
            {saveMutation.isPending ? t('common.loading') : t('common.save')}
          </Button>
        </div>
      </div>

      {/* Tabs - horizontal on desktop, accordion-style on mobile */}
      <div className="flex gap-1 bg-gray-100 rounded-lg p-1 mb-6 overflow-x-auto">
        {tabs.map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 rounded-md text-sm font-medium whitespace-nowrap transition-colors ${
              activeTab === tab
                ? 'bg-surface text-text-primary shadow-sm'
                : 'text-text-secondary hover:text-text-primary'
            }`}
          >
            {t(`admin.requestTypes.edit.tabs.${tab}`)}
          </button>
        ))}
      </div>

      <FormProvider {...methods}>
        <div className="bg-surface rounded-lg border border-border p-6">
          {activeTab === 'basic' && <BasicTab />}
          {activeTab === 'fields' && <FieldsTab fields={fields} onChange={setFields} />}
          {activeTab === 'attachments' && <AttachmentsTab attachments={attachments} onChange={setAttachments} />}
          {activeTab === 'preview' && <PreviewTab fields={fields} />}
        </div>
      </FormProvider>

      {/* Version bump modal */}
      <Modal isOpen={versionBumpModal} onClose={() => setVersionBumpModal(false)} title={t('admin.requestTypes.version.confirm')}>
        <p className="text-sm text-text-secondary mb-4">
          {t('admin.requestTypes.version.bumpWarning', {
            count: 0,
            version: (existing?.version ?? 1) + 1,
          })}
        </p>
        <div className="flex justify-end gap-3">
          <Button variant="secondary" onClick={() => setVersionBumpModal(false)}>{t('common.cancel')}</Button>
          <Button onClick={handleConfirmVersionBump}>{t('admin.requestTypes.version.confirm')}</Button>
        </div>
      </Modal>
    </div>
  );
}
