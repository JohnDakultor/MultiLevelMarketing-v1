import "server-only";

export function storefrontUrl(): string {
  return normalizeUrl(process.env.STOREFRONT_URL ?? "http://localhost:3000");
}

function normalizeUrl(value: string): string {
  return new URL(value.trim()).toString().replace(/\/$/, "");
}
