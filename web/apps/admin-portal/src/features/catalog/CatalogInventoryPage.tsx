"use client";
import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
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
  useConfirmation,
} from "@modular-mlm/design-system";
import { useState, type FormEvent } from "react";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, money, useAdminScope } from "../shared/AdminState";

export function CatalogInventoryPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [lowStock, setLowStock] = useState(false);
  const [message, setMessage] = useState("");
  const [selectedProductId, setSelectedProductId] = useState<string | null>(
    null,
  );
  const [historyVariant, setHistoryVariant] = useState<{
    id: string;
    label: string;
  } | null>(null);
  const products = useApiQuery(
    (client, signal) => adminApi.products(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const inventory = useApiQuery(
    (client, signal) =>
      adminApi.inventory(
        client,
        scope.organizationId,
        page,
        search,
        lowStock,
        signal,
      ),
    [scope.organizationId, page, search, lowStock],
    scope.isReady,
  );
  if (products.isLoading || inventory.isLoading) return <Loading />;
  if (products.error)
    return <Failure error={products.error} retry={products.reload} />;
  const refresh = (value: string) => {
    setMessage(value);
    products.reload();
    inventory.reload();
  };
  return (
    <div className="content-stack">
      <PageHeader
        title="Catalog and inventory"
        description="Manage publication and stock using current backend versions."
      />
      {message && <Alert title={message} tone="info" />}
      <CategoryAdministration />
      <CreateProductForm
        onCreated={() => refresh("Product created as a draft.")}
      />
      <h2>Products</h2>
      {!products.data?.length ? (
        <EmptyState
          title="No products"
          description="Create the first product and variant above."
        />
      ) : (
        <DataTable
          caption="Products"
          rows={products.data}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "name",
              header: "Product",
              cell: (row) => (
                <>
                  <strong>{row.name}</strong>
                  <br />
                  <small>{row.categoryName}</small>
                </>
              ),
            },
            {
              key: "price",
              header: "Price",
              cell: (row) => money(row.price, scope.currency),
            },
            { key: "bv", header: "BV", cell: (row) => row.businessVolume },
            { key: "stock", header: "Stock", cell: (row) => row.stockQuantity },
            { key: "status", header: "Status", cell: (row) => row.status },
            {
              key: "actions",
              header: "Actions",
              cell: (row) => (
                <>
                  <Button
                    variant="secondary"
                    onClick={async () => {
                      await adminApi.publishProduct(
                        api,
                        scope.organizationId,
                        row.id,
                      );
                      refresh(`${row.name} published.`);
                    }}
                  >
                    Publish
                  </Button>{" "}
                  <Button
                    variant="ghost"
                    onClick={() => setSelectedProductId(row.id)}
                  >
                    Variants
                  </Button>{" "}
                  <Button
                    variant="danger"
                    onClick={async () => {
                      if (
                        !(await confirm({
                          title: "Archive product?",
                          description: `Archive ${row.name}? It will no longer be purchasable.`,
                          confirmLabel: "Archive product",
                        }))
                      )
                        return;
                      await adminApi.archiveProduct(
                        api,
                        scope.organizationId,
                        row.id,
                      );
                      refresh(`${row.name} archived.`);
                    }}
                  >
                    Archive
                  </Button>
                </>
              ),
            },
          ]}
        />
      )}
      {selectedProductId && (
        <ProductVariants
          productId={selectedProductId}
          onClose={() => setSelectedProductId(null)}
          onSaved={() => refresh("Product variants updated.")}
        />
      )}
      <CommissionProfileForm
        onSaved={() => setMessage("Commission profile created.")}
      />
      <div className="toolbar">
        <InputField
          id="inventory-search"
          label="Search inventory"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        <label>
          <input
            type="checkbox"
            checked={lowStock}
            onChange={(event) => setLowStock(event.target.checked)}
          />{" "}
          Low stock only
        </label>
      </div>
      {inventory.error ? (
        <Failure error={inventory.error} retry={inventory.reload} />
      ) : (
        <DataTable
          caption="Inventory"
          rows={inventory.data?.items ?? []}
          rowKey={(row) => row.productVariantId}
          columns={[
            {
              key: "product",
              header: "Product",
              cell: (row) => (
                <>
                  {row.productName}
                  <br />
                  <small>{row.sku}</small>
                </>
              ),
            },
            { key: "onHand", header: "On hand", cell: (row) => row.onHand },
            {
              key: "reserved",
              header: "Reserved",
              cell: (row) => row.reserved,
            },
            {
              key: "available",
              header: "Available",
              cell: (row) => row.available ?? "Not tracked",
            },
            { key: "version", header: "Version", cell: (row) => row.version },
            {
              key: "adjust",
              header: "",
              cell: (row) => (
                <div className="button-cluster">
                  <InventoryAdjustment
                    variantId={row.productVariantId}
                    version={row.version}
                    label={row.sku}
                    onSaved={() =>
                      refresh(`Inventory for ${row.sku} adjusted.`)
                    }
                  />
                  <Button
                    variant="ghost"
                    onClick={() =>
                      setHistoryVariant({
                        id: row.productVariantId,
                        label: row.sku,
                      })
                    }
                  >
                    History
                  </Button>
                </div>
              ),
            },
          ]}
        />
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={page === 1}
          onClick={() => setPage((v) => v - 1)}
        >
          Previous
        </Button>
        <span>Page {page}</span>
        <Button
          variant="secondary"
          disabled={
            !inventory.data?.hasNextPage &&
            (inventory.data?.items.length ?? 0) < 20
          }
          onClick={() => setPage((v) => v + 1)}
        >
          Next
        </Button>
      </div>
      {historyVariant && (
        <InventoryHistory
          variant={historyVariant}
          close={() => setHistoryVariant(null)}
        />
      )}
    </div>
  );
}

