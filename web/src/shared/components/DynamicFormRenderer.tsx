import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useFormContext, Controller } from 'react-hook-form';
import { Input, Textarea, Select, Checkbox } from './Input';

export interface FieldDefinition {
  id: string;
  fieldKey: string;
  fieldType: 'text' | 'textarea' | 'number' | 'date' | 'select' | 'checkbox' | 'oib' | 'iban' | 'email' | 'phone';
  labelI18n: Record<string, string>;
  helpTextI18n?: Record<string, string> | null;
  isRequired: boolean;
  validationRules?: {
    min?: number;
    max?: number;
    regex?: string;
    minLength?: number;
    maxLength?: number;
  } | null;
  options?: Array<{ value: string; labelI18n: Record<string, string> }> | null;
  sortOrder: number;
}

export interface FormSchema {
  fields: FieldDefinition[];
}

interface DynamicFormRendererProps {
  schema: FormSchema;
  mode: 'edit' | 'readonly' | 'preview';
  values?: Record<string, unknown>;
}

function getLocalizedText(i18n: Record<string, string> | null | undefined, lang: string): string {
  if (!i18n) return '';
  return i18n[lang] ?? i18n['hr'] ?? Object.values(i18n)[0] ?? '';
}

export function DynamicFormRenderer({ schema, mode, values }: DynamicFormRendererProps) {
  const { i18n } = useTranslation();
  const lang = i18n.language;
  const sorted = [...schema.fields].sort((a, b) => a.sortOrder - b.sortOrder);

  if (mode === 'readonly') {
    return <ReadonlyFields fields={sorted} values={values ?? {}} lang={lang} />;
  }

  return (
    <div className="space-y-5">
      {sorted.map((field) => (
        <DynamicField key={field.id} field={field} mode={mode} lang={lang} />
      ))}
    </div>
  );
}

function ReadonlyFields({
  fields,
  values,
  lang,
}: {
  fields: FieldDefinition[];
  values: Record<string, unknown>;
  lang: string;
}) {
  return (
    <div className="space-y-4">
      {fields.map((field) => {
        const label = getLocalizedText(field.labelI18n, lang);
        const value = values[field.fieldKey];
        let displayValue: string;

        if (field.fieldType === 'checkbox') {
          displayValue = value ? '\u2713' : '\u2717';
        } else if (field.fieldType === 'select' && field.options) {
          const opt = field.options.find((o) => o.value === value);
          displayValue = opt ? getLocalizedText(opt.labelI18n, lang) : String(value ?? '');
        } else {
          displayValue = value != null ? String(value) : '-';
        }

        return (
          <div key={field.id}>
            <dt className="text-sm font-medium text-text-secondary">{label}</dt>
            <dd className="mt-1 text-text-primary">{displayValue}</dd>
          </div>
        );
      })}
    </div>
  );
}

function DynamicField({
  field,
  mode,
  lang,
}: {
  field: FieldDefinition;
  mode: 'edit' | 'preview';
  lang: string;
}) {
  const { t } = useTranslation();
  const { control, formState: { errors } } = useFormContext();
  const label = getLocalizedText(field.labelI18n, lang);
  const helpText = getLocalizedText(field.helpTextI18n, lang);
  const isDisabled = mode === 'preview';
  const fieldError = errors[field.fieldKey]?.message as string | undefined;

  const getInputType = useCallback((): string => {
    switch (field.fieldType) {
      case 'email': return 'email';
      case 'phone': return 'tel';
      case 'number': return 'number';
      case 'date': return 'date';
      case 'oib': return 'text';
      case 'iban': return 'text';
      default: return 'text';
    }
  }, [field.fieldType]);

  if (field.fieldType === 'checkbox') {
    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: f }) => (
          <Checkbox
            label={label}
            helpText={helpText || undefined}
            error={fieldError}
            checked={!!f.value}
            onChange={f.onChange}
            onBlur={f.onBlur}
            disabled={isDisabled}
          />
        )}
      />
    );
  }

  if (field.fieldType === 'textarea') {
    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: f }) => (
          <Textarea
            label={label}
            helpText={helpText || undefined}
            error={fieldError}
            required={field.isRequired}
            disabled={isDisabled}
            value={f.value ?? ''}
            onChange={f.onChange}
            onBlur={f.onBlur}
            placeholder={mode === 'preview' ? label : undefined}
          />
        )}
      />
    );
  }

  if (field.fieldType === 'select' && field.options) {
    const options = field.options.map((opt) => ({
      value: opt.value,
      label: getLocalizedText(opt.labelI18n, lang),
    }));

    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: f }) => (
          <Select
            label={label}
            helpText={helpText || undefined}
            error={fieldError}
            required={field.isRequired}
            disabled={isDisabled}
            options={options}
            placeholder={t('common.search') + '...'}
            value={f.value ?? ''}
            onChange={f.onChange}
            onBlur={f.onBlur}
          />
        )}
      />
    );
  }

  return (
    <Controller
      name={field.fieldKey}
      control={control}
      render={({ field: f }) => (
        <Input
          label={label}
          helpText={helpText || undefined}
          error={fieldError}
          required={field.isRequired}
          disabled={isDisabled}
          type={getInputType()}
          value={f.value ?? ''}
          onChange={f.onChange}
          onBlur={f.onBlur}
          placeholder={mode === 'preview' ? label : undefined}
          {...(field.fieldType === 'oib' ? { maxLength: 11, inputMode: 'numeric' as const } : {})}
          {...(field.fieldType === 'iban' ? { maxLength: 34 } : {})}
        />
      )}
    />
  );
}
