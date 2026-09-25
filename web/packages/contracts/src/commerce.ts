import type { Guid, IsoDateTime, Page } from "./common";

export interface ProductVariantDetailDto {
  id: Guid;
  sku: string;
  price: number;
  businessVolume: number;
  isInStock: boolean;
  stockQuantity: number | null;
  weight: number | null;
  attributes: unknown;
}

export interface ProductDetailDto {
  id: Guid;
  organizationId: Guid;
  categoryId: Guid;
  categoryName: string;
  name: string;
  slug: string;
  description: string;
  brand: string | null;
  defaultImageUrl: string | null;
  currencyCode: string;
  status: number;
  variants: ProductVariantDetailDto[];
}

export interface CartItemDto {
  id: Guid;
  productId: Guid;
  productSlug: string;
  productVariantId: Guid;
  productName: string;
  sku: string;
  imageUrl: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  businessVolumePerUnit: number;
  isAvailable: boolean;
  stockStatus: string;
}

export interface CartDto {
  id: Guid | null;
  organizationId: Guid;
  customerId: Guid | null;
  hasAnonymousSession: boolean;
  attributedAgentId: Guid | null;
  referralCode: string | null;
  items: CartItemDto[];
  currency: string;
  subtotal: number;
  itemCount: number;
}

export interface CustomerProfileDto {
  id: Guid;
  organizationId: Guid;
  displayName: string;
  defaultAddressId: Guid | null;
}

export interface CustomerAddressDto {
  id: Guid;
  label: string;
  recipientName: string;
  phoneNumber: string;
  addressLine1: string;
  addressLine2: string | null;
  barangay: string | null;
  cityOrMunicipality: string;
  province: string;
  postalCode: string;
  countryCode: string;
  isDefault: boolean;
}

export interface OrderSummaryDto {
  id: Guid;
  orderNumber: string;
  status: number;
  paymentStatus: number;
  currency: string;
  grandTotal: number;
  itemCount: number;
  createdAt: IsoDateTime;
  paidAt: IsoDateTime | null;
  deliveredAt: IsoDateTime | null;
}

export type OrderSummariesPageDto = Page<OrderSummaryDto>;

export interface AddressDto {
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

export interface OrderItemDetailsDto {
  id: Guid;
  productId: Guid;
  productVariantId: Guid;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  businessVolume: number;
  fulfillmentStatus: number;
  refundedQuantity: number;
  refundableQuantity: number;
  canRequestRefund: boolean;
  refundFailureReason: string | null;
}

export interface OrderDetailsDto {
  id: Guid;
  orderNumber: string;
  status: number;
  paymentStatus: number;
  currency: string;
  subtotal: number;
  discountTotal: number;
  shippingTotal: number;
  taxTotal: number;
  grandTotal: number;
  shippingAddress: AddressDto;
  billingAddress: AddressDto;
  createdAt: IsoDateTime;
  paidAt: IsoDateTime | null;
  deliveredAt: IsoDateTime | null;
  canRequestCancellation: boolean;
  cancellationFailureReason: string | null;
  items: OrderItemDetailsDto[];
}
