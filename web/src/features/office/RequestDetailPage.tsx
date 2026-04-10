import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { officeApi } from './api';
import { DynamicFormRenderer, Button, Modal, StatusBadge, LoadingSkeleton, useToast, Select, Textarea } from '@/shared/components';

function getLocalizedText(i18n: Record<string, string>, lang: string): string {
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${(bytes / 1024).toFixed(0)} KB`;
}

export function OfficerRequestDetailPage() {
  const { t, i18n } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const lang = i18n.language;

  const [acceptModal, setAcceptModal] = useState(false);
  const [rejectModal, setRejectModal] = useState(false);
  const [rejectionReason, setRejectionReason] = useState('');
  const [internalNote, setInternalNote] = useState('');
  const [previewAttachment, setPreviewAttachment] = useState<{ url: string; type: string } | null>(null);

  const { data: request, isLoading } = useQuery({
    queryKey: ['office', 'request', id],
    queryFn: () => officeApi.getRequest(id!),
    enabled: !!id,
  });

  const { data: reasons = [] } = useQuery({
    queryKey: ['office', 'rejection-reasons'],
    queryFn: officeApi.getRejectionReasons,
  });

  const acceptMutation = useMutation({
    mutationFn: () => officeApi.acceptRequest(id!),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['office'] });
      if (result.aktId) {
        toast('success', t('office.accept.successToast', { aktId: result.aktId }));
      } else {
        toast('warning', t('office.accept.asyncModal'));
      }
      navigate('/office/inbox');
    },
    onError: () => toast('error', t('common.error')),
  });

  const rejectMutation = useMutation({
    mutationFn: () => officeApi.rejectRequest(id!, {
      rejectionReasonCode: rejectionReason,
      internalNote: internalNote || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['office'] });
      toast('success', t('common.success'));
      navigate('/office/inbox');
    },
    onError: () => toast('error', t('common.error')),
  });

  const retryMutation = useMutation({
    mutationFn: () => officeApi.retryAccept(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['office'] });
      toast('success', t('common.success'));
    },
    onError: () => toast('error', t('common.error')),
  });

  const handlePreview = async (attachmentId: string, mimeType: string) => {
    try {
      const response = await officeApi.previewAttachment(id!, attachmentId);
      const url = URL.createObjectURL(response.data);
      setPreviewAttachment({ url, type: mimeType });
    } catch {
      toast('error', t('common.error'));
    }
  };

  const handleDownload = async (attachmentId: string, filename: string) => {
    try {
      const response = await officeApi.downloadAttachment(id!, attachmentId);
      const url = URL.createObjectURL(response.data);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast('error', t('common.error'));
    }
  };

  if (isLoading || !request) return <LoadingSkeleton lines={10} />;

  const canAct = request.status === 'submitted';
  const isDeadLetter = request.outboxStatus === 'dead_letter';
  const isProcessing = request.outboxStatus === 'processing' || request.outboxStatus === 'pending';

  return (
    <div>
      {/* Dead letter banner */}
      {isDeadLetter && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-4 flex items-center justify-between">
          <p className="text-sm text-warning">{t('office.accept.deadLetterBanner')}</p>
          <Button size="sm" onClick={() => retryMutation.mutate()} disabled={retryMutation.isPending}>
            {t('office.accept.retryButton')}
          </Button>
        </div>
      )}

      {/* Processing indicator */}
      {isProcessing && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
          <p className="text-sm text-primary">{t('office.accept.asyncStatus')}</p>
        </div>
      )}

      <h1 className="text-xl font-bold text-text-primary mb-4">{t('office.detail.title')}</h1>

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
        {/* Left column (60%) */}
        <div className="lg:col-span-3 space-y-6">
          {/* Request type & form data */}
          <section>
            <h2 className="text-sm font-medium text-text-secondary mb-1">
              {getLocalizedText(request.requestTypeName, lang)}
            </h2>
            <div className="text-xs text-text-secondary mb-3">{request.referenceNumber}</div>
            <div className="bg-surface border border-border rounded-lg p-4">
              <DynamicFormRenderer
                schema={request.formSchemaSnapshot}
                mode="readonly"
                values={request.formData}
              />
            </div>
          </section>

          {/* Attachments */}
          {request.attachments.length > 0 && (
            <section>
              <h2 className="text-lg font-semibold text-text-primary mb-3">{t('requests.detail.attachments')}</h2>
              <div className="space-y-2">
                {request.attachments.map((att) => (
                  <div key={att.id} className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-border">
                    <svg className="w-6 h-6 text-text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                    </svg>
                    <div className="flex-1 min-w-0">
                      <div className="text-sm text-text-primary truncate">{att.originalFilename}</div>
                      <div className="text-xs text-text-secondary">{formatFileSize(att.sizeBytes)}</div>
                    </div>
                    <div className="flex gap-2">
                      {(att.mimeType === 'application/pdf' || att.mimeType.startsWith('image/')) && (
                        <button onClick={() => handlePreview(att.id, att.mimeType)} className="text-primary hover:underline text-sm">
                          {t('common.preview')}
                        </button>
                      )}
                      <button onClick={() => handleDownload(att.id, att.originalFilename)} className="text-primary hover:underline text-sm">
                        {t('common.download')}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}
        </div>

        {/* Right column (40%) */}
        <div className="lg:col-span-2 space-y-6">
          {/* Applicant info */}
          <section className="bg-surface border border-border rounded-lg p-4">
            <h3 className="text-sm font-semibold text-text-primary mb-3">{t('office.detail.applicantInfo')}</h3>
            <dl className="space-y-2 text-sm">
              <div><dt className="text-text-secondary">{t('auth.firstName')}</dt><dd className="text-text-primary">{request.applicant.fullName}</dd></div>
              <div><dt className="text-text-secondary">{t('auth.oib')}</dt><dd className="text-text-primary font-mono">{request.applicant.oib}</dd></div>
              <div><dt className="text-text-secondary">{t('auth.email')}</dt><dd className="text-text-primary">{request.applicant.email}</dd></div>
              {request.applicant.phone && <div><dt className="text-text-secondary">{t('auth.phone')}</dt><dd className="text-text-primary">{request.applicant.phone}</dd></div>}
            </dl>
          </section>

          {/* Metadata */}
          <section className="bg-surface border border-border rounded-lg p-4">
            <h3 className="text-sm font-semibold text-text-primary mb-3">{t('office.detail.metadata')}</h3>
            <dl className="space-y-2 text-sm">
              <div><dt className="text-text-secondary">{t('common.status')}</dt><dd><StatusBadge status={request.status as Parameters<typeof StatusBadge>[0]['status']} /></dd></div>
              <div><dt className="text-text-secondary">{t('office.detail.submittedAt')}</dt><dd className="text-text-primary">{new Date(request.submittedAt).toLocaleString()}</dd></div>
              {request.viewedFirstAt && <div><dt className="text-text-secondary">{t('office.detail.viewedAt')}</dt><dd className="text-text-primary">{new Date(request.viewedFirstAt).toLocaleString()}</dd></div>}
              {request.aktId && <div><dt className="text-text-secondary">{t('requests.detail.aktId')}</dt><dd className="text-text-primary font-mono">{request.aktId}</dd></div>}
            </dl>
          </section>

          {/* History */}
          {request.history.length > 0 && (
            <section className="bg-surface border border-border rounded-lg p-4">
              <h3 className="text-sm font-semibold text-text-primary mb-3">{t('office.detail.history')}</h3>
              <div className="space-y-3">
                {request.history.map((entry) => (
                  <div key={entry.id} className="flex gap-2">
                    <div className="w-2 h-2 rounded-full bg-primary mt-1.5 flex-shrink-0" />
                    <div>
                      <div className="text-xs">
                        <StatusBadge status={entry.toStatus as Parameters<typeof StatusBadge>[0]['status']} />
                      </div>
                      {entry.comment && <p className="text-xs text-text-secondary mt-0.5">{entry.comment}</p>}
                      <p className="text-xs text-text-secondary">{new Date(entry.changedAt).toLocaleString()}</p>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}
        </div>
      </div>

      {/* Sticky footer actions */}
      {canAct && (
        <div className="sticky bottom-0 bg-gray-50 py-4 -mx-4 px-4 lg:-mx-6 lg:px-6 border-t border-border mt-6 flex gap-3 justify-end">
          <Button variant="danger" onClick={() => setRejectModal(true)}>
            {t('office.reject.button')}
          </Button>
          <Button onClick={() => setAcceptModal(true)}>
            {t('office.accept.button')}
          </Button>
        </div>
      )}

      {/* Accept modal */}
      <Modal isOpen={acceptModal} onClose={() => setAcceptModal(false)} title={t('office.accept.title')}>
        <p className="text-sm text-text-secondary mb-3">{t('office.accept.description')}</p>
        <dl className="text-sm space-y-1 mb-4">
          <div><dt className="text-text-secondary inline">{t('office.accept.applicant')}: </dt><dd className="inline text-text-primary">{request.applicant.fullName} ({request.applicant.oib})</dd></div>
          <div><dt className="text-text-secondary inline">{t('office.accept.type')}: </dt><dd className="inline text-text-primary">{getLocalizedText(request.requestTypeName, lang)}</dd></div>
        </dl>
        <div className="flex justify-end gap-3">
          <Button variant="secondary" onClick={() => setAcceptModal(false)}>{t('common.cancel')}</Button>
          <Button onClick={() => { setAcceptModal(false); acceptMutation.mutate(); }} disabled={acceptMutation.isPending}>
            {acceptMutation.isPending ? t('office.accept.processing') : t('office.accept.confirm')}
          </Button>
        </div>
      </Modal>

      {/* Reject modal */}
      <Modal isOpen={rejectModal} onClose={() => setRejectModal(false)} title={t('office.reject.title')}>
        <div className="space-y-4">
          <Select
            label={t('office.reject.reasonLabel')}
            required
            placeholder={t('office.reject.reasonPlaceholder')}
            options={reasons.map((r) => ({ value: r.code, label: getLocalizedText(r.labelI18n, lang) }))}
            value={rejectionReason}
            onChange={(e) => setRejectionReason(e.target.value)}
          />
          <Textarea
            label={t('office.reject.internalNote')}
            required={rejectionReason === 'other'}
            value={internalNote}
            onChange={(e) => setInternalNote(e.target.value)}
            error={rejectionReason === 'other' && !internalNote ? t('office.reject.internalNoteRequired') : undefined}
          />
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            <p className="text-xs text-warning">{t('office.reject.warning')}</p>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setRejectModal(false)}>{t('common.cancel')}</Button>
            <Button
              variant="danger"
              onClick={() => { setRejectModal(false); rejectMutation.mutate(); }}
              disabled={!rejectionReason || (rejectionReason === 'other' && !internalNote) || rejectMutation.isPending}
            >
              {t('office.reject.confirm')}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Attachment preview modal */}
      <Modal isOpen={!!previewAttachment} onClose={() => { if (previewAttachment) { URL.revokeObjectURL(previewAttachment.url); } setPreviewAttachment(null); }} size="lg" title={t('common.preview')}>
        {previewAttachment && (
          <div className="max-h-[70vh] overflow-auto">
            {previewAttachment.type.startsWith('image/') ? (
              <img src={previewAttachment.url} alt="" className="w-full rounded" />
            ) : (
              <iframe src={previewAttachment.url} className="w-full h-[60vh] rounded border-0" title="PDF preview" />
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}
