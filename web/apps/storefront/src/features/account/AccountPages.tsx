"use client";

import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import type {
  CustomerAddressDto,
  CustomerAddressRequest,
} from "@modular-mlm/contracts";
import { OrderStatus } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormErrorSummary,
  InputField,
  PageHeader,
  SelectField,
  TextareaField,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";
import { useState, type FormEvent } from "react";
import { useAuthentication } from "@modular-mlm/auth";
import { SessionManagementPage } from "@modular-mlm/auth";
import Link from "next/link";
import { storefrontApi } from "../api/storefrontApi";
import { ScreenError, ScreenLoading, date, money } from "../shared/ScreenState";
import {
  FulfillmentStatusBadge,
  OrderStatusBadge,
  PaymentStatusBadge,
} from "../shared/StorefrontPrimitives";

export function AccountHomePage() {
  return (
    <div className="content-stack">
      <PageHeader
        title="My account"
        description="Manage your profile, delivery addresses, and orders."
      />
      <div className="card-grid">
        <Card>
          <h2>
            <Link href="/account/profile">Profile</Link>
          </h2>
          <p>Review the name shown on your account.</p>
        </Card>
        <Card>
          <h2>
            <Link href="/account/addresses">Addresses</Link>
          </h2>
          <p>Manage checkout delivery and billing addresses.</p>
        </Card>
        <Card>
          <h2>
            <Link href="/account/orders">Orders</Link>
          </h2>
          <p>Track delivery, cancellations, and available refunds.</p>
        </Card>
        <Card>
          <h2>
            <Link href="/account/security">Sign-in sessions</Link>
          </h2>
          <p>Review and revoke browsers signed in to your account.</p>
        </Card>
      </div>
    </div>
  );
}

export function AccountSecurityPage() {
  const api = useApiClient();
  const authentication = useAuthentication();
  const { confirm } = useConfirmation();
  const info = useApiQuery(
    (client, signal) => storefrontApi.identityInfo(client, signal),
    [],
    true,
  );
  const accountFeedback = useFormSubmission();
  const mfaFeedback = useFormSubmission();
  const [twoFactor, setTwoFactor] = useState<
    import("@modular-mlm/contracts").TwoFactorResponse | null
  >(null);
  if (info.isLoading) return <ScreenLoading />;
  if (info.error) return <ScreenError error={info.error} retry={info.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title="Account security"
        description="Update sign-in credentials, multi-factor authentication, and active sessions."
      />
      <Card>
        <h2>Email and password</h2>
        <form
          className="form-grid"
          onSubmit={async (event) => {
            event.preventDefault();
            const form = event.currentTarget;
            const data = new FormData(form);
            const saved = await accountFeedback.submit(
              () =>
                storefrontApi.updateIdentityInfo(api, {
                  newEmail: String(data.get("newEmail") ?? "").trim() || null,
                  newPassword: String(data.get("newPassword") ?? "") || null,
                  oldPassword: String(data.get("oldPassword") ?? "") || null,
                }),
              "Sign-in information updated.",
            );
            if (saved) {
              form.reset();
              info.reload();
              await authentication.refresh();
            }
          }}
        >
          <FormErrorSummary
            errors={accountFeedback.fieldErrors}
            generalErrors={accountFeedback.formErrors}
            id={accountFeedback.errorSummaryId}
          />
          <InputField
            name="newEmail"
            label="Email address"
            type="email"
            defaultValue={info.data?.email}
            hint={
              info.data?.isEmailConfirmed ? "Confirmed" : "Confirmation pending"
            }
          />
          <InputField
            name="oldPassword"
            label="Current password"
            type="password"
            autoComplete="current-password"
          />
          <InputField
            name="newPassword"
            label="New password"
            type="password"
            autoComplete="new-password"
          />
          <Button type="submit" isLoading={accountFeedback.isSubmitting}>
            Update sign-in information
          </Button>
        </form>
      </Card>
      <Card>
        <h2>Authenticator app</h2>
        <p>
          {authentication.user?.mfaEnabled
            ? "Multi-factor authentication is enabled."
            : "Add an authenticator app for stronger sign-in security."}
        </p>
        <FormErrorSummary
          errors={mfaFeedback.fieldErrors}
          generalErrors={mfaFeedback.formErrors}
          id={mfaFeedback.errorSummaryId}
        />
        {!twoFactor?.sharedKey && !authentication.user?.mfaEnabled && (
          <Button
            variant="secondary"
            onClick={() =>
              void mfaFeedback.submit(async () => {
                const result = await storefrontApi.updateTwoFactor(api, {
                  resetSharedKey: true,
                });
                setTwoFactor(result);
              }, "Authenticator setup started.")
            }
          >
            Set up authenticator
          </Button>
        )}
        {twoFactor?.sharedKey && !twoFactor.isTwoFactorEnabled && (
          <form
            className="form-grid"
            onSubmit={async (event) => {
              event.preventDefault();
              const code = String(
                new FormData(event.currentTarget).get("code") ?? "",
              );
              const saved = await mfaFeedback.submit(async () => {
                const result = await storefrontApi.updateTwoFactor(api, {
                  enable: true,
                  twoFactorCode: code,
                });
                setTwoFactor(result);
              }, "Multi-factor authentication enabled.");
              if (saved) await authentication.refresh();
            }}
          >
            <p>
              Enter this key in your authenticator app:{" "}
              <code>{twoFactor.sharedKey}</code>
            </p>
            <InputField
              name="code"
              label="Authenticator code"
              inputMode="numeric"
              autoComplete="one-time-code"
              required
            />
            <Button type="submit" isLoading={mfaFeedback.isSubmitting}>
              Verify and enable
            </Button>
          </form>
        )}
        {twoFactor?.recoveryCodes?.length ? (
          <Alert title="Save these recovery codes" tone="warning">
            <code>{twoFactor.recoveryCodes.join(" · ")}</code>
          </Alert>
        ) : null}
        {authentication.user?.mfaEnabled && (
          <Button
            variant="danger"
            onClick={async () => {
              if (
                !(await confirm({
                  title: "Disable multi-factor authentication?",
                  description:
                    "Your account will rely on its password until MFA is configured again.",
                  confirmLabel: "Disable MFA",
                }))
              )
                return;
              const saved = await mfaFeedback.submit(
                () => storefrontApi.updateTwoFactor(api, { enable: false }),
                "Multi-factor authentication disabled.",
              );
              if (saved) {
                setTwoFactor(null);
                await authentication.refresh();
              }
            }}
          >
            Disable MFA
          </Button>
        )}
      </Card>
      <SessionManagementPage signInPath="/sign-in" showHeader={false} />
    </div>
  );
}

