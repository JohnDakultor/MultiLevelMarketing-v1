"use client";

import { useEffect, useId, useRef, type ReactNode } from "react";
import { Button } from "./Button";

export function Dialog({
  isOpen,
  title,
  description,
  children,
  onClose,
}: {
  isOpen: boolean;
  title: string;
  description?: string;
  children: ReactNode;
  onClose(): void;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descriptionId = useId();

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (isOpen && !dialog.open) {
      dialog.showModal();
      requestAnimationFrame(() =>
        dialog
          .querySelector<HTMLElement>(
            "button, input, select, textarea, a[href]",
          )
          ?.focus(),
      );
    }
    if (!isOpen && dialog.open) dialog.close();
  }, [isOpen]);

  return (
    <dialog
      ref={dialogRef}
      className="ds-dialog"
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClose={onClose}
    >
      <div className="ds-dialog__header">
        <div>
          <h2 id={titleId}>{title}</h2>
          {description && <p id={descriptionId}>{description}</p>}
        </div>
        <Button
          variant="ghost"
          type="button"
          onClick={onClose}
          aria-label="Close dialog"
        >
          Close
        </Button>
      </div>
      {children}
    </dialog>
  );
}

export function ConfirmationDialog({
  isOpen,
  title,
  description,
  confirmLabel,
  isConfirming,
  onConfirm,
  onClose,
}: {
  isOpen: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  isConfirming?: boolean;
  onConfirm(): void;
  onClose(): void;
}) {
  return (
    <Dialog
      isOpen={isOpen}
      title={title}
      description={description}
      onClose={onClose}
    >
      <div className="ds-dialog__actions">
        <Button
          variant="secondary"
          type="button"
          onClick={onClose}
          disabled={isConfirming}
        >
          Cancel
        </Button>
        <Button
          variant="danger"
          type="button"
          isLoading={isConfirming}
          disabled={isConfirming}
          onClick={onConfirm}
        >
          {confirmLabel}
        </Button>
      </div>
    </Dialog>
  );
}
