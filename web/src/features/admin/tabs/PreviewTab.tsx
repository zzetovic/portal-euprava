import { useTranslation } from 'react-i18next';
import { FormProvider, useForm } from 'react-hook-form';
import { DynamicFormRenderer, type FieldDefinition } from '@/shared/components';
import type { RequestTypeField } from '../api';

interface PreviewTabProps {
  fields: RequestTypeField[];
}

export function PreviewTab({ fields }: PreviewTabProps) {
  const { t } = useTranslation();
  const methods = useForm();

  const schema = {
    fields: fields.map((f): FieldDefinition => ({
      id: f.id,
      fieldKey: f.fieldKey,
      fieldType: f.fieldType as FieldDefinition['fieldType'],
      labelI18n: f.labelI18n,
      helpTextI18n: f.helpTextI18n,
      isRequired: f.isRequired,
      validationRules: f.validationRules as FieldDefinition['validationRules'],
      options: f.options,
      sortOrder: f.sortOrder,
    })),
  };

  if (fields.length === 0) {
    return (
      <p className="text-sm text-text-secondary">{t('admin.requestTypes.fields.empty')}</p>
    );
  }

  return (
    <FormProvider {...methods}>
      <DynamicFormRenderer schema={schema} mode="preview" />
    </FormProvider>
  );
}
