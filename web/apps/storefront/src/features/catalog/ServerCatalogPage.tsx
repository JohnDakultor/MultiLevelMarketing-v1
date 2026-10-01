import type {
  CategoryDto,
  ProductPage,
  PublicOrganizationConfigDto,
} from "@modular-mlm/contracts";
import { organizationSelectionCookie } from "@modular-mlm/organization-context";
import { cookies, headers } from "next/headers";
import { CatalogPage } from "./CatalogPages";

const backendOrigin = (
  process.env.BACKEND_API_BASE_URL ?? "http://localhost:5154"
).replace(/\/$/, "");

export async function ServerCatalogPage({
  mode = "catalog",
}: {
  mode?: "home" | "catalog";
} = {}) {
  const requestHeaders = await headers();
  const cookieStore = await cookies();
  const hostName =
    requestHeaders.get("x-forwarded-host") ??
    requestHeaders.get("host") ??
    "localhost";
  const organization = await resolveOrganization(
    hostName,
    cookieStore.get(organizationSelectionCookie)?.value,
  );
  if (!organization) return <CatalogPage mode={mode} />;

  const [products, categories] = await Promise.all([
    getPublic<ProductPage>(
      `/api/organizations/${organization.id}/products?page=1&pageSize=24`,
    ),
    getPublic<CategoryDto[]>(
      `/api/organizations/${organization.id}/categories`,
    ),
  ]);
  return (
    <CatalogPage
      mode={mode}
      initialProducts={products ?? undefined}
      initialCategories={categories ?? []}
    />
  );
}

async function resolveOrganization(
  hostName: string,
  selectedSlug?: string,
): Promise<PublicOrganizationConfigDto | null> {
  const slug =
    selectedSlug ||
    (process.env.NODE_ENV === "development"
      ? process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim()
      : undefined);
  if (slug)
    return getPublic<PublicOrganizationConfigDto>(
      `/api/organizations/${encodeURIComponent(slug)}/public-config`,
    );

  const byHost = await fetch(
    `${backendOrigin}/api/organizations/public-config`,
    {
      headers: { "X-Forwarded-Host": hostName },
      next: { revalidate: 60 },
    },
  );
  if (byHost.ok) return (await byHost.json()) as PublicOrganizationConfigDto;

  return null;
}

async function getPublic<T>(path: string): Promise<T | null> {
  const response = await fetch(`${backendOrigin}${path}`, {
    next: { revalidate: 60 },
  });
  return response.ok ? ((await response.json()) as T) : null;
}
