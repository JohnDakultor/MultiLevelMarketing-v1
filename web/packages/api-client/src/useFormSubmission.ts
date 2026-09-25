"use client";

import { useCallback, useId, useRef, useState } from "react";
import {
  firstFieldError,
  validationFieldErrors,
  validationMessages,
  type FieldErrors,
} from "./ApiError";

export interface FormSubmissionState {
  isSubmitting: boolean;
  fieldErrors: FieldErrors;
  formErrors: readonly string[];
  successMessage: string | null;
  errorSummaryId: string;
  successId: string;
  fieldError(name: string): string | undefined;
  submit(
    operation: () => Promise<unknown>,
    successMessage: string,
  ): Promise<boolean>;
  clear(): void;
}

export function useFormSubmission(): FormSubmissionState {
  const lock = useRef(false);
  const formId = useId().replaceAll(":", "");
  const errorSummaryId = `${formId}-errors`;
  const successId = `${formId}-success`;
  const [isSubmitting, setSubmitting] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formErrors, setFormErrors] = useState<readonly string[]>([]);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const clear = useCallback(() => {
    setFieldErrors({});
    setFormErrors([]);
    setSuccessMessage(null);
  }, []);

  const submit = useCallback(
    async (operation: () => Promise<unknown>, message: string) => {
      if (lock.current) return false;
      lock.current = true;
      setSubmitting(true);
      clear();
      try {
        await operation();
        setSuccessMessage(message);
        requestAnimationFrame(() =>
          document.getElementById(successId)?.focus(),
        );
        return true;
      } catch (error) {
        const mapped = validationFieldErrors(error);
        setFieldErrors(mapped);
        setFormErrors(
          Object.keys(mapped).length > 0 ? [] : validationMessages(error),
        );
        requestAnimationFrame(() =>
          document.getElementById(errorSummaryId)?.focus(),
        );
        return false;
      } finally {
        lock.current = false;
        setSubmitting(false);
      }
    },
    [clear, errorSummaryId, successId],
  );

  return {
    isSubmitting,
    fieldErrors,
    formErrors,
    successMessage,
    errorSummaryId,
    successId,
    fieldError: (name) => firstFieldError(fieldErrors, name),
    submit,
    clear,
  };
}
