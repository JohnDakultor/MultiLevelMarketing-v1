"use client";

import { useApiClient, useApiQuery } from "@modular-mlm/api-client";
import {
  Button,
  Card,
  EmptyState,
  PageHeader,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";
import Image from "next/image";
import Link from "next/link";
import { useState, type FormEvent } from "react";
import { storefrontApi } from "../api/storefrontApi";
import { ScreenError, ScreenLoading, money } from "../shared/ScreenState";

export function CartPage() {
  const api = useApiClient();
  const { organization } = useOrganization();
  const cart = useApiQuery(
    (client, signal) => storefrontApi.cart(client, organization!.id, signal),
    [organization?.id],
    Boolean(organization),
  );
  const [workingItem, setWorkingItem] = useState<string | null>(null);
  const [message, setMessage] = useState("");
  const [isApplyingReferral, setApplyingReferral] = useState(false);
  if (cart.isLoading) return <ScreenLoading />;
  if (cart.error) return <ScreenError error={cart.error} retry={cart.reload} />;
  if (!cart.data?.items.length)
    return (
      <div className="content-stack">
        <PageHeader title="Your cart" />
        <EmptyState
          title="Your cart is empty"
          description="Browse the catalog and add a product to begin checkout."
          action={
            <Link className="ds-button ds-button--primary" href="/products">
              Browse products
            </Link>
          }
        />
      </div>
    );
  const mutate = async (itemId: string, operation: () => Promise<unknown>) => {
    setWorkingItem(itemId);
    setMessage("");
    try {
      await operation();
      cart.reload();
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "The cart could not be updated.",
      );
    } finally {
      setWorkingItem(null);
    }
  };
  return (
    <div className="content-stack storefront-cart-page">
      <PageHeader
        eyebrow="Bag"
        title="Your cart"
        description="Review your items before continuing to checkout."
      />
      {message && (
        <p className="storefront-inline-feedback" role="alert">
          {message}
        </p>
      )}
      <div className="storefront-cart-layout">
        <div className="storefront-cart-items">
          {cart.data.items.map((item) => (
            <Card key={item.id} className="storefront-cart-item">
              <Link
                className="storefront-cart-item__media"
                href={`/products/${item.productSlug}`}
              >
                {item.imageUrl ? (
                  <Image
                    src={item.imageUrl}
                    alt={item.productName}
                    width={180}
                    height={220}
                    unoptimized
                  />
                ) : (
                  <span aria-hidden>◇</span>
                )}
              </Link>
              <div className="storefront-cart-item__details">
                <h2>
                  <Link href={`/products/${item.productSlug}`}>
                    {item.productName}
                  </Link>
                </h2>
                <p>
                  {item.sku} · {item.stockStatus}
                </p>
                {!item.isAvailable && (
                  <strong className="danger-text">
                    Remove this unavailable item before checkout.
                  </strong>
                )}
                <div className="storefront-cart-item__actions">
                  <div
                    className="storefront-quantity"
                    aria-label={`Quantity for ${item.productName}`}
                  >
                    <Button
                      variant="secondary"
                      disabled={item.quantity <= 1 || workingItem === item.id}
                      aria-label={`Decrease ${item.productName} quantity`}
                      onClick={() =>
                        void mutate(item.id, () =>
                          storefrontApi.updateCartItem(
                            api,
                            organization!.id,
                            item.id,
                            item.quantity - 1,
                          ),
                        )
                      }
                    >
                      −
                    </Button>
                    <output aria-live="polite">{item.quantity}</output>
                    <Button
                      variant="secondary"
                      disabled={workingItem === item.id}
                      aria-label={`Increase ${item.productName} quantity`}
                      onClick={() =>
                        void mutate(item.id, () =>
                          storefrontApi.updateCartItem(
                            api,
                            organization!.id,
                            item.id,
                            item.quantity + 1,
                          ),
                        )
                      }
                    >
                      +
                    </Button>
                  </div>
                  <strong>{money(item.lineTotal, cart.data!.currency)}</strong>
                </div>
                <Button
                  variant="ghost"
                  isLoading={workingItem === item.id}
                  onClick={() =>
                    void mutate(item.id, () =>
                      storefrontApi.removeCartItem(
                        api,
                        organization!.id,
                        item.id,
                      ),
                    )
                  }
                >
                  Remove
                </Button>
              </div>
            </Card>
          ))}
        </div>
        <aside className="storefront-cart-sidebar">
          <Card className="cart-total storefront-order-summary">
            <h2>Order summary</h2>
            <div>
              <span>Subtotal</span>
              <strong>{money(cart.data.subtotal, cart.data.currency)}</strong>
            </div>
            <p>
              {cart.data.itemCount} item(s). Shipping and the final total are
              shown at checkout.
            </p>
            <Link className="ds-button ds-button--primary" href="/checkout">
              Continue to checkout
            </Link>
            <Link className="storefront-text-link" href="/products">
              Continue shopping
            </Link>
          </Card>
          <Card className="storefront-referral-card">
            <h2>Have a shopping code?</h2>
            <p>Apply your code before continuing to checkout.</p>
            <form
              className="inline-form"
              onSubmit={async (event: FormEvent<HTMLFormElement>) => {
                event.preventDefault();
                if (!organization) return;
                const code = String(
                  new FormData(event.currentTarget).get("referralCode"),
                ).trim();
                if (!code) return;
                setApplyingReferral(true);
                setMessage("");
                try {
                  await storefrontApi.applyReferral(api, organization.id, code);
                  setMessage("Shopping code applied.");
                  cart.reload();
                } catch (error) {
                  setMessage(
                    error instanceof Error
                      ? error.message
                      : "The shopping code could not be applied.",
                  );
                } finally {
                  setApplyingReferral(false);
                }
              }}
            >
              <label>
                <span>Code</span>
                <input name="referralCode" required autoComplete="off" />
              </label>
              <Button
                type="submit"
                variant="secondary"
                isLoading={isApplyingReferral}
                disabled={isApplyingReferral}
              >
                Apply code
              </Button>
            </form>
          </Card>
        </aside>
      </div>
    </div>
  );
}
