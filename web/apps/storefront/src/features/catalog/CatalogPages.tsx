"use client";

import { useApiClient, useApiQuery } from "@modular-mlm/api-client";
import {
  Alert,
  Badge,
  Button,
  Card,
  EmptyState,
  InputField,
  PageHeader,
  SelectField,
  ToastRegion,
} from "@modular-mlm/design-system";
import type {
  CategoryDto,
  ProductDto,
  ProductPage,
} from "@modular-mlm/contracts";
import { useOrganization } from "@modular-mlm/organization-context";
import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { storefrontApi } from "../api/storefrontApi";
import { ScreenError, ScreenLoading, money } from "../shared/ScreenState";
import {
  ProductCard,
  ProductImagePlaceholder,
  StorefrontHero,
  StorefrontSectionHeader,
  TrustStrip,
} from "../shared/StorefrontPrimitives";

export function CatalogPage({
  mode = "catalog",
  initialProducts,
  initialCategories,
}: {
  mode?: "home" | "catalog";
  initialProducts?: ProductPage;
  initialCategories?: CategoryDto[];
} = {}) {
  const { organization } = useOrganization();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [availability, setAvailability] = useState<
    "Any" | "InStock" | "OutOfStock"
  >("Any");
  const [sort, setSort] = useState<
    | "Newest"
    | "PriceAscending"
    | "PriceDescending"
    | "NameAscending"
    | "NameDescending"
  >("Newest");
  const [minimumPrice, setMinimumPrice] = useState("");
  const [maximumPrice, setMaximumPrice] = useState("");
  const debouncedSearch = useDebouncedValue(search, 300);
  const products = useApiQuery(
    (api, signal) =>
      storefrontApi.products(
        api,
        organization!.id,
        {
          page,
          search: debouncedSearch.trim() || undefined,
          categoryId: categoryId || undefined,
          minimumPrice: optionalNumber(minimumPrice),
          maximumPrice: optionalNumber(maximumPrice),
          availability,
          sort,
        },
        signal,
      ),
    [
      organization?.id,
      page,
      debouncedSearch,
      categoryId,
      minimumPrice,
      maximumPrice,
      availability,
      sort,
    ],
    Boolean(organization),
    page === 1 ? initialProducts : undefined,
  );
  const categories = useApiQuery(
    (api, signal) => storefrontApi.categories(api, organization!.id, signal),
    [organization?.id],
    Boolean(organization),
    initialCategories,
  );
  const shown =
    mode === "home"
      ? (products.data?.items ?? []).slice(0, 8)
      : (products.data?.items ?? []);

  if (products.isLoading || categories.isLoading) return <ScreenLoading />;
  if (products.error)
    return <ScreenError error={products.error} retry={products.reload} />;

  const currency = organization?.currencyCode ?? "PHP";
  const storeName = organization?.storeTitle || organization?.name || "Shop";

  if (mode === "home") {
    return (
      <div className="storefront-home">
        <StorefrontHero
          storeName={storeName}
          productCount={products.data?.totalCount ?? 0}
          categoryCount={categories.data?.length ?? 0}
        />
        {categories.data?.length ? (
          <section
            className="storefront-section"
            aria-labelledby="collections-title"
          >
            <StorefrontSectionHeader
              eyebrow="Explore"
              title="Shop by collection"
              description="Explore the collections available in this shop."
              action={
                <Link className="storefront-text-link" href="/products">
                  View all products <span aria-hidden>→</span>
                </Link>
              }
            />
            <div className="storefront-category-list" id="collections-title">
              {categories.data.map((category, index) => (
                <div className="storefront-category-card" key={category.id}>
                  <span>{String(index + 1).padStart(2, "0")}</span>
                  <strong>{category.name}</strong>
                  <small>Collection</small>
                </div>
              ))}
            </div>
          </section>
        ) : null}
        <section className="storefront-section" id="featured-products">
          <StorefrontSectionHeader
            eyebrow="Featured"
            title="Fresh from the catalog"
            description="Recently available products with current prices."
            action={
              <Link className="storefront-text-link" href="/products">
                Shop everything <span aria-hidden>→</span>
              </Link>
            }
          />
          <ProductResults products={shown} currency={currency} />
        </section>
        <TrustStrip />
      </div>
    );
  }

  return (
    <div className="content-stack storefront-catalog-page">
      <PageHeader
        eyebrow="The collection"
        title="Shop all products"
        description={`Explore the latest products from ${storeName}.`}
      />
      <div className="storefront-catalog-toolbar">
        <InputField
          id="catalog-search"
          label="Search products"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
          placeholder="Search product names"
          type="search"
        />
        <SelectField
          id="catalog-category"
          label="Collection"
          value={categoryId}
          onChange={(event) => {
            setCategoryId(event.target.value);
            setPage(1);
          }}
        >
          <option value="">All collections</option>
          {categories.data?.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </SelectField>
        <SelectField
          id="catalog-availability"
          label="Availability"
          value={availability}
          onChange={(event) => {
            setAvailability(event.target.value as typeof availability);
            setPage(1);
          }}
        >
          <option value="Any">Any availability</option>
          <option value="InStock">In stock</option>
          <option value="OutOfStock">Out of stock</option>
        </SelectField>
        <InputField
          id="catalog-minimum-price"
          label="Minimum price"
          type="number"
          min={0}
          step="0.01"
          value={minimumPrice}
          onChange={(event) => {
            setMinimumPrice(event.target.value);
            setPage(1);
          }}
        />
        <InputField
          id="catalog-maximum-price"
          label="Maximum price"
          type="number"
          min={0}
          step="0.01"
          value={maximumPrice}
          onChange={(event) => {
            setMaximumPrice(event.target.value);
            setPage(1);
          }}
        />
        <SelectField
          id="catalog-sort"
          label="Sort"
          value={sort}
          onChange={(event) => {
            setSort(event.target.value as typeof sort);
            setPage(1);
          }}
        >
          <option value="Newest">Newest</option>
          <option value="PriceAscending">Price: low to high</option>
          <option value="PriceDescending">Price: high to low</option>
          <option value="NameAscending">Name: A to Z</option>
          <option value="NameDescending">Name: Z to A</option>
        </SelectField>
        <div className="storefront-catalog-toolbar__summary" aria-live="polite">
          <strong>{products.data?.totalCount ?? 0}</strong>
          <span>
            {products.data?.totalCount === 1 ? "product" : "products"}
          </span>
        </div>
      </div>
      <ProductResults products={shown} currency={currency} />
      <div className="pagination-row storefront-pagination">
        <Button
          variant="secondary"
          disabled={page === 1}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span aria-live="polite">Page {page}</span>
        <Button
          variant="secondary"
          disabled={!products.data?.hasNextPage}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}

function optionalNumber(value: string): number | undefined {
  if (!value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : undefined;
}

function useDebouncedValue<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timeout = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(timeout);
  }, [delay, value]);
  return debounced;
}