export function ProfilePage() {
  const api = useApiClient();
  const { organization } = useOrganization();
  const profile = useApiQuery(
    (client, signal) => storefrontApi.profile(client, organization!.id, signal),
    [organization?.id],
    Boolean(organization),
  );
  const [displayName, setDisplayName] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  if (profile.isLoading) return <ScreenLoading />;
  if (profile.error)
    return <ScreenError error={profile.error} retry={profile.reload} />;
  if (!profile.data)
    return (
      <EmptyState
        title="Profile unavailable"
        description="Your profile could not be loaded for this shop."
      />
    );
  const value = displayName || profile.data?.displayName || "";
  return (
    <div className="content-stack">
      <PageHeader
        title="Profile"
        description="Update the name shown on your account."
      />
      <Card className="form-card">
        <InputField
          id="display-name"
          label="Display name"
          required
          value={value}
          onChange={(event) => setDisplayName(event.target.value)}
        />
        {message && <p role="status">{message}</p>}
        <Button
          isLoading={saving}
          onClick={async () => {
            if (!organization || !value.trim()) return;
            setSaving(true);
            try {
              await storefrontApi.updateProfile(
                api,
                organization.id,
                value.trim(),
              );
              setMessage("Profile saved.");
              profile.reload();
            } catch (error) {
              setMessage(
                error instanceof Error
                  ? error.message
                  : "Could not save profile.",
              );
            } finally {
              setSaving(false);
            }
          }}
        >
          Save profile
        </Button>
      </Card>
    </div>
  );
}

