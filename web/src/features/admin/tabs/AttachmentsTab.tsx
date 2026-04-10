import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { RequestTypeAttachment } from '../api';
import { Button, Modal, Input, Textarea, Select, Checkbox, EmptyState } from '@/shared/components';
import { generateId } from '@/shared/utils/uuid';

function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[čć]/g, 'c').replace(/[đ]/g, 'd').replace(/[š]/g, 's').replace(/[ž]/g, 'z')
    .replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '');
}

const sizeOptions = [
  { value: (2 * 1024 * 1024).toString(), labelKey: 'admin.requestTypes.attachments.sizeOptions.2mb' },
  { value: (5 * 1024 * 1024).toString(), labelKey: 'admin.requestTypes.attachments.sizeOptions.5mb' },
  { value: (10 * 1024 * 1024).toString(), labelKey: 'admin.requestTypes.attachments.sizeOptions.10mb' },
  { value: (25 * 1024 * 1024).toString(), labelKey: 'admin.requestTypes.attachments.sizeOptions.25mb' },
];

const mimeGroups = [
  { value: 'application/pdf', labelKey: 'admin.requestTypes.attachments.mimeTypes.pdf' },
  { value: 'image/jpeg,image/png', labelKey: 'admin.requestTypes.attachments.mimeTypes.images' },
  { value: 'application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document', labelKey: 'admin.requestTypes.attachments.mimeTypes.documents' },
];

interface AttachmentsTabProps {
  attachments: RequestTypeAttachment[];
  onChange: (attachments: RequestTypeAttachment[]) => void;
}

interface AttForm {
  attachmentKey: string;
  labelHr: string;
  labelEn: string;
  descriptionHr: string;
  descriptionEn: string;
  isRequired: boolean;
  maxSizeBytes: string;
  allowedMimeTypes: string[];
}

const emptyForm: AttForm = {
  attachmentKey: '', labelHr: '', labelEn: '', descriptionHr: '', descriptionEn: '',
  isRequired: false, maxSizeBytes: (10 * 1024 * 1024).toString(), allowedMimeTypes: ['application/pdf'],
};