function ProductResults({
  products,
  currency,
}: {
  products: ProductDto[];
  currency: string;
}) {
  if (!products.length)
    return (
      <EmptyState
        title="No matching products"
        description="Try a different search or return when more products have been published."
      />
    );
  return (
    <div className="storefront-product-grid">
      {products.map((product) => (
        <ProductCard key={product.id} product={product} currency={currency} />
      ))}
    </div>
  );
}

export function ProductPage({ slug }: { slug: string }) {
  const api = useApiClient();
  const { organization } = useOrganization();
  const [variantId, setVariantId] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [message, setMessage] = useState("");
  const [messageTone, setMessageTone] = useState<"success" | "danger">(
    "success",
  );
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
        description="This product may have been unpublished or removed."
        action={<Link href="/products">Return to products</Link>}
      />
    );

  const selected =
    product.data.variants.find((variant) => variant.id === variantId) ??
    product.data.variants[0];
  const hasPurchasableVariant = product.data.variants.some(
    (variant) => variant.isInStock,
  );

  return (
    <div className="content-stack storefront-product-page">
      <nav className="storefront-breadcrumb" aria-label="Breadcrumb">
        <Link href="/">Home</Link>
        <span aria-hidden>/</span>
        <Link href="/products">Shop</Link>
        <span aria-hidden>/</span>
        <span aria-current="page">{product.data.name}</span>
      </nav>
      <div className="storefront-product-detail">
        <div className="storefront-product-detail__media">
          {product.data.defaultImageUrl ? (
            <Image
              className="product-detail-image"
              src={product.data.defaultImageUrl}
              alt={product.data.name}
              width={760}
              height={900}
              sizes="(max-width: 768px) 100vw, 55vw"
              priority
              unoptimized
            />
          ) : (
            <ProductImagePlaceholder label={`${product.data.name} image`} />
          )}
        </div>
        <Card className="storefront-product-detail__purchase">
          <div className="storefront-product-detail__heading">
            <div>
              <p className="storefront-kicker">{product.data.categoryName}</p>
              <h1>{product.data.name}</h1>
              {product.data.brand && <p>by {product.data.brand}</p>}
            </div>
            <Badge tone={hasPurchasableVariant ? "success" : "danger"}>
              {hasPurchasableVariant ? "In stock" : "Out of stock"}
            </Badge>
          </div>
          <p className="storefront-product-detail__price">
            {selected
              ? money(selected.price, product.data.currencyCode)
              : "Unavailable"}
          </p>
          <p className="storefront-product-detail__description">
            {product.data.description}
          </p>
          <div className="storefront-purchase-form">
            <SelectField
              id="variant"
              label="Choose a variant"
              value={selected?.id ?? ""}
              onChange={(event) => {
                setVariantId(event.target.value);
                setQuantity(1);
              }}
            >
              {product.data.variants.map((variant) => (
                <option
                  key={variant.id}
                  value={variant.id}
                  disabled={!variant.isInStock}
                >
                  {variant.sku} —{" "}
                  {money(variant.price, product.data!.currencyCode)}
                  {!variant.isInStock ? " (Unavailable)" : ""}
                </option>
              ))}
            </SelectField>
            <InputField
              id="quantity"
              label="Quantity"
              type="number"
              min={1}
              max={selected?.stockQuantity ?? undefined}
              value={Number.isFinite(quantity) ? quantity : ""}
              onChange={(event) => setQuantity(event.target.valueAsNumber)}
              hint={
                selected?.stockQuantity == null
                  ? undefined
                  : `${selected.stockQuantity} available`
              }
            />
            <Button
              disabled={
                !selected?.isInStock ||
                !Number.isInteger(quantity) ||
                quantity < 1 ||
                (selected.stockQuantity != null &&
                  quantity > selected.stockQuantity)
              }
              isLoading={isAdding}
              loadingLabel="Adding to cart"
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
                  setMessageTone("success");
                  setMessage("Added to your cart.");
                } catch (error) {
                  setMessageTone("danger");
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
            <Link className="ds-button ds-button--secondary" href="/cart">
              View cart
            </Link>
          </div>
          <ul className="storefront-purchase-assurances">
            <li>Live stock validation at checkout</li>
            <li>Secure payment</li>
            <li>Order tracking from your account</li>
          </ul>
          <ToastRegion>
            {message && <Alert title={message} tone={messageTone} />}
          </ToastRegion>
        </Card>
      </div>
      <TrustStrip />
    </div>
  );
}

