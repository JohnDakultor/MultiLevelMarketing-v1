"use client";

import {
  Activity,
  BarChart3,
  Bell,
  Boxes,
  Building2,
  CircleDollarSign,
  ClipboardList,
  CreditCard,
  LayoutDashboard,
  LockKeyhole,
  Network,
  ShieldCheck,
  ShoppingBag,
  Users,
  WalletCards,
  type LucideIcon,
} from "lucide-react";
import {
  useEffect,
  useRef,
  useState,
  type ComponentType,
  type MouseEventHandler,
  type ReactNode,
} from "react";

export type ApplicationLinkComponent = ComponentType<{
  href: string;
  children: ReactNode;
  className?: string;
  "aria-label"?: string;
  "aria-current"?: "page";
  onClick?: MouseEventHandler<HTMLAnchorElement>;
}>;

export interface ApplicationNavigationItem {
  href: string;
  label: string;
  description?: string;
  group?: string;
  icon?: ApplicationNavigationIcon;
}

export type ApplicationNavigationIcon =
  | "activity"
  | "administrators"
  | "agents"
  | "catalog"
  | "compensation"
  | "customers"
  | "dashboard"
  | "finance"
  | "notifications"
  | "operations"
  | "orders"
  | "organization"
  | "payouts"
  | "reports"
  | "security";

const navigationIcons: Record<ApplicationNavigationIcon, LucideIcon> = {
  activity: Activity,
  administrators: ShieldCheck,
  agents: Network,
  catalog: Boxes,
  compensation: CircleDollarSign,
  customers: Users,
  dashboard: LayoutDashboard,
  finance: WalletCards,
  notifications: Bell,
  operations: ClipboardList,
  orders: ShoppingBag,
  organization: Building2,
  payouts: CreditCard,
  reports: BarChart3,
  security: LockKeyhole,
};

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
  linkComponent,
  appearance = "default",
}: {
  variant: "storefront" | "workspace";
  brand: ApplicationBrand;
  navigation: readonly ApplicationNavigationItem[];
  currentPath: string;
  account?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
  linkComponent?: ApplicationLinkComponent;
  appearance?: "default" | "enterprise";
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

  const isEnterpriseWorkspace =
    variant === "workspace" && appearance === "enterprise";
  const currentItem = [...navigation]
    .sort((left, right) => right.href.length - left.href.length)
    .find((item) => matchesPath(currentPath, item.href));

  const navigationContent = (
    <NavigationLinks
      items={navigation}
      currentPath={currentPath}
      onNavigate={() => closeMenu(false)}
      linkComponent={linkComponent}
      grouped={isEnterpriseWorkspace}
    />
  );

  const Link = linkComponent ?? "a";

  return (
    <div
      className={`application-shell application-shell--${variant} application-shell--${appearance}`}
    >
      <a className="application-shell__skip-link" href="#main-content">
        Skip to main content
      </a>

      <header className="application-shell__header">
        {!isEnterpriseWorkspace && <BrandLink brand={brand} Link={Link} />}

        {isEnterpriseWorkspace && (
          <div className="application-shell__workspace-context">
            <span>{brand.contextLabel}</span>
            <strong>{currentItem?.label ?? "Workspace"}</strong>
          </div>
        )}

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
          {isEnterpriseWorkspace && (
            <div className="application-shell__sidebar-brand">
              <BrandLink brand={brand} Link={Link} />
            </div>
          )}
          <nav aria-label="Workspace navigation">{navigationContent}</nav>
          {isEnterpriseWorkspace && footer && (
            <div className="application-shell__sidebar-footer">{footer}</div>
          )}
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
          <BrandLink brand={brand} Link={Link} />
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
      {footer && !isEnterpriseWorkspace && (
        <footer className="application-shell__footer">{footer}</footer>
      )}
    </div>
  );
}

function NavigationLinks({
  items,
  currentPath,
  onNavigate,
  linkComponent,
  grouped = false,
}: {
  items: readonly ApplicationNavigationItem[];
  currentPath: string;
  onNavigate(): void;
  linkComponent?: ApplicationLinkComponent;
  grouped?: boolean;
}) {
  const Link = linkComponent ?? "a";

  if (grouped) {
    const groups = items.reduce<
      Array<{ label: string; items: ApplicationNavigationItem[] }>
    >((result, item) => {
      const label = item.group ?? "Workspace";
      const existing = result.find((group) => group.label === label);
      if (existing) existing.items.push(item);
      else result.push({ label, items: [item] });
      return result;
    }, []);

    return (
      <div className="application-shell__navigation-groups">
        {groups.map((group) => (
          <div
            className="application-shell__navigation-group"
            key={group.label}
          >
            <p>{group.label}</p>
            <NavigationList
              items={group.items}
              currentPath={currentPath}
              onNavigate={onNavigate}
              Link={Link}
            />
          </div>
        ))}
      </div>
    );
  }

  return (
    <NavigationList
      items={items}
      currentPath={currentPath}
      onNavigate={onNavigate}
      Link={Link}
    />
  );
}

function NavigationList({
  items,
  currentPath,
  onNavigate,
  Link,
}: {
  items: readonly ApplicationNavigationItem[];
  currentPath: string;
  onNavigate(): void;
  Link: ApplicationLinkComponent | "a";
}) {
  return (
    <ul className="application-shell__navigation-list">
      {items.map((item) => {
        const isCurrent = matchesPath(currentPath, item.href);
        const Icon = item.icon ? navigationIcons[item.icon] : null;
        return (
          <li key={item.href}>
            <Link
              href={item.href}
              aria-current={isCurrent ? "page" : undefined}
              onClick={onNavigate}
            >
              {Icon && (
                <Icon className="application-shell__nav-icon" aria-hidden />
              )}
              <span className="application-shell__nav-copy">
                <span>{item.label}</span>
                {item.description && <small>{item.description}</small>}
              </span>
            </Link>
          </li>
        );
      })}
    </ul>
  );
}

function BrandLink({
  brand,
  Link,
}: {
  brand: ApplicationBrand;
  Link: ApplicationLinkComponent | "a";
}) {
  return (
    <Link
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
    </Link>
  );
}

function matchesPath(currentPath: string, href: string): boolean {
  return href === "/"
    ? currentPath === href
    : currentPath === href || currentPath.startsWith(`${href}/`);
}
