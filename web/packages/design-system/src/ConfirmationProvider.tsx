"use client";

import {
  createContext,
  useCallback,
  useContext,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { ConfirmationDialog } from "./Dialog";
import { Dialog } from "./Dialog";
import { Button } from "./Button";
import { InputField } from "./FormControls";

export interface ConfirmationRequest {
  title: string;
  description: string;
  confirmLabel: string;
}

export interface PromptRequest {
  title: string;
  description: string;
  label: string;
  submitLabel: string;
  maxLength?: number;
}

interface ConfirmationContextValue {
  confirm(request: ConfirmationRequest): Promise<boolean>;
  prompt(request: PromptRequest): Promise<string | null>;
}

const ConfirmationContext = createContext<ConfirmationContextValue | null>(
  null,
);

export function ConfirmationProvider({ children }: { children: ReactNode }) {
  const resolver = useRef<((confirmed: boolean) => void) | null>(null);
  const promptResolver = useRef<((value: string | null) => void) | null>(null);
  const [request, setRequest] = useState<ConfirmationRequest | null>(null);
  const [promptRequest, setPromptRequest] = useState<PromptRequest | null>(
    null,
  );

  const close = useCallback((confirmed: boolean) => {
    resolver.current?.(confirmed);
    resolver.current = null;
    setRequest(null);
  }, []);

  const confirm = useCallback((nextRequest: ConfirmationRequest) => {
    resolver.current?.(false);
    setRequest(nextRequest);
    return new Promise<boolean>((resolve) => {
      resolver.current = resolve;
    });
  }, []);

  const prompt = useCallback((nextRequest: PromptRequest) => {
    promptResolver.current?.(null);
    setPromptRequest(nextRequest);
    return new Promise<string | null>((resolve) => {
      promptResolver.current = resolve;
    });
  }, []);

  const closePrompt = useCallback((value: string | null) => {
    promptResolver.current?.(value);
    promptResolver.current = null;
    setPromptRequest(null);
  }, []);

  return (
    <ConfirmationContext.Provider value={{ confirm, prompt }}>
      {children}
      <ConfirmationDialog
        isOpen={request !== null}
        title={request?.title ?? "Confirm action"}
        description={request?.description ?? ""}
        confirmLabel={request?.confirmLabel ?? "Confirm"}
        onConfirm={() => close(true)}
        onClose={() => close(false)}
      />
      <Dialog
        isOpen={promptRequest !== null}
        title={promptRequest?.title ?? "Provide details"}
        description={promptRequest?.description}
        onClose={() => closePrompt(null)}
      >
        <form
          className="form-grid"
          onSubmit={(event) => {
            event.preventDefault();
            const value = String(
              new FormData(event.currentTarget).get("promptValue") ?? "",
            ).trim();
            if (value) closePrompt(value);
          }}
        >
          <InputField
            name="promptValue"
            label={promptRequest?.label ?? "Reason"}
            required
            maxLength={promptRequest?.maxLength ?? 500}
          />
          <div className="ds-dialog__actions">
            <Button
              type="button"
              variant="secondary"
              onClick={() => closePrompt(null)}
            >
              Cancel
            </Button>
            <Button type="submit" variant="danger">
              {promptRequest?.submitLabel ?? "Continue"}
            </Button>
          </div>
        </form>
      </Dialog>
    </ConfirmationContext.Provider>
  );
}

export function useConfirmation(): ConfirmationContextValue {
  const value = useContext(ConfirmationContext);
  if (!value)
    throw new Error(
      "useConfirmation must be used inside ConfirmationProvider.",
    );
  return value;
}
