"use client";

import { useApiClient, useApiQuery } from "@modular-mlm/api-client";
import { useAuthentication } from "@modular-mlm/auth";
import {
  Alert,
  Button,
  Card,
  EmptyState,
  PageHeader,
  SelectField,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import { addressInput, storefrontApi } from "../api/storefrontApi";
import { ScreenError, ScreenLoading, money } from "../shared/ScreenState";

export function CheckoutPage() {
  const api = useApiClient();
  const auth = useAuthentication();
  const { organization } = useOrganization();
  const addresses = useApiQuery(
    (client, signal) =>
      storefrontApi.addresses(client, organization!.id, signal),
    [organization?.id],
    Boolean(organization && auth.user),
  );
  const cart = useApiQuery(
    (client, signal) => storefrontApi.cart(client, organization!.id, signal),
    [organization?.id],
    Boolean(organization),
  );
  const [shippingId, setShippingId] = useState("");
  const [billingId, setBillingId] = useState("");
  const [isSubmitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  if (!auth.user)
    return (
      <div className="content-stack">
        <PageHeader title="Checkout" />
        <EmptyState
          title="Sign in to checkout"
          description="Your anonymous cart will be resolved by the backend after authentication."
          action={
            <a
              className="ds-button ds-button--primary"
              href="/sign-in?returnTo=%2Fcheckout"
            >
              Sign in
            </a>
          }
        />
      </div>
    );
  if (addresses.isLoading || cart.isLoading) return <ScreenLoading />;
  if (addresses.error)
    return <ScreenError error={addresses.error} retry={addresses.reload} />;
  if (cart.error) return <ScreenError error={cart.error} retry={cart.reload} />;
  if (!cart.data?.items.length)
    return (
      <EmptyState
        title="Nothing to checkout"
        description="Your cart does not contain any items."
      />
    );
  const shipping =
    addresses.data?.find((address) => address.id === shippingId) ??
    addresses.data?.find((address) => address.isDefault);
  const billing =
    addresses.data?.find((address) => address.id === billingId) ?? shipping;
  return (
    <div className="content-stack">
      <PageHeader
        title="Checkout"
        description="Review the server-authoritative cart and choose saved delivery details."
      />
      <div className="detail-grid">
        <Card>
          <h2>Delivery</h2>
          {!addresses.data?.length ? (
            <EmptyState
              title="Add an address first"
              description="Checkout uses a complete saved address snapshot."
              action={<a href="/account/addresses">Manage addresses</a>}
            />
          ) : (
            <>
              <SelectField
                id="shipping-address"
                label="Shipping address"
                value={shipping?.id ?? ""}
                onChange={(event) => setShippingId(event.target.value)}
              >
                {addresses.data.map((address) => (
                  <option key={address.id} value={address.id}>
                    {address.label} — {address.addressLine1},{" "}
                    {address.cityOrMunicipality}
                  </option>
                ))}
              </SelectField>
              <SelectField
                id="billing-address"
                label="Billing address"
                value={billing?.id ?? ""}
                onChange={(event) => setBillingId(event.target.value)}
              >
                {addresses.data.map((address) => (
                  <option key={address.id} value={address.id}>
                    {address.label} — {address.recipientName}
                  </option>
                ))}
              </SelectField>
            </>
          )}
        </Card>
        <Card>
          <h2>Order summary</h2>
          {cart.data.items.map((item) => (
            <div className="summary-line" key={item.id}>
              <span>
                {item.productName} × {item.quantity}
              </span>
              <strong>{money(item.lineTotal, cart.data!.currency)}</strong>
            </div>
          ))}
          <div className="summary-line">
            <span>Subtotal</span>
            <strong>{money(cart.data.subtotal, cart.data.currency)}</strong>
          </div>
          {error && (
            <Alert title="Checkout could not continue" tone="danger">
              {error}
            </Alert>
          )}
          <Button
            disabled={
              !shipping ||
              !billing ||
              cart.data.items.some((item) => !item.isAvailable)
            }
            isLoading={isSubmitting}
            loadingLabel="Creating order"
            onClick={async () => {
              if (!organization || !shipping || !billing) return;
              setSubmitting(true);
              setError("");
              try {
                const orderId = await storefrontApi.checkout(
                  api,
                  organization.id,
                  addressInput(shipping),
                  addressInput(billing),
                );
                const returnBase = `${window.location.origin}/checkout/result?orderId=${orderId}`;
                const payment = await storefrontApi.paymentSession(
                  api,
                  organization.id,
                  orderId,
                  `${returnBase}&return=succeeded`,
                  `${returnBase}&return=cancelled`,
                );
                window.location.assign(payment.checkoutUrl);
              } catch (reason) {
                setError(
                  reason instanceof Error
                    ? reason.message
                    : "Please try again.",
                );
                setSubmitting(false);
              }
            }}
          >
            Continue to secure payment
          </Button>
        </Card>
      </div>
    </div>
  );
}

export function PaymentReturnPage() {
  const params = useSearchParams();
  const { organization } = useOrganization();
  const orderId = params.get("orderId") ?? "";
  const returnedAs = params.get("return");
  const order = useApiQuery(
    (api, signal) =>
      storefrontApi.order(api, organization!.id, orderId, signal),
    [organization?.id, orderId],
    Boolean(organization && orderId),
  );
  if (!orderId)
    return (
      <EmptyState
        title="Payment return is incomplete"
        description="No order was supplied. Open your order history to check payment status."
        action={<a href="/account/orders">View orders</a>}
      />
    );
  if (order.isLoading) return <ScreenLoading />;
  if (order.error)
    return <ScreenError error={order.error} retry={order.reload} />;
  const paid = order.data?.paidAt !== null && order.data?.paidAt !== undefined;
  const title = paid
    ? "Payment received"
    : returnedAs === "cancelled"
      ? "Payment cancelled"
      : "Payment pending";
  return (
    <div className="content-stack">
      <PageHeader
        title={title}
        description={`Order ${order.data?.orderNumber ?? orderId}`}
      />
      <Alert
        title={
          paid
            ? "Your order is paid"
            : "We are waiting for provider confirmation"
        }
        tone={
          paid ? "success" : returnedAs === "cancelled" ? "warning" : "info"
        }
      >
        {paid
          ? "You can follow fulfillment from order details."
          : "Refreshing this page is safe. The displayed status always comes from the order API, not the redirect URL."}
      </Alert>
      <div>
        <Button variant="secondary" onClick={order.reload}>
          Refresh status
        </Button>{" "}
        <a
          className="ds-button ds-button--primary"
          href={`/account/orders/${orderId}`}
        >
          View order
        </a>
      </div>
    </div>
  );
}