const emptyAddress: CustomerAddressRequest = {
  label: "Home",
  recipientName: "",
  phoneNumber: "",
  addressLine1: "",
  addressLine2: null,
  barangay: null,
  cityOrMunicipality: "",
  province: "",
  postalCode: "",
  countryCode: "PH",
  makeDefault: false,
};
export function AddressesPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const { organization } = useOrganization();
  const addresses = useApiQuery(
    (client, signal) =>
      storefrontApi.addresses(client, organization!.id, signal),
    [organization?.id],
    Boolean(organization),
  );
  const [editing, setEditing] = useState<CustomerAddressDto | null>(null);
  const [message, setMessage] = useState("");
  if (addresses.isLoading) return <ScreenLoading />;
  if (addresses.error)
    return <ScreenError error={addresses.error} retry={addresses.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title="Addresses"
        description="Manage the addresses saved to your account."
      />
      {message && <Alert title={message} tone="success" />}
      {addresses.data?.length ? (
        <div className="card-grid">
          {addresses.data.map((address) => (
            <Card key={address.id}>
              <h2>
                {address.label} {address.isDefault && <small>Default</small>}
              </h2>
              <address>
                {address.recipientName}
                <br />
                {address.addressLine1}
                <br />
                {address.barangay && (
                  <>
                    {address.barangay}
                    <br />
                  </>
                )}
                {address.cityOrMunicipality}, {address.province}{" "}
                {address.postalCode}
                <br />
                {address.countryCode}
              </address>
              <div>
                <Button variant="secondary" onClick={() => setEditing(address)}>
                  Edit
                </Button>{" "}
                <Button
                  variant="danger"
                  onClick={async () => {
                    if (!organization) return;
                    if (
                      !(await confirm({
                        title: "Remove address?",
                        description: `Remove ${address.label} from your saved addresses? This cannot be undone.`,
                        confirmLabel: "Remove address",
                      }))
                    )
                      return;
                    await storefrontApi.removeAddress(
                      api,
                      organization.id,
                      address.id,
                    );
                    setMessage("Address removed.");
                    addresses.reload();
                  }}
                >
                  Remove
                </Button>
              </div>
            </Card>
          ))}
        </div>
      ) : (
        <EmptyState
          title="No saved addresses"
          description="Add an address before checkout."
        />
      )}
      <AddressForm
        address={editing}
        onCancel={() => setEditing(null)}
        onSaved={() => {
          setEditing(null);
          setMessage("Address saved.");
          addresses.reload();
        }}
      />
    </div>
  );
}

function AddressForm({
  address,
  onCancel,
  onSaved,
}: {
  address: CustomerAddressDto | null;
  onCancel(): void;
  onSaved(): void;
}) {
  const api = useApiClient();
  const { organization } = useOrganization();
  const feedback = useFormSubmission();
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!organization) return;
    const form = event.currentTarget;
    const data = new FormData(form);
    const request: CustomerAddressRequest = {
      ...emptyAddress,
      label: String(data.get("label")),
      recipientName: String(data.get("recipientName")),
      phoneNumber: String(data.get("phoneNumber")),
      addressLine1: String(data.get("addressLine1")),
      addressLine2: String(data.get("addressLine2")) || null,
      barangay: String(data.get("barangay")) || null,
      cityOrMunicipality: String(data.get("cityOrMunicipality")),
      province: String(data.get("province")),
      postalCode: String(data.get("postalCode")),
      countryCode: String(data.get("countryCode")),
      makeDefault: data.get("makeDefault") === "on",
    };
    const saved = await feedback.submit(
      async () => {
        if (address)
          await storefrontApi.updateAddress(
            api,
            organization.id,
            address.id,
            request,
          );
        else await storefrontApi.addAddress(api, organization.id, request);
      },
      address ? "Address updated." : "Address added.",
    );
    if (saved) {
      onSaved();
      form.reset();
    }
  }
  return (
    <Card className="form-card">
      <h2>{address ? `Edit ${address.label}` : "Add an address"}</h2>
      <form onSubmit={submit} className="form-grid">
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <InputField
          name="label"
          label="Label"
          required
          defaultValue={address?.label ?? "Home"}
          error={feedback.fieldError("label")}
        />
        <InputField
          name="recipientName"
          label="Recipient"
          required
          defaultValue={address?.recipientName}
          error={feedback.fieldError("recipientName")}
        />
        <InputField
          name="phoneNumber"
          label="Phone"
          required
          defaultValue={address?.phoneNumber}
          error={feedback.fieldError("phoneNumber")}
        />
        <InputField
          name="addressLine1"
          label="Address line 1"
          required
          defaultValue={address?.addressLine1}
          error={feedback.fieldError("addressLine1")}
        />
        <InputField
          name="addressLine2"
          label="Address line 2"
          defaultValue={address?.addressLine2 ?? ""}
          error={feedback.fieldError("addressLine2")}
        />
        <InputField
          name="barangay"
          label="Barangay"
          defaultValue={address?.barangay ?? ""}
          error={feedback.fieldError("barangay")}
        />
        <InputField
          name="cityOrMunicipality"
          label="City or municipality"
          required
          defaultValue={address?.cityOrMunicipality}
          error={feedback.fieldError("cityOrMunicipality")}
        />
        <InputField
          name="province"
          label="Province"
          required
          defaultValue={address?.province}
          error={feedback.fieldError("province")}
        />
        <InputField
          name="postalCode"
          label="Postal code"
          required
          defaultValue={address?.postalCode}
          error={feedback.fieldError("postalCode")}
        />
        <InputField
          name="countryCode"
          label="Country code"
          required
          maxLength={2}
          defaultValue={address?.countryCode ?? "PH"}
          error={feedback.fieldError("countryCode")}
        />
        <label>
          <input
            name="makeDefault"
            type="checkbox"
            defaultChecked={address?.isDefault}
          />{" "}
          Make default
        </label>
        <div>
          <Button
            type="submit"
            isLoading={feedback.isSubmitting}
            disabled={feedback.isSubmitting}
          >
            Save address
          </Button>
          {address && (
            <Button type="button" variant="ghost" onClick={onCancel}>
              Cancel
            </Button>
          )}
        </div>
      </form>
    </Card>
  );
}

