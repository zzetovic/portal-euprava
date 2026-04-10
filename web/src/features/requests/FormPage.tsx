import { useState, useEffect, useCallback, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { FormProvider, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { requestsApi } from './api';
import { DynamicFormRenderer } from '@/shared/components';
import { buildZodSchema } from '@/shared/validation';
import { Button, useToast } from '@/shared/components';
import { AttachmentUploader } from './AttachmentUploader';

export function FormPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved'>('idle');
  const [savedTime, setSavedTime] = useState<string>('');
  const debounceRef = useRef<ReturnType<typeof setTimeout>>();

  const { data: request, isLoading } = useQuery({
    queryKey: ['request', id],
    queryFn: () => requestsApi.getRequest(id!),
    enabled: !!id,
  });

  const schema = request?.formSchemaSnapshot;
  const zodSchema = schema ? buildZodSchema(schema.fields, t) : undefined;

  const methods = useForm({
    resolver: zodSchema ? zodResolver(zodSchema) : undefined,
    defaultValues: request?.formData ?? {},
    mode: 'onBlur',
  });

  // Reset form when request data loads
  const [initialized, setInitialized] = useState(false);
  useEffect(() => {
    if (request && !initialized) {
      methods.reset(request.formData);
      setInitialized(true);
    }
  }, [request, methods, initialized]);

  const patchMutation = useMutation({
    mutationFn: (formData: Record<string, unknown>) =>
      requestsApi.patchRequest(id!, formData, request!.etag),
    onSuccess: (data) => {
      queryClient.setQueryData(['request', id], data);
      setSaveStatus('saved');
      setSavedTime(new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));
    },
    onError: (err: unknown) => {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 409) {
        toast('warning', t('requests.editedInAnotherTab'));
      } else {
        toast('error', t('common.error'));
      }
      setSaveStatus('idle');
    },
  });

  // Debounced auto-save
  const autoSave = useCallback(() => {
    if (!request || request.status !== 'draft') return;
    if (debounceRef.current) clearTimeout(debounceRef.current);
    setSaveStatus('saving');
    debounceRef.current = setTimeout(() => {
      patchMutation.mutate(methods.getValues());
    }, 3000);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [request]);

  useEffect(() => {
    const sub = methods.watch(() => autoSave());
    return () => {
      sub.unsubscribe();
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [methods, autoSave]);

  const handleSaveAndExit = () => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    patchMutation.mutate(methods.getValues());
    navigate('/requests');
  };

  const handleNext = methods.handleSubmit(
    () => navigate(`/requests/${id}/review`),
    () => toast('error', t('common.error')),
  );

  if (isLoading || !request || !schema) {
    return <div className="animate-pulse space-y-4"><div className="h-8 bg-gray-200 rounded w-1/3" /><div className="h-4 bg-gray-200 rounded w-full" /><div className="h-4 bg-gray-200 rounded w-2/3" /></div>;
  }

  return (
    <div>
      {/* Sticky header */}
      <div className="sticky top-14 z-20 bg-gray-50 py-3 -mx-4 px-4 border-b border-border mb-6">
        <div className="flex items-center justify-between">
          <h1 className="font-semibold text-text-primary truncate">
            {request.requestTypeName[t('common.code') === 'Kod' ? 'hr' : 'en'] ?? Object.values(request.requestTypeName)[0]}
          </h1>
          <span className="text-xs text-text-secondary ml-2 flex-shrink-0">
            {saveStatus === 'saving' && t('common.saving')}
            {saveStatus === 'saved' && t('common.savedAt', { time: savedTime })}
          </span>
        </div>
      </div>

      <FormProvider {...methods}>
        <form onSubmit={handleNext}>
          <DynamicFormRenderer schema={schema} mode="edit" />

          {/* Attachments section */}
          <div className="mt-8 pt-6 border-t border-border">
            <h2 className="text-lg font-semibold text-text-primary mb-4">{t('requests.form.attachments')}</h2>
            <AttachmentUploader
              requestId={id!}
              attachments={request.attachments}
              schemaAttachments={schema.fields.length > 0 ? [] : []}
              onUploaded={() => queryClient.invalidateQueries({ queryKey: ['request', id] })}
            />
          </div>

          {/* Sticky footer */}
          <div className="sticky bottom-0 bg-gray-50 py-4 -mx-4 px-4 border-t border-border mt-8 flex gap-3 justify-end">
            <Button type="button" variant="secondary" onClick={handleSaveAndExit}>
              {t('requests.form.saveAndContinue')}
            </Button>
            <Button type="submit">
              {t('common.next')}
            </Button>
          </div>
        </form>
      </FormProvider>
    </div>
  );
}
