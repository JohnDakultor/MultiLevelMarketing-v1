"use client";

import { useEffect, type ReactNode } from "react";

export function Drawer({
  isOpen,
  title,
  children,
  onClose,
}: {
  isOpen: boolean;
  title: string;
  children: ReactNode;
  onClose(): void;
}) {
  useEffect(() => {
    if (!isOpen) return;
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", closeOnEscape);
    return () => document.removeEventListener("keydown", closeOnEscape);
  }, [isOpen, onClose]);

  if (!isOpen) return null;
  return (
    <div
      className="ds-drawer-backdrop"
      role="presentation"
      onMouseDown={onClose}
    >
      <aside
        className="ds-drawer"
        role="dialog"
        aria-modal="true"
        aria-labelledby="drawer-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header>
          <h2 id="drawer-title">{title}</h2>
          <button type="button" onClick={onClose} aria-label="Close drawer">
            Close
          </button>
        </header>
        {children}
      </aside>
    </div>
  );
}

export function Dropdown({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <details className="ds-dropdown">
      <summary>{label}</summary>
      <div className="ds-dropdown__menu">{children}</div>
    </details>
  );
}

export function Tooltip({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <span className="ds-tooltip" data-tooltip={label} aria-label={label}>
      {children}
    </span>
  );
}
