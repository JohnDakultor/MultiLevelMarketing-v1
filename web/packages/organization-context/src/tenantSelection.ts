export const organizationSelectionCookie = "modular-mlm-organization";

const organizationSlugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export function normalizeOrganizationSlug(
  value: string | null | undefined,
): string | undefined {
  const normalized = value?.trim().toLowerCase();
  if (!normalized || !organizationSlugPattern.test(normalized))
    return undefined;
  return normalized;
}

export function organizationSelectionUrl(slug: string, returnTo = "/"): string {
  const normalizedSlug = normalizeOrganizationSlug(slug);
  if (!normalizedSlug)
    throw new Error("A valid organization slug is required.");

  const safeReturnTo = isLocalReturnPath(returnTo) ? returnTo : "/";
  const parameters = new URLSearchParams({
    slug: normalizedSlug,
    returnTo: safeReturnTo,
  });
  return `/select-organization?${parameters.toString()}`;
}

export function isLocalReturnPath(value: string): boolean {
  return (
    value.startsWith("/") && !value.startsWith("//") && !value.includes("\\")
  );
}