export function ReferralStorePage({ code }: { code: string }) {
  const api = useApiClient();
  const { organization } = useOrganization();
  const [attribution, setAttribution] = useState("Opening your collection…");
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
      .then(() => setAttribution("Your shopping link is active."))
      .catch(() => setAttribution("This shopping link could not be opened."));
  }, [api, code, organization]);
  if (store.isLoading) return <ScreenLoading />;
  if (store.error)
    return <ScreenError error={store.error} retry={store.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Recommended for you"
        title="Your selected collection"
        description={attribution}
      />
      {!store.data?.products.length ? (
        <EmptyState
          title="No products yet"
          description="There are no products in this collection yet."
        />
      ) : (
        <div className="storefront-product-grid">
          {store.data.products.map((product) => (
            <article
              className="storefront-product-card"
              key={product.productId}
            >
              <Link
                className="storefront-product-card__media"
                href={`/products/${product.slug}`}
              >
                {product.imageUrl ? (
                  <Image
                    src={product.imageUrl}
                    alt={product.name}
                    width={560}
                    height={680}
                    unoptimized
                  />
                ) : (
                  <ProductImagePlaceholder label={`${product.name} image`} />
                )}
              </Link>
              <div className="storefront-product-card__body">
                <div>
                  <h3>
                    <Link href={`/products/${product.slug}`}>
                      {product.name}
                    </Link>
                  </h3>
                  <p>{product.description}</p>
                </div>
                <strong>
                  {money(
                    product.startingPrice,
                    organization?.currencyCode ?? "PHP",
                  )}
                </strong>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
