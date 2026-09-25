import type { Guid, IsoDateTime, Page } from "./common";
import type {
  CartDto,
  CustomerAddressDto,
  CustomerProfileDto,
  OrderDetailsDto,
  OrderSummariesPageDto,
  ProductDetailDto,
} from "./commerce";

export interface CategoryDto {
  id: Guid;
  name: string;
  slug: string;
}
export interface ProductDto {
  id: Guid;
  name: string;
  slug: string;
  imageUrl: string | null;
  price: number;
  businessVolume: number;
}
export interface AgentStorefrontDto {
  agentId: Guid;
  agentCode: string;
  referralCode: string;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  products: StorefrontProductDto[];
}
export interface StorefrontProductDto {
  productId: Guid;
  name: string;
  slug: string;
  description: string;
  imageUrl: string | null;
  startingPrice: number;
  referralUrl: string;
}
export interface ReferralAttributionDto {
  organizationId: Guid;
  agentId: Guid;
  agentCode: string;
  referralCode: string;
}
export interface PaymentSessionDto {
  paymentId: Guid;
  providerCheckoutSessionId: string;
  checkoutUrl: string;
}
export interface CheckoutAddressInput {
  recipientName: string;
  phoneNumber: string;
  addressLine1: string;
  addressLine2: string | null;
  barangay: string | null;
  cityOrMunicipality: string;
  province: string;
  postalCode: string;
  countryCode: string;
}
export interface CustomerAddressRequest extends CheckoutAddressInput {
  label: string;
  makeDefault: boolean;
}

export type StorefrontApiContracts = {
  categories: CategoryDto[];
  products: ProductDto[];
  product: ProductDetailDto;
  cart: CartDto;
  profile: CustomerProfileDto;
  addresses: CustomerAddressDto[];
  orders: OrderSummariesPageDto;
  order: OrderDetailsDto;
};

export interface PaymentReturnState {
  paymentId?: Guid;
  orderId?: Guid;
  status: "cancelled" | "pending" | "succeeded" | "failed" | "unknown";
  observedAt: IsoDateTime;
}
export type ProductPage = Page<ProductDto>;