export function OrdersPage() {
  const { organization } = useOrganization();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const orders = useApiQuery(
    (api, signal) =>
      storefrontApi.orders(
        api,
        organization!.id,
        page,
        status === "" ? undefined : Number(status),
        signal,
      ),
    [organization?.id, page, status],
    Boolean(organization),
  );
  if (orders.isLoading) return <ScreenLoading />;
  if (orders.error)
    return <ScreenError error={orders.error} retry={orders.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title="Orders"
        description="Review purchases and follow their progress."
      />
      <div className="storefront-order-filters">
        <SelectField
          id="order-status"
          label="Filter by status"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value);
            setPage(1);
          }}
        >
          <option value="">All orders</option>
          <option value={OrderStatus.pendingPayment}>Awaiting payment</option>
          <option value={OrderStatus.paid}>Paid</option>
          <option value={OrderStatus.processing}>Processing</option>
          <option value={OrderStatus.shipped}>Shipped</option>
          <option value={OrderStatus.delivered}>Delivered</option>
          <option value={OrderStatus.cancelled}>Cancelled</option>
          <option value={OrderStatus.partiallyRefunded}>
            Partially refunded
          </option>
          <option value={OrderStatus.refunded}>Refunded</option>
        </SelectField>
      </div>
      {!orders.data?.items.length ? (
        <EmptyState
          title="No orders yet"
          description="Completed checkouts will appear here."
        />
      ) : (
        <DataTable
          caption="Order history"
          rows={orders.data.items}
          rowKey={(item) => item.id}
          columns={[
            {
              key: "number",
              header: "Order",
              cell: (item) => (
                <Link href={`/account/orders/${item.id}`}>
                  {item.orderNumber}
                </Link>
              ),
            },
            {
              key: "date",
              header: "Placed",
              cell: (item) => date(item.createdAt),
            },
            { key: "items", header: "Items", cell: (item) => item.itemCount },
            {
              key: "status",
              header: "Status",
              cell: (item) => (
                <div className="storefront-status-cluster">
                  <OrderStatusBadge value={item.status} />
                  <PaymentStatusBadge value={item.paymentStatus} />
                </div>
              ),
            },
            {
              key: "total",
              header: "Total",
              align: "end",
              cell: (item) => money(item.grandTotal, item.currency),
            },
          ]}
        />
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={!orders.data?.hasPreviousPage}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>Page {orders.data?.page ?? page}</span>
        <Button
          variant="secondary"
          disabled={!orders.data?.hasNextPage}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}

export function OrderDetailsPage({ orderId }: { orderId: string }) {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const { organization } = useOrganization();
  const order = useApiQuery(
    (client, signal) =>
      storefrontApi.order(client, organization!.id, orderId, signal),
    [organization?.id, orderId],
    Boolean(organization),
  );
  const [reason, setReason] = useState("");
  const [message, setMessage] = useState("");
  if (order.isLoading) return <ScreenLoading />;
  if (order.error)
    return <ScreenError error={order.error} retry={order.reload} />;
  if (!order.data)
    return (
      <EmptyState
        title="Order unavailable"
        description="This order could not be found in your account."
        action={<a href="/account/orders">Return to order history</a>}
      />
    );
  return (
    <div className="content-stack">
      <PageHeader
        title={`Order ${order.data.orderNumber}`}
        description={`Placed ${date(order.data.createdAt)}`}
      />
      {message && <Alert title={message} tone="info" />}
      <Card>
        <div className="storefront-status-cluster">
          <OrderStatusBadge value={order.data.status} />
          <PaymentStatusBadge value={order.data.paymentStatus} />
        </div>
        <div className="summary-line">
          <span>Total</span>
          <strong>{money(order.data.grandTotal, order.data.currency)}</strong>
        </div>
        <p>
          Paid: {date(order.data.paidAt)} · Delivered:{" "}
          {date(order.data.deliveredAt)}
        </p>
      </Card>
      {order.data.items.map((item) => (
        <Card key={item.id}>
          <h2>{item.productName}</h2>
          <p>
            {item.sku} · Quantity {item.quantity}
          </p>
          <FulfillmentStatusBadge value={item.fulfillmentStatus} />
          <p>{money(item.lineTotal, order.data!.currency)}</p>
          {item.canRequestRefund ? (
            <details>
              <summary>Request item refund</summary>
              <RefundForm
                max={item.refundableQuantity}
                submit={async (quantity, refundReason) => {
                  await storefrontApi.refundItem(
                    api,
                    organization!.id,
                    orderId,
                    item.id,
                    quantity,
                    refundReason,
                  );
                  setMessage("Refund requested.");
                  order.reload();
                }}
              />
            </details>
          ) : (
            item.refundFailureReason && <p>{item.refundFailureReason}</p>
          )}
        </Card>
      ))}
      {order.data.canRequestCancellation ? (
        <Card>
          <h2>Cancel order</h2>
          <TextareaField
            id="cancellation-reason"
            label="Reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
          <Button
            variant="danger"
            disabled={!reason.trim()}
            onClick={async () => {
              if (
                !(await confirm({
                  title: "Request order cancellation?",
                  description: `Request cancellation of order ${order.data!.orderNumber}? We will confirm whether it can still be cancelled.`,
                  confirmLabel: "Request cancellation",
                }))
              )
                return;
              await storefrontApi.cancelOrder(
                api,
                organization!.id,
                orderId,
                reason,
              );
              setMessage("Cancellation requested.");
              order.reload();
            }}
          >
            Request cancellation
          </Button>
        </Card>
      ) : (
        order.data.cancellationFailureReason && (
          <Alert title="Cancellation unavailable" tone="warning">
            {order.data.cancellationFailureReason}
          </Alert>
        )
      )}
    </div>
  );
}

function RefundForm({
  max,
  submit,
}: {
  max: number;
  submit(quantity: number, reason: string): Promise<void>;
}) {
  const [quantity, setQuantity] = useState(1);
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  return (
    <div className="form-grid">
      <InputField
        id={`refund-quantity-${max}`}
        label="Quantity"
        type="number"
        min={1}
        max={max}
        value={quantity}
        onChange={(event) => setQuantity(event.target.valueAsNumber)}
      />
      <TextareaField
        id={`refund-reason-${max}`}
        label="Reason"
        value={reason}
        onChange={(event) => setReason(event.target.value)}
      />
      <Button
        isLoading={busy}
        disabled={!reason.trim() || quantity < 1 || quantity > max}
        onClick={async () => {
          setBusy(true);
          try {
            await submit(quantity, reason);
          } finally {
            setBusy(false);
          }
        }}
      >
        Submit refund request
      </Button>
    </div>
  );
}
