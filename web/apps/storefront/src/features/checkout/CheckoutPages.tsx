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
import Link from "next/link";
import { useState } from "react";
import { addressInput, storefrontApi } from "../api/storefrontApi";
import { ScreenError, ScreenLoading, money } from "../shared/ScreenState";
import { CheckoutSteps } from "../shared/StorefrontPrimitives";

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
      <div className="content-stack storefront-checkout-page">
        <PageHeader title="Checkout" />
        <CheckoutSteps current={1} />
        <EmptyState
          title="Sign in to checkout"
          description="Sign in to continue with the items already in your cart."
          action={
            <Link
              className="ds-button ds-button--primary"
              href="/sign-in?returnTo=%2Fcheckout"
            >
              Sign in
            </Link>
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
    <div className="content-stack storefront-checkout-page">
      <PageHeader
        eyebrow="Secure checkout"
        title="Checkout"
        description="Choose your delivery details and review your order."
      />
      <CheckoutSteps current={1} />
      <div className="storefront-checkout-layout">
        <Card className="storefront-checkout-panel">
          <div className="storefront-numbered-heading">
            <span>1</span>
            <div>
              <h2>Delivery details</h2>
              <p>Choose where this order should be delivered and billed.</p>
            </div>
          </div>
          {!addresses.data?.length ? (
            <EmptyState
              title="Add an address first"
              description="Checkout uses a complete saved address snapshot."
              action={<Link href="/account/addresses">Manage addresses</Link>}
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
        <Card className="storefront-order-summary storefront-checkout-summary">
          <h2>Your order</h2>
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
          <p className="storefront-secure-note">
            You will continue to a secure payment page. Your card or wallet
            details are not collected here.
          </p>
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
        action={<Link href="/account/orders">View orders</Link>}
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
    <div className="content-stack storefront-payment-result">
      <CheckoutSteps current={3} />
      <PageHeader
        title={title}
        description={`Order ${order.data?.orderNumber ?? orderId}`}
      />
      <Alert
        title={paid ? "Your order is paid" : "We are confirming your payment"}
        tone={
          paid ? "success" : returnedAs === "cancelled" ? "warning" : "info"
        }
      >
        {paid
          ? "You can follow delivery progress from your order details."
          : "Refreshing this page is safe. The latest order status will appear here."}
      </Alert>
      <div>
        <Button variant="secondary" onClick={order.reload}>
          Refresh status
        </Button>{" "}
        <Link
          className="ds-button ds-button--primary"
          href={`/account/orders/${orderId}`}
        >
          View order
        </Link>
      </div>
    </div>
  );
}
