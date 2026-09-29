import { Badge, Skeleton, type FeedbackTone } from "@modular-mlm/design-system";
import {
  FulfillmentStatus,
  OrderStatus,
  PaymentStatus,
  type ProductDto,
} from "@modular-mlm/contracts";
import Image from "next/image";
import Link from "next/link";
import type { ReactNode } from "react";
import { money } from "./ScreenState";

export function StorefrontHero({
  storeName,
  productCount,
  categoryCount,
}: {
  storeName: string;
  productCount: number;
  categoryCount: number;
}) {
  return (
    <section className="storefront-hero">
      <div className="storefront-hero__content">
        <p className="storefront-kicker">Curated for everyday living</p>
        <h1>Discover something worth bringing home.</h1>
        <p>
          Shop the latest collection from {storeName}. Find current prices and
          see what is ready to order.
        </p>
        <div className="storefront-hero__actions">
          <Link className="ds-button ds-button--primary" href="/products">
            Shop the collection
          </Link>
          <a className="storefront-text-link" href="#featured-products">
            Browse featured products <span aria-hidden>→</span>
          </a>
        </div>
        <dl className="storefront-hero__facts">
          <div>
            <dt>Live products</dt>
            <dd>{productCount}</dd>
          </div>
          <div>
            <dt>Collections</dt>
            <dd>{categoryCount}</dd>
          </div>
          <div>
            <dt>Checkout</dt>
            <dd>Secure</dd>
          </div>
        </dl>
      </div>
      <div className="storefront-hero__visual" aria-hidden>
        <span className="storefront-hero__orb storefront-hero__orb--one" />
        <span className="storefront-hero__orb storefront-hero__orb--two" />
        <div className="storefront-hero__visual-card">
          <span>Curated edit</span>
          <strong>New season essentials</strong>
          <small>Thoughtfully selected. Ready to shop.</small>
        </div>
      </div>
    </section>
  );
}

export function StorefrontSectionHeader({
  eyebrow,
  title,
  description,
  action,
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <header className="storefront-section-header">
      <div>
        {eyebrow && <p className="storefront-kicker">{eyebrow}</p>}
        <h2>{title}</h2>
        {description && <p>{description}</p>}
      </div>
      {action}
    </header>
  );
}

export function ProductCard({
  product,
  currency,
}: {
  product: ProductDto;
  currency: string;
}) {
  return (
    <article className="storefront-product-card">
      <Link
        className="storefront-product-card__media"
        href={`/products/${product.slug}`}
        aria-label={`View ${product.name}`}
      >
        {product.imageUrl ? (
          <Image
            src={product.imageUrl}
            alt={product.name}
            width={560}
            height={680}
            sizes="(max-width: 640px) 50vw, (max-width: 1024px) 33vw, 25vw"
            unoptimized
          />
        ) : (
          <ProductImagePlaceholder />
        )}
        <span className="storefront-product-card__view">View product</span>
      </Link>
      <div className="storefront-product-card__body">
        <div>
          <h3>
            <Link href={`/products/${product.slug}`}>{product.name}</Link>
          </h3>
        </div>
        <strong>{money(product.price, currency)}</strong>
      </div>
    </article>
  );
}

export function ProductImagePlaceholder({ label = "Product image" }) {
  return (
    <div
      className="storefront-product-placeholder"
      role="img"
      aria-label={label}
    >
      <span aria-hidden>◇</span>
      <small>Image coming soon</small>
    </div>
  );
}

export function ProductGridSkeleton({ count = 8 }: { count?: number }) {
  return (
    <div className="storefront-product-grid" aria-hidden>
      {Array.from({ length: count }, (_, index) => (
        <div className="storefront-product-card" key={index}>
          <Skeleton height="18rem" />
          <div className="storefront-product-card__body">
            <Skeleton width="70%" />
            <Skeleton width="5rem" />
          </div>
        </div>
      ))}
    </div>
  );
}

export function TrustStrip() {
  return (
    <section className="storefront-trust-strip" aria-label="Shopping benefits">
      <TrustItem icon="✓" title="Live availability">
        Availability is confirmed again before checkout.
      </TrustItem>
      <TrustItem icon="▣" title="Secure payment">
        Complete payment through a secure checkout page.
      </TrustItem>
      <TrustItem icon="↺" title="Order support">
        Track orders and request eligible cancellations or refunds.
      </TrustItem>
    </section>
  );
}

