import { useTranslation } from 'react-i18next';
import { useFormContext } from 'react-hook-form';
import { Input, Textarea, Checkbox } from '@/shared/components';

function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[čć]/g, 'c')
    .replace(/[đ]/g, 'd')
    .replace(/[š]/g, 's')
    .replace(/[ž]/g, 'z')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
}

export function BasicTab() {
  const { t } = useTranslation();
  const { register, setValue, watch } = useFormContext();
  const code = watch('code');

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    register('nameHr').onChange(e);
    if (!code || code === slugify(watch('nameHr'))) {
      setValue('code', slugify(val));
    }
  };

  return (
    <div className="space-y-5 max-w-2xl">
      <Input
        label={t('admin.requestTypes.edit.nameHr')}
        required
        {...register('nameHr')}
        onChange={handleNameChange}
      />
      <Input
        label={t('admin.requestTypes.edit.nameEn')}
        {...register('nameEn')}
      />
      <Input
        label={t('admin.requestTypes.edit.code')}
        helpText={t('admin.requestTypes.edit.codeHelp')}
        required
        {...register('code')}
      />
      <Textarea
        label={t('admin.requestTypes.edit.descriptionHr')}
        {...register('descriptionHr')}
      />
      <Textarea
        label={t('admin.requestTypes.edit.descriptionEn')}
        {...register('descriptionEn')}
      />
      <Checkbox
        label={t('admin.requestTypes.edit.isActive')}
        {...register('isActive')}
      >
        {t('admin.requestTypes.edit.isActive')}
      </Checkbox>
      <Input
        label={t('admin.requestTypes.edit.estimatedDays')}
        type="number"
        helpText={t('admin.requestTypes.edit.estimatedDaysHelp', { days: 5 })}
        {...register('estimatedProcessingDays')}
      />
      <Input
        label={t('admin.requestTypes.edit.sortOrder')}
        type="number"
        {...register('sortOrder')}
      />
    </div>
  );
}