function CategoryAdministration() {
  const api = useApiClient();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  const { confirm, prompt } = useConfirmation();
  const categories = useApiQuery(
    (client, signal) => adminApi.categories(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  return (
    <Card>
      <h2>Categories</h2>
      <form
        className="inline-form"
        onSubmit={async (event) => {
          event.preventDefault();
          const form = event.currentTarget;
          const data = new FormData(form);
          const saved = await feedback.submit(
            () => adminApi.createCategory(
              api,
              scope.organizationId,
              String(data.get("name")),
              String(data.get("slug")),
            ),
            "Category created.",
          );
          if (saved) {
            form.reset();
            categories.reload();
          }
        }}
      >
        <FormErrorSummary errors={feedback.fieldErrors} generalErrors={feedback.formErrors} id={feedback.errorSummaryId} />
        <InputField name="name" label="Category name" required />
        <InputField name="slug" label="Slug" pattern="[a-z0-9]+(?:-[a-z0-9]+)*" required />
        <Button type="submit" isLoading={feedback.isSubmitting}>Create category</Button>
      </form>
      {categories.error ? <Failure error={categories.error} retry={categories.reload} /> : (
        <DataTable
          caption="Category administration"
          rows={categories.data?.items ?? []}
          rowKey={(row) => row.id}
          columns={[
            { key: "name", header: "Name", cell: (row) => row.name },
            { key: "slug", header: "Slug", cell: (row) => row.slug },
            { key: "products", header: "Products", cell: (row) => row.productCount },
            { key: "status", header: "Status", cell: (row) => row.isActive ? "Active" : "Archived" },
            { key: "actions", header: "", cell: (row) => (
              <div className="button-cluster">
                <Button variant="secondary" onClick={async () => {
                  const name = await prompt({ title: "Rename category", description: `Choose a new name for ${row.name}.`, label: "Category name", submitLabel: "Rename", maxLength: 200 });
                  if (!name) return;
                  await adminApi.renameCategory(api, scope.organizationId, row.id, name);
                  categories.reload();
                }}>Rename</Button>
                <Button variant={row.isActive ? "danger" : "secondary"} onClick={async () => {
                  const action = row.isActive ? "archive" : "activate";
                  if (!(await confirm({ title: `${action} category?`, description: `${action} ${row.name}?`, confirmLabel: action }))) return;
                  await adminApi.categoryAction(api, scope.organizationId, row.id, action);
                  categories.reload();
                }}>{row.isActive ? "Archive" : "Activate"}</Button>
              </div>
            )},
          ]}
        />
      )}
    </Card>
  );
}

function ProductVariants({
  productId,
  onClose,
  onSaved,
}: {
  productId: string;
  onClose(): void;
  onSaved(): void;
}) {
  const api = useApiClient();
  const { confirm, prompt } = useConfirmation();
  const variantFeedback = useFormSubmission();
  const scope = useAdminScope();
  const product = useApiQuery(
    (client, signal) =>
      adminApi.product(client, scope.organizationId, productId, signal),
    [scope.organizationId, productId],
    scope.isReady,
  );
  const categories = useApiQuery(
    (client, signal) =>
      adminApi.categories(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const profiles = useApiQuery(
    (client, signal) =>
      adminApi.commissionProfiles(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const productFeedback = useFormSubmission();
  const assignmentFeedback = useFormSubmission();
  if (product.isLoading || categories.isLoading || profiles.isLoading)
    return <Loading />;
  if (product.error)
    return <Failure error={product.error} retry={product.reload} />;
  return (
    <Card>
      <div className="action-row">
        <div>
          <h2>{product.data?.name} variants</h2>
          <p>Version values protect concurrent stock and lifecycle changes.</p>
        </div>
        <Button variant="ghost" onClick={onClose}>
          Close
        </Button>
      </div>
      {product.data && (
        <>
          <form
            className="inline-form"
            onSubmit={async (event) => {
              event.preventDefault();
              const data = new FormData(event.currentTarget);
              const saved = await productFeedback.submit(
                () =>
                  adminApi.updateProduct(api, scope.organizationId, productId, {
                    categoryId: String(data.get("categoryId")),
                    name: String(data.get("name")),
                    description: String(data.get("description")),
                  }),
                "Product updated.",
              );
              if (saved) {
                product.reload();
                onSaved();
              }
            }}
          >
            <FormErrorSummary
              errors={productFeedback.fieldErrors}
              generalErrors={productFeedback.formErrors}
              id={productFeedback.errorSummaryId}
            />
            <SelectField
              name="categoryId"
              label="Category"
              defaultValue={product.data.categoryId}
              required
            >
              {categories.data?.items.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </SelectField>
            <InputField
              name="name"
              label="Product name"
              defaultValue={product.data.name}
              required
            />
            <InputField
              name="description"
              label="Description"
              defaultValue={product.data.description}
              required
            />
            <Button type="submit" isLoading={productFeedback.isSubmitting}>
              Save product
            </Button>
          </form>
          <form
            className="inline-form"
            onSubmit={async (event) => {
              event.preventDefault();
              const value = String(
                new FormData(event.currentTarget).get("commissionProfileId") ??
                  "",
              );
              const saved = await assignmentFeedback.submit(
                () =>
                  adminApi.assignCommissionProfile(
                    api,
                    scope.organizationId,
                    productId,
                    value || null,
                  ),
                "Commission profile assigned.",
              );
              if (saved) {
                product.reload();
                onSaved();
              }
            }}
          >
            <FormErrorSummary
              errors={assignmentFeedback.fieldErrors}
              generalErrors={assignmentFeedback.formErrors}
              id={assignmentFeedback.errorSummaryId}
            />
            <SelectField
              name="commissionProfileId"
              label="Commission profile"
              defaultValue={product.data.commissionProfileId ?? ""}
            >
              <option value="">Use commission plan defaults</option>
              {profiles.data?.map((profile) => (
                <option key={profile.id} value={profile.id}>
                  {profile.name}
                </option>
              ))}
            </SelectField>
            <Button
              type="submit"
              variant="secondary"
              isLoading={assignmentFeedback.isSubmitting}
            >
              Assign profile
            </Button>
          </form>
        </>
      )}
      <DataTable
        caption="Product variants"
        rows={product.data?.variants ?? []}
        rowKey={(row) => row.id}
        columns={[
          { key: "sku", header: "SKU", cell: (row) => row.sku },
          {
            key: "price",
            header: "Price",
            cell: (row) => money(row.price, scope.currency),
          },
          { key: "bv", header: "BV", cell: (row) => row.businessVolume },
          {
            key: "stock",
            header: "Available",
            cell: (row) => row.available ?? "Not tracked",
          },
          { key: "status", header: "Status", cell: (row) => row.status },
          {
            key: "action",
            header: "",
            cell: (row) => (
              <div className="button-cluster">
                <VariantEditor
                  productId={productId}
                  variant={row}
                  saved={() => {
                    product.reload();
                    onSaved();
                  }}
                />
                <Button
                  variant="danger"
                  onClick={async () => {
                    const reason = await prompt({
                      title: "Reason for archiving variant",
                      description: `Explain why ${row.sku} is being archived. This is recorded in the audit trail.`,
                      label: "Archive reason",
                      submitLabel: "Continue",
                    });
                    if (!reason) return;
                    if (
                      !(await confirm({
                        title: "Archive product variant?",
                        description: `Archive variant ${row.sku}? It will no longer be available for new purchases.`,
                        confirmLabel: "Archive variant",
                      }))
                    )
                      return;
                    await adminApi.archiveVariant(
                      api,
                      scope.organizationId,
                      productId,
                      row.id,
                      reason,
                      row.version,
                    );
                    product.reload();
                    onSaved();
                  }}
                >
                  Archive
                </Button>
              </div>
            ),
          },
        ]}
      />
      <form
        className="inline-form"
        onSubmit={async (event) => {
          event.preventDefault();
          const form = event.currentTarget;
          const data = new FormData(form);
          const saved = await variantFeedback.submit(
            () =>
              adminApi.createVariant(api, scope.organizationId, productId, {
                sku: String(data.get("sku")),
                price: Number(data.get("price")),
                businessVolume: Number(data.get("businessVolume")),
                initialOnHandQuantity: Number(
                  data.get("initialOnHandQuantity"),
                ),
                stockKeepingEnabled: data.get("stockKeepingEnabled") === "on",
              }),
            "Product variant created.",
          );
          if (saved) {
            form.reset();
            product.reload();
            onSaved();
          }
        }}
      >
        <FormErrorSummary
          errors={variantFeedback.fieldErrors}
          generalErrors={variantFeedback.formErrors}
          id={variantFeedback.errorSummaryId}
        />
        <InputField
          name="sku"
          label="New SKU"
          required
          error={variantFeedback.fieldError("sku")}
        />
        <InputField
          name="price"
          label="Price"
          type="number"
          min={0}
          step="0.01"
          required
          error={variantFeedback.fieldError("price")}
        />
        <InputField
          name="businessVolume"
          label="BV"
          type="number"
          min={0}
          step="0.01"
          required
          error={variantFeedback.fieldError("businessVolume")}
        />
        <InputField
          name="initialOnHandQuantity"
          label="Initial stock"
          type="number"
          min={0}
          required
          error={variantFeedback.fieldError("initialOnHandQuantity")}
        />
        <label>
          <input name="stockKeepingEnabled" type="checkbox" defaultChecked />{" "}
          Track stock
        </label>
        <Button
          type="submit"
          isLoading={variantFeedback.isSubmitting}
          disabled={variantFeedback.isSubmitting}
        >
          Add variant
        </Button>
      </form>
    </Card>
  );
}

function VariantEditor({
  productId,
  variant,
  saved,
}: {
  productId: string;
  variant: import("@modular-mlm/contracts").AdminProductVariantDto;
  saved(): void;
}) {
  const api = useApiClient();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  return (
    <details>
      <summary>Edit</summary>
      <form
        className="form-grid compact-form"
        onSubmit={async (event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const completed = await feedback.submit(
            () =>
              adminApi.updateVariant(
                api,
                scope.organizationId,
                productId,
                variant.id,
                {
                  price: Number(data.get("price")),
                  businessVolume: Number(data.get("businessVolume")),
                  weight: Number(data.get("weight")) || null,
                  attributesJson: String(data.get("attributesJson")),
                  stockKeepingEnabled: data.get("stockKeepingEnabled") === "on",
                  expectedVersion: variant.version,
                },
              ),
            "Variant updated.",
          );
          if (completed) saved();
        }}
      >
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <InputField
          name="price"
          label="Price"
          type="number"
          min={0}
          step="0.01"
          defaultValue={variant.price}
          required
        />
        <InputField
          name="businessVolume"
          label="BV"
          type="number"
          min={0}
          step="0.01"
          defaultValue={variant.businessVolume}
          required
        />
        <InputField
          name="weight"
          label="Weight"
          type="number"
          min={0}
          step="0.01"
          defaultValue={variant.weight ?? ""}
        />
        <InputField
          name="attributesJson"
          label="Attributes JSON"
          defaultValue={variant.attributesJson}
          required
        />
        <label>
          <input
            name="stockKeepingEnabled"
            type="checkbox"
            defaultChecked={variant.stockKeepingEnabled}
          />{" "}
          Track stock
        </label>
        <Button type="submit" isLoading={feedback.isSubmitting}>
          Save variant
        </Button>
      </form>
    </details>
  );
}

function InventoryHistory({
  variant,
  close,
}: {
  variant: { id: string; label: string };
  close(): void;
}) {
  const scope = useAdminScope();
  const [page, setPage] = useState(1);
  const history = useApiQuery(
    (client, signal) =>
      adminApi.inventoryHistory(
        client,
        scope.organizationId,
        variant.id,
        page,
        signal,
      ),
    [scope.organizationId, variant.id, page],
    scope.isReady,
  );
  if (history.isLoading) return <Loading />;
  if (history.error)
    return <Failure error={history.error} retry={history.reload} />;
  return (
    <Card>
      <div className="action-row">
        <div>
          <h2>Inventory history: {variant.label}</h2>
          <p>Immutable stock changes and balances.</p>
        </div>
        <Button variant="ghost" onClick={close}>
          Close
        </Button>
      </div>
      {!history.data?.items.length ? (
        <EmptyState
          title="No adjustments"
          description="No inventory adjustments have been recorded for this variant."
        />
      ) : (
        <DataTable
          caption="Inventory adjustments"
          rows={history.data.items}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "date",
              header: "Occurred",
              cell: (row) => new Date(row.occurredAt).toLocaleString(),
            },
            { key: "type", header: "Type", cell: (row) => row.adjustmentType },
            {
              key: "delta",
              header: "Change",
              cell: (row) => row.quantityDelta,
            },
            {
              key: "balance",
              header: "Balance",
              cell: (row) => `${row.balanceBefore} → ${row.balanceAfter}`,
            },
            { key: "reason", header: "Reason", cell: (row) => row.reason },
          ]}
        />
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={page === 1}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>
          Page {page} of {history.data?.totalPages ?? 1}
        </span>
        <Button
          variant="secondary"
          disabled={page >= (history.data?.totalPages ?? 1)}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
    </Card>
  );
}

function CommissionProfileForm({ onSaved }: { onSaved(): void }) {
  const api = useApiClient();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  return (
    <Card>
      <details>
        <summary>
          <strong>Create commission profile</strong>
        </summary>
        <form
          className="form-grid"
          onSubmit={async (event) => {
            event.preventDefault();
            const form = event.currentTarget;
            const data = new FormData(event.currentTarget);
            const saved = await feedback.submit(
              () =>
                adminApi.createCommissionProfile(api, scope.organizationId, {
                  name: String(data.get("name")),
                  directSalesEligible: data.get("directSalesEligible") === "on",
                  directSalesRateOverride:
                    Number(data.get("directSalesRateOverride")) || null,
                  binaryVolumeEligible:
                    data.get("binaryVolumeEligible") === "on",
                  binaryVolumeOverride:
                    Number(data.get("binaryVolumeOverride")) || null,
                  effectiveFrom: new Date(
                    String(data.get("effectiveFrom")),
                  ).toISOString(),
                }),
              "Commission profile created.",
            );
            if (saved) {
              form.reset();
              onSaved();
            }
          }}
        >
          <FormErrorSummary
            errors={feedback.fieldErrors}
            generalErrors={feedback.formErrors}
            id={feedback.errorSummaryId}
          />
          <InputField
            name="name"
            label="Profile name"
            required
            error={feedback.fieldError("name")}
          />
          <label>
            <input name="directSalesEligible" type="checkbox" defaultChecked />{" "}
            Direct-sales eligible
          </label>
          <InputField
            name="directSalesRateOverride"
            label="Direct-sales rate override"
            type="number"
            min={0}
            step="0.01"
            error={feedback.fieldError("directSalesRateOverride")}
          />
          <label>
            <input name="binaryVolumeEligible" type="checkbox" defaultChecked />{" "}
            Binary-volume eligible
          </label>
          <InputField
            name="binaryVolumeOverride"
            label="Binary-volume override"
            type="number"
            min={0}
            step="0.01"
            error={feedback.fieldError("binaryVolumeOverride")}
          />
          <InputField
            name="effectiveFrom"
            label="Effective from"
            type="datetime-local"
            required
            error={feedback.fieldError("effectiveFrom")}
          />
          <Button
            type="submit"
            isLoading={feedback.isSubmitting}
            disabled={feedback.isSubmitting}
          >
            Create profile
          </Button>
        </form>
      </details>
    </Card>
  );
}

function CreateProductForm({ onCreated }: { onCreated(): void }) {
  const api = useApiClient();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  const categories = useApiQuery(
    (client, signal) =>
      adminApi.categories(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const saved = await feedback.submit(
      () =>
        adminApi.createProduct(api, scope.organizationId, {
          categoryId: String(data.get("categoryId")),
          name: String(data.get("name")),
          slug: String(data.get("slug")),
          description: String(data.get("description")),
          sku: String(data.get("sku")),
          price: Number(data.get("price")),
          businessVolume: Number(data.get("businessVolume")),
          stockQuantity: Number(data.get("stockQuantity")),
        }),
      "Draft product created.",
    );
    if (saved) {
      form.reset();
      onCreated();
    }
  }
  return (
    <Card>
      <details>
        <summary>
          <strong>Create product</strong>
        </summary>
        <form className="form-grid" onSubmit={submit}>
          <FormErrorSummary
            errors={feedback.fieldErrors}
            generalErrors={feedback.formErrors}
            id={feedback.errorSummaryId}
          />
          <SelectField
            name="categoryId"
            label="Category"
            required
            error={feedback.fieldError("categoryId")}
          >
            <option value="">Select category</option>
            {categories.data?.items.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </SelectField>
          <InputField
            name="name"
            label="Name"
            required
            error={feedback.fieldError("name")}
          />
          <InputField
            name="slug"
            label="Slug"
            required
            pattern="[a-z0-9-]+"
            hint="Lowercase letters, numbers, and hyphens only."
            error={feedback.fieldError("slug")}
          />
          <InputField
            name="description"
            label="Description"
            required
            error={feedback.fieldError("description")}
          />
          <InputField
            name="sku"
            label="Initial SKU"
            required
            error={feedback.fieldError("sku")}
          />
          <InputField
            name="price"
            label="Price"
            type="number"
            min={0}
            step="0.01"
            required
            error={feedback.fieldError("price")}
          />
          <InputField
            name="businessVolume"
            label="Business volume"
            type="number"
            min={0}
            step="0.01"
            required
            error={feedback.fieldError("businessVolume")}
          />
          <InputField
            name="stockQuantity"
            label="Initial stock"
            type="number"
            min={0}
            required
            error={feedback.fieldError("stockQuantity")}
          />
          <Button
            type="submit"
            isLoading={feedback.isSubmitting}
            disabled={feedback.isSubmitting}
          >
            Create draft product
          </Button>
        </form>
      </details>
    </Card>
  );
}
function InventoryAdjustment({
  variantId,
  version,
  label,
  onSaved,
}: {
  variantId: string;
  version: number;
  label: string;
  onSaved(): void;
}) {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  return (
    <details>
      <summary>Adjust</summary>
      <form
        className="form-grid compact-form"
        onSubmit={async (event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          if (
            !(await confirm({
              title: "Adjust inventory?",
              description: `Adjust inventory for ${label} by ${data.get("quantityDelta")}? This creates an immutable inventory record.`,
              confirmLabel: "Adjust inventory",
            }))
          )
            return;
          const saved = await feedback.submit(
            () =>
              adminApi.adjustInventory(
                api,
                scope.organizationId,
                variantId,
                {
                  quantityDelta: Number(data.get("quantityDelta")),
                  adjustmentType: Number(data.get("adjustmentType")),
                  reason: String(data.get("reason")),
                  expectedVersion: version,
                },
                crypto.randomUUID(),
              ),
            "Inventory adjusted.",
          );
          if (saved) onSaved();
        }}
      >
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <InputField
          name="quantityDelta"
          label="Quantity change"
          type="number"
          required
          error={feedback.fieldError("quantityDelta")}
        />
        <InputField
          name="adjustmentType"
          label="Adjustment type"
          type="number"
          min={0}
          required
          defaultValue={0}
          error={feedback.fieldError("adjustmentType")}
        />
        <InputField
          name="reason"
          label="Reason"
          required
          error={feedback.fieldError("reason")}
        />
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Confirm adjustment
        </Button>
      </form>
    </details>
  );
}
