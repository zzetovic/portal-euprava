import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { RequestTypeField } from '../api';
import { Button, Modal, Input, Select, Checkbox, EmptyState } from '@/shared/components';

const fieldTypes = ['text', 'textarea', 'number', 'date', 'select', 'checkbox', 'oib', 'iban', 'email', 'phone'] as const;

function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[čć]/g, 'c').replace(/[đ]/g, 'd').replace(/[š]/g, 's').replace(/[ž]/g, 'z')
    .replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '');
}

interface FieldsTabProps {
  fields: RequestTypeField[];
  onChange: (fields: RequestTypeField[]) => void;
}

interface FieldForm {
  fieldKey: string;
  fieldType: string;
  labelHr: string;
  labelEn: string;
  helpTextHr: string;
  helpTextEn: string;
  isRequired: boolean;
  min: string;
  max: string;
  regex: string;
  options: Array<{ value: string; labelHr: string; labelEn: string }>;
}

const emptyFieldForm: FieldForm = {
  fieldKey: '', fieldType: 'text', labelHr: '', labelEn: '',
  helpTextHr: '', helpTextEn: '', isRequired: false, min: '', max: '', regex: '',
  options: [],
};

export function FieldsTab({ fields, onChange }: FieldsTabProps) {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const [modalOpen, setModalOpen] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [form, setForm] = useState<FieldForm>(emptyFieldForm);
  const [showAdvanced, setShowAdvanced] = useState(false);

  const openNew = () => {
    setForm(emptyFieldForm);
    setEditingIndex(null);
    setShowAdvanced(false);
    setModalOpen(true);
  };

  const openEdit = (index: number) => {
    const f = fields[index]!;
    setForm({
      fieldKey: f.fieldKey,
      fieldType: f.fieldType,
      labelHr: f.labelI18n.hr ?? '',
      labelEn: f.labelI18n.en ?? '',
      helpTextHr: f.helpTextI18n?.hr ?? '',
      helpTextEn: f.helpTextI18n?.en ?? '',
      isRequired: f.isRequired,
      min: f.validationRules?.min?.toString() ?? '',
      max: f.validationRules?.max?.toString() ?? '',
      regex: (f.validationRules?.regex as string) ?? '',
      options: f.options?.map((o) => ({ value: o.value, labelHr: o.labelI18n.hr ?? '', labelEn: o.labelI18n.en ?? '' })) ?? [],
    });
    setEditingIndex(index);
    setShowAdvanced(false);
    setModalOpen(true);
  };

  const handleSaveField = () => {
    const field: RequestTypeField = {
      id: editingIndex !== null ? fields[editingIndex]!.id : crypto.randomUUID(),
      fieldKey: form.fieldKey || slugify(form.labelHr),
      fieldType: form.fieldType,
      labelI18n: { hr: form.labelHr, ...(form.labelEn ? { en: form.labelEn } : {}) },
      helpTextI18n: form.helpTextHr ? { hr: form.helpTextHr, ...(form.helpTextEn ? { en: form.helpTextEn } : {}) } : null,
      isRequired: form.isRequired,
      validationRules: (form.min || form.max || form.regex) ? {
        ...(form.min ? { min: Number(form.min) } : {}),
        ...(form.max ? { max: Number(form.max) } : {}),
        ...(form.regex ? { regex: form.regex } : {}),
      } : null,
      options: form.fieldType === 'select' ? form.options.map((o) => ({
        value: o.value,
        labelI18n: { hr: o.labelHr, ...(o.labelEn ? { en: o.labelEn } : {}) },
      })) : null,
      sortOrder: editingIndex !== null ? fields[editingIndex]!.sortOrder : fields.length,
    };

    if (editingIndex !== null) {
      const updated = [...fields];
      updated[editingIndex] = field;
      onChange(updated);
    } else {
      onChange([...fields, field]);
    }
    setModalOpen(false);
  };

  const removeField = (index: number) => {
    onChange(fields.filter((_, i) => i !== index));
  };

  const moveField = (index: number, dir: -1 | 1) => {
    const target = index + dir;
    if (target < 0 || target >= fields.length) return;
    const updated = [...fields];
    [updated[index], updated[target]] = [updated[target]!, updated[index]!];
    updated.forEach((f, i) => f.sortOrder = i);
    onChange(updated);
  };

  const typeOptions = fieldTypes.map((ft) => ({
    value: ft,
    label: t(`admin.requestTypes.fields.fieldTypes.${ft}`),
  }));

  const getLabel = (f: RequestTypeField) => f.labelI18n[lang] ?? f.labelI18n.hr ?? '';

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-medium text-text-primary">{t('admin.requestTypes.fields.title')}</h3>
        <Button size="sm" onClick={openNew}>{t('admin.requestTypes.fields.addField')}</Button>
      </div>

      {fields.length === 0 ? (
        <EmptyState title={t('admin.requestTypes.fields.empty')} />
      ) : (
        <div className="space-y-2">
          {[...fields].sort((a, b) => a.sortOrder - b.sortOrder).map((field, index) => (
            <div key={field.id} className="flex items-center gap-3 p-3 border border-border rounded-lg bg-gray-50">
              <div className="flex flex-col gap-1">
                <button onClick={() => moveField(index, -1)} className="text-text-secondary hover:text-text-primary disabled:opacity-30" disabled={index === 0}>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" /></svg>
                </button>
                <button onClick={() => moveField(index, 1)} className="text-text-secondary hover:text-text-primary disabled:opacity-30" disabled={index === fields.length - 1}>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>
                </button>
              </div>
              <div className="flex-1 min-w-0">
                <div className="font-medium text-sm text-text-primary truncate">{getLabel(field)}</div>
                <div className="text-xs text-text-secondary">
                  {t(`admin.requestTypes.fields.fieldTypes.${field.fieldType}`)}
                  {field.isRequired && <span className="text-error ml-1">*</span>}
                  <span className="ml-2 text-gray-400">{field.fieldKey}</span>
                </div>
              </div>
              <div className="flex gap-2">
                <Button variant="secondary" size="sm" onClick={() => openEdit(index)}>{t('common.edit')}</Button>
                <Button variant="danger" size="sm" onClick={() => removeField(index)}>{t('common.delete')}</Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Field edit modal */}
      <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)} size="lg"
        title={editingIndex !== null ? t('admin.requestTypes.fields.editField') : t('admin.requestTypes.fields.addField')}>
        <div className="space-y-4">
          <Select
            label={t('admin.requestTypes.fields.fieldType')}
            required
            options={typeOptions}
            value={form.fieldType}
            onChange={(e) => setForm({ ...form, fieldType: e.target.value })}
          />
          <Input
            label={t('admin.requestTypes.fields.labelHr')}
            required
            value={form.labelHr}
            onChange={(e) => setForm({ ...form, labelHr: e.target.value })}
          />
          <Input
            label={t('admin.requestTypes.fields.labelEn')}
            value={form.labelEn}
            onChange={(e) => setForm({ ...form, labelEn: e.target.value })}
          />
          <Input
            label={t('admin.requestTypes.fields.helpTextHr')}
            value={form.helpTextHr}
            onChange={(e) => setForm({ ...form, helpTextHr: e.target.value })}
          />
          <Input
            label={t('admin.requestTypes.fields.helpTextEn')}
            value={form.helpTextEn}
            onChange={(e) => setForm({ ...form, helpTextEn: e.target.value })}
          />
          <Input
            label={t('admin.requestTypes.fields.fieldKey')}
            helpText={t('admin.requestTypes.edit.codeHelp')}
            value={form.fieldKey || slugify(form.labelHr)}
            onChange={(e) => setForm({ ...form, fieldKey: e.target.value })}
          />
          <Checkbox
            label={t('admin.requestTypes.fields.isRequired')}
            checked={form.isRequired}
            onChange={(e) => setForm({ ...form, isRequired: (e.target as HTMLInputElement).checked })}
          >
            {t('admin.requestTypes.fields.isRequired')}
          </Checkbox>

          {/* Advanced settings */}
          <button
            type="button"
            onClick={() => setShowAdvanced(!showAdvanced)}
            className="text-sm text-primary hover:underline"
          >
            {t('admin.requestTypes.fields.advancedSettings')} {showAdvanced ? '▲' : '▼'}
          </button>
          {showAdvanced && (
            <div className="space-y-4 pl-4 border-l-2 border-border">
              <Input
                label={t('admin.requestTypes.fields.minValue')}
                type="number"
                value={form.min}
                onChange={(e) => setForm({ ...form, min: e.target.value })}
              />
              <Input
                label={t('admin.requestTypes.fields.maxValue')}
                type="number"
                value={form.max}
                onChange={(e) => setForm({ ...form, max: e.target.value })}
              />
              <Input
                label={t('admin.requestTypes.fields.regex')}
                value={form.regex}
                onChange={(e) => setForm({ ...form, regex: e.target.value })}
              />
            </div>
          )}

          {/* Select options */}
          {form.fieldType === 'select' && (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h4 className="text-sm font-medium text-text-primary">{t('admin.requestTypes.fields.options')}</h4>
                <Button size="sm" variant="secondary" onClick={() => setForm({
                  ...form,
                  options: [...form.options, { value: '', labelHr: '', labelEn: '' }],
                })}>
                  {t('admin.requestTypes.fields.addOption')}
                </Button>
              </div>
              {form.options.map((opt, i) => (
                <div key={i} className="flex gap-2 items-end">
                  <Input
                    label={t('admin.requestTypes.fields.optionValue')}
                    value={opt.value}
                    onChange={(e) => {
                      const opts = [...form.options];
                      opts[i] = { ...opt, value: e.target.value };
                      setForm({ ...form, options: opts });
                    }}
                  />
                  <Input
                    label={t('admin.requestTypes.fields.optionLabelHr')}
                    value={opt.labelHr}
                    onChange={(e) => {
                      const opts = [...form.options];
                      opts[i] = { ...opt, labelHr: e.target.value };
                      setForm({ ...form, options: opts });
                    }}
                  />
                  <Input
                    label={t('admin.requestTypes.fields.optionLabelEn')}
                    value={opt.labelEn}
                    onChange={(e) => {
                      const opts = [...form.options];
                      opts[i] = { ...opt, labelEn: e.target.value };
                      setForm({ ...form, options: opts });
                    }}
                  />
                  <Button variant="danger" size="sm" onClick={() => setForm({
                    ...form,
                    options: form.options.filter((_, j) => j !== i),
                  })}>
                    {t('common.delete')}
                  </Button>
                </div>
              ))}
            </div>
          )}

          <div className="flex justify-end gap-3 pt-4 border-t border-border">
            <Button variant="secondary" onClick={() => setModalOpen(false)}>{t('common.cancel')}</Button>
            <Button onClick={handleSaveField} disabled={!form.labelHr}>{t('common.save')}</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
