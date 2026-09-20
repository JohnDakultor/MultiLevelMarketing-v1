"use client";

import { useApiClient, useApiQuery } from "@modular-mlm/api-client";
import {
  Button,
  Card,
  EmptyState,
  InputField,
  PageHeader,
  SelectField,
  ToastRegion,
} from "@modular-mlm/design-system";
import type { CategoryDto, ProductDto } from "@modular-mlm/contracts";
import { useOrganization } from "@modular-mlm/organization-context";
import Image from "next/image";
import { useEffect, useMemo, useState } from "react";
import { storefrontApi } from "../api/storefrontApi";
import { ScreenError, ScreenLoading, money } from "../shared/ScreenState";

export function CatalogPage({
  initialProducts,
  initialCategories,
}: {
  initialProducts?: ProductDto[];
  initialCategories?: CategoryDto[];
} = {}) {
  const { organization } = useOrganization();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const products = useApiQuery(
    (api, signal) =>
      storefrontApi.products(api, organization!.id, page, signal),
    [organization?.id, page],
    Boolean(organization),
    page === 1 ? initialProducts : undefined,
  );
  const categories = useApiQuery(
    (api, signal) => storefrontApi.categories(api, organization!.id, signal),
    [organization?.id],
    Boolean(organization),
    initialCategories,
  );
  const shown = useMemo(
    () =>
      products.data?.filter((product) =>
        product.name
          .toLocaleLowerCase()
          .includes(search.trim().toLocaleLowerCase()),
      ) ?? [],
    [products.data, search],
  );

  if (products.isLoading || categories.isLoading) return <ScreenLoading />;
  if (products.error)
    return <ScreenError error={products.error} retry={products.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Marketplace"
        title={organization?.storeTitle ?? "Products"}
        description="Current prices and business volume come directly from the marketplace."
      />
      <div className="toolbar">
        <InputField
          id="catalog-search"
          label="Search this page"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Product name"
        />
        <SelectField
          id="category-reference"
          label="Browse categories"
          defaultValue=""
        >
          <option value="">All categories</option>
          {categories.data?.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </SelectField>
      </div>
      <p className="supporting-copy">
        The API currently supports catalog paging, but not server-side search or
        category filtering. Search applies only to this loaded page.
      </p>
      {shown.length === 0 ? (
        <EmptyState
          title="No products on this page"
          description="Try another search or return when the catalog has published products."
        />
      ) : (
        <div className="product-grid">
          {shown.map((product) => (
            <Card key={product.id} className="product-card">
              {product.imageUrl ? (
                <Image
                  src={product.imageUrl}
                  alt=""
                  width={360}
                  height={240}
                  unoptimized
                />
              ) : (
                <div className="product-image-placeholder" aria-hidden />
              )}
              <div>
                <h2>
                  <a href={`/products/${product.slug}`}>{product.name}</a>
                </h2>
                <p>
                  {money(product.price, organization?.currencyCode ?? "PHP")}
                </p>
                <small>{product.businessVolume} BV</small>
              </div>
            </Card>
          ))}
        </div>
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={page === 1}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>Page {page}</span>
        <Button
          variant="secondary"
          disabled={(products.data?.length ?? 0) < 24}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}

export function ProductPage({ slug }: { slug: string }) {
  const api = useApiClient();
  const { organization } = useOrganization();
  const [variantId, setVariantId] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [message, setMessage] = useState("");
  const [isAdding, setAdding] = useState(false);
  const product = useApiQuery(
    (client, signal) =>
      storefrontApi.product(client, organization!.id, slug, signal),
    [organization?.id, slug],
    Boolean(organization),
  );
  if (product.isLoading) return <ScreenLoading />;
  if (product.error)
    return <ScreenError error={product.error} retry={product.reload} />;
  if (!product.data)
    return (
      <EmptyState
        title="Product unavailable"
        description="This product may have been unpublished or removed. Return to the catalog to continue shopping."
        action={<a href="/products">Return to products</a>}
      />
    );
  const selected =
    product.data.variants.find((variant) => variant.id === variantId) ??
    product.data.variants[0];
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow={product.data.categoryName}
        title={product.data.name}
        description={product.data.description}
      />
      <div className="detail-grid">
        <Card>
          {product.data.defaultImageUrl ? (
            <Image
              className="product-detail-image"
              src={product.data.defaultImageUrl}
              alt=""
              width={640}
              height={480}
              unoptimized
            />
          ) : (
            <div className="product-image-placeholder" />
          )}
        </Card>
        <Card>
          <p>{product.data.brand}</p>
          <SelectField
            id="variant"
            label="Variant"
            value={selected?.id ?? ""}
            onChange={(event) => setVariantId(event.target.value)}
          >
            {product.data.variants.map((variant) => (
              <option
                key={variant.id}
                value={variant.id}
                disabled={!variant.isInStock}
              >
                {variant.sku} —{" "}
                {money(variant.price, product.data!.currencyCode)}{" "}
                {!variant.isInStock ? "(Unavailable)" : ""}
              </option>
            ))}
          </SelectField>
          <InputField
            id="quantity"
            label="Quantity"
            type="number"
            min={1}
            max={selected?.stockQuantity ?? undefined}
            value={quantity}
            onChange={(event) => setQuantity(event.target.valueAsNumber)}
          />
          <p>
            {selected
              ? `${selected.businessVolume} BV per item`
              : "No purchasable variants"}
          </p>
          <Button
            disabled={!selected?.isInStock || quantity < 1}
            isLoading={isAdding}
            loadingLabel="Adding"
            onClick={async () => {
              if (!selected || !organization) return;
              setAdding(true);
              setMessage("");
              try {
                await storefrontApi.addCartItem(
                  api,
                  organization.id,
                  selected.id,
                  quantity,
                );
                setMessage("Added to your cart.");
              } catch (error) {
                setMessage(
                  error instanceof Error
                    ? error.message
                    : "Could not add this item.",
                );
              } finally {
                setAdding(false);
              }
            }}
          >
            Add to cart
          </Button>
          <ToastRegion>{message && <p>{message}</p>}</ToastRegion>
        </Card>
      </div>
    </div>
  );
}

export function ReferralStorePage({ code }: { code: string }) {
  const api = useApiClient();
  const { organization } = useOrganization();
  const [attribution, setAttribution] = useState("Connecting referral…");
  const store = useApiQuery(
    (client, signal) =>
      storefrontApi.referralStore(client, organization!.id, code, 1, signal),
    [organization?.id, code],
    Boolean(organization),
  );
  useEffect(() => {
    if (!organization) return;
    void storefrontApi
      .resolveReferral(api, organization.id, code)
      .then(() => setAttribution("Referral attribution applied."))
      .catch(() => setAttribution("This referral link could not be applied."));
  }, [api, code, organization]);
  if (store.isLoading) return <ScreenLoading />;
  if (store.error)
    return <ScreenError error={store.error} retry={store.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title={`Agent ${store.data?.agentCode ?? "store"}`}
        description={attribution}
      />
      {!store.data?.products.length ? (
        <EmptyState
          title="No referred products"
          description="This Agent storefront has no published products yet."
        />
      ) : (
        <div className="product-grid">
          {store.data.products.map((product) => (
            <Card key={product.productId}>
              <h2>
                <a href={`/products/${product.slug}`}>{product.name}</a>
              </h2>
              <p>{product.description}</p>
              <p>
                {money(
                  product.startingPrice,
                  organization?.currencyCode ?? "PHP",
                )}
              </p>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
