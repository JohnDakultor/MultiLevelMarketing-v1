import {
  AccountHomePage,
  AgentApplicationPage,
  AddressesPage,
  OrderDetailsPage,
  OrdersPage,
  ProfilePage,
  AccountSecurityPage,
} from "../../features/account/AccountPages";
import { CartPage } from "../../features/cart/CartPage";
import {
  ProductPage,
  ReferralStorePage,
} from "../../features/catalog/CatalogPages";
import {
  CheckoutPage,
  PaymentReturnPage,
} from "../../features/checkout/CheckoutPages";
import { ServerCatalogPage } from "../../features/catalog/ServerCatalogPage";
import { notFound } from "next/navigation";

export default async function StorefrontRoute({
  params,
}: {
  params: Promise<{ path: string[] }>;
}) {
  const path = (await params).path;
  if (path.length === 1 && path[0] === "products") return <ServerCatalogPage />;
  if (path.length === 2 && path[0] === "products")
    return <ProductPage slug={path[1]!} />;
  if (path.length === 2 && path[0] === "r")
    return <ReferralStorePage code={path[1]!} />;
  if (path.length === 1 && path[0] === "cart") return <CartPage />;
  if (path.length === 1 && path[0] === "checkout") return <CheckoutPage />;
  if (path.length === 2 && path[0] === "checkout" && path[1] === "result")
    return <PaymentReturnPage />;
  if (path.length === 1 && path[0] === "account") return <AccountHomePage />;
  if (path.length === 2 && path[0] === "account" && path[1] === "profile")
    return <ProfilePage />;
  if (path.length === 2 && path[0] === "account" && path[1] === "addresses")
    return <AddressesPage />;
  if (path.length === 2 && path[0] === "account" && path[1] === "orders")
    return <OrdersPage />;
  if (path.length === 2 && path[0] === "account" && path[1] === "security")
    return <AccountSecurityPage />;
  if (
    path.length === 2 &&
    path[0] === "account" &&
    path[1] === "agent-application"
  )
    return <AgentApplicationPage />;
  if (path.length === 3 && path[0] === "account" && path[1] === "orders")
    return <OrderDetailsPage orderId={path[2]!} />;
  notFound();
}
