"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";

export interface ApplicationNavigationItem {
  href: string;
  label: string;
  description?: string;
}

export interface ApplicationBrand {
  name: string;
  contextLabel: string;
  logoUrl?: string | null;
}

export function ApplicationShell({
  variant,
  brand,
  navigation,
  currentPath,
  account,
  children,
  footer,
}: {
  variant: "storefront" | "workspace";
  brand: ApplicationBrand;
  navigation: readonly ApplicationNavigationItem[];
  currentPath: string;
  account?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
}) {
  const [isMenuOpen, setMenuOpen] = useState(false);
  const menuButtonRef = useRef<HTMLButtonElement>(null);
  const menuDialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = menuDialogRef.current;
    if (!dialog) return;
    if (isMenuOpen && !dialog.open) dialog.showModal();
    if (!isMenuOpen && dialog.open) dialog.close();
  }, [isMenuOpen]);

  function closeMenu(restoreFocus = true) {
    setMenuOpen(false);
    if (restoreFocus) {
      requestAnimationFrame(() => menuButtonRef.current?.focus());
    }
  }

  const navigationContent = (
    <NavigationLinks
      items={navigation}
      currentPath={currentPath}
      onNavigate={() => closeMenu(false)}
    />
  );

  return (
    <div className={`application-shell application-shell--${variant}`}>
      <a className="application-shell__skip-link" href="#main-content">
        Skip to main content
      </a>

      <header className="application-shell__header">
        <a
          className="application-shell__brand"
          href="/"
          aria-label={`${brand.name} home`}
        >
          {brand.logoUrl ? (
            <span
              className="application-shell__brand-image"
              style={{ backgroundImage: `url(${brand.logoUrl})` }}
              aria-hidden
            />
          ) : (
            <span className="application-shell__brand-mark" aria-hidden>
              {brand.name.slice(0, 1).toUpperCase()}
            </span>
          )}
          <span>
            <strong>{brand.name}</strong>
            <small>{brand.contextLabel}</small>
          </span>
        </a>

        {variant === "storefront" && (
          <nav
            className="application-shell__desktop-top-nav"
            aria-label="Primary navigation"
          >
            {navigationContent}
          </nav>
        )}

        <div className="application-shell__account">{account}</div>
        <button
          ref={menuButtonRef}
          className="application-shell__menu-button"
          type="button"
          aria-haspopup="dialog"
          aria-expanded={isMenuOpen}
          aria-controls="mobile-navigation"
          onClick={() => setMenuOpen(true)}
        >
          <span aria-hidden>Menu</span>
          <span className="ds-sr-only">Open navigation</span>
        </button>
      </header>

      {variant === "workspace" && (
        <aside className="application-shell__sidebar">
          <nav aria-label="Workspace navigation">{navigationContent}</nav>
        </aside>
      )}

      <dialog
        ref={menuDialogRef}
        id="mobile-navigation"
        className="application-shell__mobile-dialog"
        aria-label="Navigation"
        onCancel={(event) => {
          event.preventDefault();
          closeMenu();
        }}
        onClose={() => setMenuOpen(false)}
      >
        <div className="application-shell__mobile-header">
          <strong>{brand.name}</strong>
          <button type="button" onClick={() => closeMenu()}>
            Close
          </button>
        </div>
        <nav aria-label="Mobile navigation">{navigationContent}</nav>
        {account && (
          <div className="application-shell__mobile-account">{account}</div>
        )}
      </dialog>

      <main id="main-content" className="application-shell__main" tabIndex={-1}>
        {children}
      </main>
      {footer && (
        <footer className="application-shell__footer">{footer}</footer>
      )}
    </div>
  );
}

function NavigationLinks({
  items,
  currentPath,
  onNavigate,
}: {
  items: readonly ApplicationNavigationItem[];
  currentPath: string;
  onNavigate(): void;
}) {
  return (
    <ul className="application-shell__navigation-list">
      {items.map((item) => {
        const isCurrent = matchesPath(currentPath, item.href);
        return (
          <li key={item.href}>
            <a
              href={item.href}
              aria-current={isCurrent ? "page" : undefined}
              onClick={onNavigate}
            >
              <span>{item.label}</span>
              {item.description && <small>{item.description}</small>}
            </a>
          </li>
        );
      })}
    </ul>
  );
}

function matchesPath(currentPath: string, href: string): boolean {
  return href === "/"
    ? currentPath === href
    : currentPath === href || currentPath.startsWith(`${href}/`);
}
