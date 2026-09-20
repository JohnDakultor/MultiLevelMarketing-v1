import { EmptyState } from "@modular-mlm/design-system";
export default function NotFound() {
  return (
    <EmptyState
      title="Page not found"
      description="The requested administration page does not exist."
      action={<a href="/">Return to the dashboard</a>}
    />
  );
}
