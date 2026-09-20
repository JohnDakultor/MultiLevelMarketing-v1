"use client";

import { useApiClient, useApiQuery } from "@modular-mlm/api-client";
import {
  Button,
  Card,
  EmptyState,
  PageHeader,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";
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
            <a className="ds-button ds-button--primary" href="/products">
              Browse products
            </a>
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
    <div className="content-stack">
      <PageHeader
        title="Your cart"
        description="Prices, availability, and totals are refreshed from the server."
      />
      {message && <p role="alert">{message}</p>}
      <div className="content-stack">
        {cart.data.items.map((item) => (
          <Card key={item.id} className="cart-row">
            <div>
              <h2>
                <a href={`/products/${item.productSlug}`}>{item.productName}</a>
              </h2>
              <p>
                {item.sku} · {item.stockStatus}
              </p>
              {!item.isAvailable && (
                <strong className="danger-text">
                  Remove this unavailable item before checkout.
                </strong>
              )}
            </div>
            <div>
              <label>
                Quantity{" "}
                <input
                  aria-label={`Quantity for ${item.productName}`}
                  type="number"
                  min={1}
                  value={item.quantity}
                  disabled={workingItem === item.id}
                  onChange={(event) =>
                    void mutate(item.id, () =>
                      storefrontApi.updateCartItem(
                        api,
                        organization!.id,
                        item.id,
                        event.target.valueAsNumber,
                      ),
                    )
                  }
                />
              </label>
              <p>{money(item.lineTotal, cart.data!.currency)}</p>
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
      <Card className="cart-total">
        <div>
          <span>Subtotal</span>
          <strong>{money(cart.data.subtotal, cart.data.currency)}</strong>
        </div>
        <p>
          {cart.data.itemCount} item(s). Shipping, tax, and final eligibility
          are determined at checkout.
        </p>
        <a className="ds-button ds-button--primary" href="/checkout">
          Continue to checkout
        </a>
      </Card>
      <Card>
        <h2>Referral code</h2>
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
              setMessage("Referral code applied.");
              cart.reload();
            } catch (error) {
              setMessage(
                error instanceof Error
                  ? error.message
                  : "The referral code could not be applied.",
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
    </div>
  );
}
