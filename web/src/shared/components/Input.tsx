import { forwardRef, type InputHTMLAttributes, type TextareaHTMLAttributes, type SelectHTMLAttributes, type ReactNode } from 'react';

const baseInputClasses =
  'block w-full rounded-lg border px-3 py-2 min-h-touch text-base focus:outline-none focus:ring-2 focus:ring-offset-0 disabled:bg-gray-100 disabled:cursor-not-allowed';

const errorClasses = 'border-error focus:ring-error';
const normalClasses = 'border-border focus:ring-primary';

interface BaseFieldProps {
  label?: string;
  error?: string;
  helpText?: string;
  required?: boolean;
}

interface InputProps extends InputHTMLAttributes<HTMLInputElement>, BaseFieldProps {}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helpText, required, className = '', id, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className="w-full">
        {label && (
          <label htmlFor={inputId} className="block text-sm font-medium text-text-primary mb-1">
            {label}
            {required && <span className="text-error ml-0.5">*</span>}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={`${baseInputClasses} ${error ? errorClasses : normalClasses} ${className}`}
          aria-invalid={!!error}
          aria-describedby={error ? `${inputId}-error` : helpText ? `${inputId}-help` : undefined}
          {...props}
        />
        {helpText && !error && (
          <p id={`${inputId}-help`} className="mt-1 text-sm text-text-secondary">{helpText}</p>
        )}
        {error && (
          <p id={`${inputId}-error`} className="mt-1 text-sm text-error" role="alert">{error}</p>
        )}
      </div>
    );
  },
);
Input.displayName = 'Input';

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement>, BaseFieldProps {}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ label, error, helpText, required, className = '', id, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className="w-full">
        {label && (
          <label htmlFor={inputId} className="block text-sm font-medium text-text-primary mb-1">
            {label}
            {required && <span className="text-error ml-0.5">*</span>}
          </label>
        )}
        <textarea
          ref={ref}
          id={inputId}
          className={`${baseInputClasses} min-h-[100px] ${error ? errorClasses : normalClasses} ${className}`}
          aria-invalid={!!error}
          aria-describedby={error ? `${inputId}-error` : helpText ? `${inputId}-help` : undefined}
          {...props}
        />
        {helpText && !error && (
          <p id={`${inputId}-help`} className="mt-1 text-sm text-text-secondary">{helpText}</p>
        )}
        {error && (
          <p id={`${inputId}-error`} className="mt-1 text-sm text-error" role="alert">{error}</p>
        )}
      </div>
    );
  },
);
Textarea.displayName = 'Textarea';

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement>, BaseFieldProps {
  options: { value: string; label: string }[];
  placeholder?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, helpText, required, options, placeholder, className = '', id, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className="w-full">
        {label && (
          <label htmlFor={inputId} className="block text-sm font-medium text-text-primary mb-1">
            {label}
            {required && <span className="text-error ml-0.5">*</span>}
          </label>
        )}
        <select
          ref={ref}
          id={inputId}
          className={`${baseInputClasses} ${error ? errorClasses : normalClasses} ${className}`}
          aria-invalid={!!error}
          aria-describedby={error ? `${inputId}-error` : helpText ? `${inputId}-help` : undefined}
          {...props}
        >
          {placeholder && <option value="">{placeholder}</option>}
          {options.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
        {helpText && !error && (
          <p id={`${inputId}-help`} className="mt-1 text-sm text-text-secondary">{helpText}</p>
        )}
        {error && (
          <p id={`${inputId}-error`} className="mt-1 text-sm text-error" role="alert">{error}</p>
        )}
      </div>
    );
  },
);
Select.displayName = 'Select';

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>, BaseFieldProps {
  children?: ReactNode;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ label, error, helpText, children, className = '', id, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className="w-full">
        <label htmlFor={inputId} className={`inline-flex items-start gap-2 cursor-pointer ${className}`}>
          <input
            ref={ref}
            id={inputId}
            type="checkbox"
            className="mt-1 h-4 w-4 rounded border-border text-primary focus:ring-primary"
            aria-invalid={!!error}
            {...props}
          />
          <span className="text-sm text-text-primary">{children ?? label}</span>
        </label>
        {helpText && !error && (
          <p className="mt-1 ml-6 text-sm text-text-secondary">{helpText}</p>
        )}
        {error && (
          <p className="mt-1 ml-6 text-sm text-error" role="alert">{error}</p>
        )}
      </div>
    );
  },
);
Checkbox.displayName = 'Checkbox';
