import { EmptyState } from "@modular-mlm/design-system";
import Link from "next/link";
export default function NotFound() {
  return (
    <EmptyState
      title="Page not found"
      description="The requested Agent Portal page does not exist."
      action={<Link href="/">Return to the dashboard</Link>}
    />
  );
}