function TrustItem({
  icon,
  title,
  children,
}: {
  icon: string;
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="storefront-trust-item">
      <span className="storefront-trust-item__icon" aria-hidden>
        {icon}
      </span>
      <div>
        <strong>{title}</strong>
        <p>{children}</p>
      </div>
    </div>
  );
}

export function OrderStatusBadge({ value }: { value: number }) {
  return <MappedStatusBadge value={value} map={orderStatuses} />;
}

export function PaymentStatusBadge({ value }: { value: number }) {
  return <MappedStatusBadge value={value} map={paymentStatuses} />;
}

export function FulfillmentStatusBadge({ value }: { value: number }) {
  return <MappedStatusBadge value={value} map={fulfillmentStatuses} />;
}

type StatusPresentation = { label: string; tone: FeedbackTone };

function MappedStatusBadge({
  value,
  map,
}: {
  value: number;
  map: Readonly<Record<number, StatusPresentation>>;
}) {
  const presentation = map[value] ?? {
    label: `Unknown (${value})`,
    tone: "neutral" as const,
  };
  return <Badge tone={presentation.tone}>{presentation.label}</Badge>;
}

const orderStatuses: Readonly<Record<number, StatusPresentation>> = {
  [OrderStatus.pendingPayment]: { label: "Awaiting payment", tone: "warning" },
  [OrderStatus.paid]: { label: "Paid", tone: "info" },
  [OrderStatus.processing]: { label: "Processing", tone: "info" },
  [OrderStatus.shipped]: { label: "Shipped", tone: "info" },
  [OrderStatus.delivered]: { label: "Delivered", tone: "success" },
  [OrderStatus.cancelled]: { label: "Cancelled", tone: "danger" },
  [OrderStatus.partiallyRefunded]: {
    label: "Partially refunded",
    tone: "warning",
  },
  [OrderStatus.refunded]: { label: "Refunded", tone: "neutral" },
};

const paymentStatuses: Readonly<Record<number, StatusPresentation>> = {
  [PaymentStatus.pending]: { label: "Payment pending", tone: "warning" },
  [PaymentStatus.authorized]: { label: "Authorized", tone: "info" },
  [PaymentStatus.paid]: { label: "Payment received", tone: "success" },
  [PaymentStatus.failed]: { label: "Payment failed", tone: "danger" },
  [PaymentStatus.partiallyRefunded]: {
    label: "Partially refunded",
    tone: "warning",
  },
  [PaymentStatus.refunded]: { label: "Refunded", tone: "neutral" },
};

const fulfillmentStatuses: Readonly<Record<number, StatusPresentation>> = {
  [FulfillmentStatus.unfulfilled]: {
    label: "Not yet shipped",
    tone: "warning",
  },
  [FulfillmentStatus.processing]: { label: "Processing", tone: "info" },
  [FulfillmentStatus.shipped]: { label: "Shipped", tone: "info" },
  [FulfillmentStatus.delivered]: { label: "Delivered", tone: "success" },
  [FulfillmentStatus.cancelled]: { label: "Cancelled", tone: "danger" },
  [FulfillmentStatus.refunded]: { label: "Refunded", tone: "neutral" },
};

export function CheckoutSteps({ current }: { current: 1 | 2 | 3 }) {
  return (
    <ol className="storefront-checkout-steps" aria-label="Checkout progress">
      {["Delivery", "Review", "Payment"].map((label, index) => {
        const step = (index + 1) as 1 | 2 | 3;
        return (
          <li
            key={label}
            data-active={step <= current}
            aria-current={step === current ? "step" : undefined}
          >
            <span>{step}</span>
            {label}
          </li>
        );
      })}
    </ol>
  );
}

export function StorefrontFooter({ storeName }: { storeName: string }) {
  return (
    <div className="storefront-footer">
      <div>
        <strong>{storeName}</strong>
        <p>Browse the collection, checkout securely, and follow your orders.</p>
      </div>
      <nav aria-label="Footer navigation">
        <Link href="/products">Shop</Link>
        <Link href="/cart">Cart</Link>
        <Link href="/account/orders">Orders</Link>
      </nav>
    </div>
  );
}
