export function formatMoney(
  value: number,
  currency: string,
  locale?: string,
): string {
  return new Intl.NumberFormat(locale, { style: "currency", currency }).format(
    value,
  );
}

export function formatDateTime(
  value: string | null | undefined,
  locale?: string,
): string {
  if (!value) return "—";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.valueOf())) return "—";
  return new Intl.DateTimeFormat(locale, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(parsed);
}

export function formatDate(
  value: string | null | undefined,
  locale?: string,
): string {
  if (!value) return "—";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.valueOf())) return "—";
  return new Intl.DateTimeFormat(locale, { dateStyle: "medium" }).format(
    parsed,
  );
}

export function formatStatusLabel(value: string | number): string {
  return String(value)
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .replace(/^./, (letter) => letter.toUpperCase());
}