export function AttachmentsTab({ attachments, onChange }: AttachmentsTabProps) {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const [modalOpen, setModalOpen] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [form, setForm] = useState<AttForm>(emptyForm);

  const openNew = () => {
    setForm(emptyForm);
    setEditingIndex(null);
    setModalOpen(true);
  };

  const openEdit = (index: number) => {
    const a = attachments[index]!;
    setForm({
      attachmentKey: a.attachmentKey,
      labelHr: a.labelI18n.hr ?? '',
      labelEn: a.labelI18n.en ?? '',
      descriptionHr: a.descriptionI18n?.hr ?? '',
      descriptionEn: a.descriptionI18n?.en ?? '',
      isRequired: a.isRequired,
      maxSizeBytes: a.maxSizeBytes.toString(),
      allowedMimeTypes: a.allowedMimeTypes,
    });
    setEditingIndex(index);
    setModalOpen(true);
  };

  const handleSave = () => {
    const att: RequestTypeAttachment = {
      id: editingIndex !== null ? attachments[editingIndex]!.id : generateId(),
      attachmentKey: form.attachmentKey || slugify(form.labelHr),
      labelI18n: { hr: form.labelHr, ...(form.labelEn ? { en: form.labelEn } : {}) },
      descriptionI18n: form.descriptionHr ? { hr: form.descriptionHr, ...(form.descriptionEn ? { en: form.descriptionEn } : {}) } : null,
      isRequired: form.isRequired,
      maxSizeBytes: parseInt(form.maxSizeBytes),
      allowedMimeTypes: form.allowedMimeTypes,
      sortOrder: editingIndex !== null ? attachments[editingIndex]!.sortOrder : attachments.length,
    };

    if (editingIndex !== null) {
      const updated = [...attachments];
      updated[editingIndex] = att;
      onChange(updated);
    } else {
      onChange([...attachments, att]);
    }
    setModalOpen(false);
  };

  const remove = (index: number) => onChange(attachments.filter((_, i) => i !== index));

  const move = (index: number, dir: -1 | 1) => {
    const target = index + dir;
    if (target < 0 || target >= attachments.length) return;
    const updated = [...attachments];
    [updated[index], updated[target]] = [updated[target]!, updated[index]!];
    updated.forEach((a, i) => a.sortOrder = i);
    onChange(updated);
  };

  const getLabel = (a: RequestTypeAttachment) => a.labelI18n[lang] ?? a.labelI18n.hr ?? '';

  const toggleMime = (mime: string) => {
    const mimes = mime.split(',');
    const current = new Set(form.allowedMimeTypes);
    const allPresent = mimes.every((m) => current.has(m));
    if (allPresent) {
      mimes.forEach((m) => current.delete(m));
    } else {
      mimes.forEach((m) => current.add(m));
    }
    setForm({ ...form, allowedMimeTypes: Array.from(current) });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-medium text-text-primary">{t('admin.requestTypes.attachments.title')}</h3>
        <Button size="sm" onClick={openNew}>{t('admin.requestTypes.attachments.addAttachment')}</Button>
      </div>

      {attachments.length === 0 ? (
        <EmptyState title={t('admin.requestTypes.attachments.empty')} />
      ) : (
        <div className="space-y-2">
          {[...attachments].sort((a, b) => a.sortOrder - b.sortOrder).map((att, index) => (
            <div key={att.id} className="flex items-center gap-3 p-3 border border-border rounded-lg bg-gray-50">
              <div className="flex flex-col gap-1">
                <button onClick={() => move(index, -1)} className="text-text-secondary hover:text-text-primary disabled:opacity-30" disabled={index === 0}>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" /></svg>
                </button>
                <button onClick={() => move(index, 1)} className="text-text-secondary hover:text-text-primary disabled:opacity-30" disabled={index === attachments.length - 1}>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>
                </button>
              </div>
              <div className="flex-1 min-w-0">
                <div className="font-medium text-sm text-text-primary truncate">{getLabel(att)}</div>
                <div className="text-xs text-text-secondary">
                  {att.isRequired && <span className="text-error mr-1">*</span>}
                  {(att.maxSizeBytes / (1024 * 1024)).toFixed(0)} MB
                  <span className="ml-2 text-gray-400">{att.attachmentKey}</span>
                </div>
              </div>
              <div className="flex gap-2">
                <Button variant="secondary" size="sm" onClick={() => openEdit(index)}>{t('common.edit')}</Button>
                <Button variant="danger" size="sm" onClick={() => remove(index)}>{t('common.delete')}</Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)} size="lg"
        title={editingIndex !== null ? t('admin.requestTypes.attachments.editAttachment') : t('admin.requestTypes.attachments.addAttachment')}>
        <div className="space-y-4">
          <Input label={t('admin.requestTypes.attachments.labelHr')} required value={form.labelHr} onChange={(e) => setForm({ ...form, labelHr: e.target.value })} />
          <Input label={t('admin.requestTypes.attachments.labelEn')} value={form.labelEn} onChange={(e) => setForm({ ...form, labelEn: e.target.value })} />
          <Textarea label={t('admin.requestTypes.attachments.descriptionHr')} value={form.descriptionHr} onChange={(e) => setForm({ ...form, descriptionHr: e.target.value })} />
          <Textarea label={t('admin.requestTypes.attachments.descriptionEn')} value={form.descriptionEn} onChange={(e) => setForm({ ...form, descriptionEn: e.target.value })} />
          <Input label={t('admin.requestTypes.attachments.attachmentKey')} value={form.attachmentKey || slugify(form.labelHr)} onChange={(e) => setForm({ ...form, attachmentKey: e.target.value })} />
          <Checkbox label={t('admin.requestTypes.attachments.isRequired')} checked={form.isRequired} onChange={(e) => setForm({ ...form, isRequired: (e.target as HTMLInputElement).checked })}>
            {t('admin.requestTypes.attachments.isRequired')}
          </Checkbox>
          <Select
            label={t('admin.requestTypes.attachments.maxSize')}
            options={sizeOptions.map((s) => ({ value: s.value, label: t(s.labelKey) }))}
            value={form.maxSizeBytes}
            onChange={(e) => setForm({ ...form, maxSizeBytes: e.target.value })}
          />
          <div>
            <label className="block text-sm font-medium text-text-primary mb-2">{t('admin.requestTypes.attachments.allowedMimeTypes')}</label>
            <div className="space-y-2">
              {mimeGroups.map((mg) => (
                <Checkbox
                  key={mg.value}
                  label={t(mg.labelKey)}
                  checked={mg.value.split(',').every((m) => form.allowedMimeTypes.includes(m))}
                  onChange={() => toggleMime(mg.value)}
                >
                  {t(mg.labelKey)}
                </Checkbox>
              ))}
            </div>
          </div>
          <div className="flex justify-end gap-3 pt-4 border-t border-border">
            <Button variant="secondary" onClick={() => setModalOpen(false)}>{t('common.cancel')}</Button>
            <Button onClick={handleSave} disabled={!form.labelHr}>{t('common.save')}</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
