import type {
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from "react";

interface FieldDescriptionProps {
  id: string;
  hint?: string;
  error?: string;
}

function FieldDescription({ id, hint, error }: FieldDescriptionProps) {
  return (
    <>
      {hint && (
        <span className="ds-field__hint" id={`${id}-hint`}>
          {hint}
        </span>
      )}
      {error && (
        <span className="ds-field__error" id={`${id}-error`} role="alert">
          {error}
        </span>
      )}
    </>
  );
}

function describedBy(
  id: string,
  hint?: string,
  error?: string,
): string | undefined {
  const ids = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(
    Boolean,
  );
  return ids.length > 0 ? ids.join(" ") : undefined;
}

export function InputField({
  label,
  hint,
  error,
  className = "",
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
  error?: string;
}) {
  const id = props.id ?? props.name;
  if (!id) throw new Error("InputField requires an id or name.");

  return (
    <label className={`ds-field ${className}`.trim()} htmlFor={id}>
      <span className="ds-field__label">
        {label}
        {props.required && <span aria-hidden> *</span>}
      </span>
      <FieldDescription id={id} hint={hint} />
      <input
        {...props}
        id={id}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy(id, hint, error)}
      />
      {error && <FieldDescription id={id} error={error} />}
    </label>
  );
}

export function TextareaField({
  label,
  hint,
  error,
  ...props
}: TextareaHTMLAttributes<HTMLTextAreaElement> & {
  label: string;
  hint?: string;
  error?: string;
}) {
  const id = props.id ?? props.name;
  if (!id) throw new Error("TextareaField requires an id or name.");

  return (
    <label className="ds-field" htmlFor={id}>
      <span className="ds-field__label">{label}</span>
      <FieldDescription id={id} hint={hint} />
      <textarea
        {...props}
        id={id}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy(id, hint, error)}
      />
      {error && <FieldDescription id={id} error={error} />}
    </label>
  );
}

export function SelectField({
  label,
  hint,
  error,
  children,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  hint?: string;
  error?: string;
  children: ReactNode;
}) {
  const id = props.id ?? props.name;
  if (!id) throw new Error("SelectField requires an id or name.");

  return (
    <label className="ds-field" htmlFor={id}>
      <span className="ds-field__label">{label}</span>
      <FieldDescription id={id} hint={hint} />
      <select
        {...props}
        id={id}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy(id, hint, error)}
      >
        {children}
      </select>
      {error && <FieldDescription id={id} error={error} />}
    </label>
  );
}

export function Checkbox({
  label,
  description,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  description?: string;
}) {
  const id = props.id ?? props.name;
  if (!id) throw new Error("Checkbox requires an id or name.");

  return (
    <label className="ds-choice" htmlFor={id}>
      <input {...props} id={id} type="checkbox" />
      <span>
        <strong>{label}</strong>
        {description && <small>{description}</small>}
      </span>
    </label>
  );
}

export interface RadioOption<T extends string> {
  value: T;
  label: string;
  description?: string;
}

export function RadioGroup<T extends string>({
  legend,
  name,
  value,
  options,
  onChange,
}: {
  legend: string;
  name: string;
  value: T;
  options: readonly RadioOption<T>[];
  onChange(value: T): void;
}) {
  return (
    <fieldset className="ds-radio-group">
      <legend>{legend}</legend>
      {options.map((option) => (
        <label className="ds-choice" key={option.value}>
          <input
            type="radio"
            name={name}
            value={option.value}
            checked={value === option.value}
            onChange={() => onChange(option.value)}
          />
          <span>
            <strong>{option.label}</strong>
            {option.description && <small>{option.description}</small>}
          </span>
        </label>
      ))}
    </fieldset>
  );
}
