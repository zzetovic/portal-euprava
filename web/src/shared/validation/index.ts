import { z } from 'zod';
import type { TFunction } from 'i18next';
import type { FieldDefinition } from '@/shared/components/DynamicFormRenderer';

export function validateOib(oib: string): boolean {
  if (!/^\d{11}$/.test(oib)) return false;
  let a = 10;
  for (let i = 0; i < 10; i++) {
    a = (a + parseInt(oib[i]!, 10)) % 10;
    if (a === 0) a = 10;
    a = (a * 2) % 11;
  }
  const check = (11 - a) % 10;
  return check === parseInt(oib[10]!, 10);
}

export function validateIban(iban: string): boolean {
  const cleaned = iban.replace(/\s/g, '').toUpperCase();
  if (cleaned.length < 15 || cleaned.length > 34) return false;
  if (!/^[A-Z]{2}\d{2}[A-Z0-9]+$/.test(cleaned)) return false;

  const rearranged = cleaned.slice(4) + cleaned.slice(0, 4);
  const numStr = rearranged
    .split('')
    .map((c) => {
      const code = c.charCodeAt(0);
      return code >= 65 && code <= 90 ? (code - 55).toString() : c;
    })
    .join('');

  let remainder = 0;
  for (let i = 0; i < numStr.length; i++) {
    remainder = (remainder * 10 + parseInt(numStr[i]!, 10)) % 97;
  }
  return remainder === 1;
}

export function buildZodSchema(fields: FieldDefinition[], t: TFunction): z.ZodObject<Record<string, z.ZodTypeAny>> {
  const shape: Record<string, z.ZodTypeAny> = {};

  for (const field of fields) {
    let fieldSchema: z.ZodTypeAny;

    switch (field.fieldType) {
      case 'checkbox':
        fieldSchema = field.isRequired ? z.literal(true, { errorMap: () => ({ message: t('common.required') }) }) : z.boolean();
        break;

      case 'number': {
        let num = z.coerce.number({ invalid_type_error: t('validation.pattern') });
        if (field.validationRules?.min != null) num = num.min(field.validationRules.min, t('validation.min', { min: field.validationRules.min }));
        if (field.validationRules?.max != null) num = num.max(field.validationRules.max, t('validation.max', { max: field.validationRules.max }));
        fieldSchema = field.isRequired ? num : num.optional();
        break;
      }

      case 'oib': {
        let oib = z.string()
          .refine((v) => !v || validateOib(v), { message: t('validation.oib') });
        fieldSchema = field.isRequired
          ? z.string().min(1, t('common.required')).pipe(z.string().refine(validateOib, { message: t('validation.oib') }))
          : oib;
        break;
      }

      case 'iban': {
        let iban = z.string()
          .refine((v) => !v || validateIban(v), { message: t('validation.iban') });
        fieldSchema = field.isRequired
          ? z.string().min(1, t('common.required')).pipe(z.string().refine(validateIban, { message: t('validation.iban') }))
          : iban;
        break;
      }

      case 'email': {
        const email = z.string().email(t('validation.email'));
        fieldSchema = field.isRequired ? email.min(1, t('common.required')) : email.or(z.literal(''));
        break;
      }

      default: {
        let str = z.string();
        if (field.isRequired) str = str.min(1, t('common.required'));
        if (field.validationRules?.minLength) str = str.min(field.validationRules.minLength, t('validation.minLength', { min: field.validationRules.minLength }));
        if (field.validationRules?.maxLength) str = str.max(field.validationRules.maxLength, t('validation.maxLength', { max: field.validationRules.maxLength }));
        if (field.validationRules?.regex) {
          const regex = new RegExp(field.validationRules.regex);
          fieldSchema = str.refine((v) => !v || regex.test(v), { message: t('validation.pattern') });
        } else {
          fieldSchema = str;
        }
        break;
      }
    }

    shape[field.fieldKey] = fieldSchema;
  }

  return z.object(shape);
}
