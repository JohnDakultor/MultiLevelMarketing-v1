import type { Guid, IsoDateTime } from "./common";

export interface ReferralSettingsDto {
  attributionWindowDays: number;
  allowReferralOverride: boolean;
  referralLockAfterFirstPurchase: boolean;
}

export interface CreateOrganizationRequest {
  name: string;
  slug: string;
  currencyCode: string;
  timeZone: string;
  locale: string;
}

export interface UpdateBrandingRequest {
  storeTitle: string;
  supportEmail: string;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  logoUrl: string | null;
  faviconUrl: string | null;
  supportPhone: string | null;
  footerText: string | null;
}

export interface PublicOrganizationConfigDto {
  id: Guid;
  name: string;
  slug: string;
  currencyCode: string;
  locale: string;
  storeTitle: string;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  logoUrl: string | null;
  agentProgramEnabled: boolean;
  binaryNetworkEnabled: boolean;
  walletEnabled: boolean;
  payoutEnabled: boolean;
}

export interface OrganizationProfileDto {
  id: Guid;
  name: string;
  slug: string;
  status: number;
  currencyCode: string;
  timeZone: string;
  locale: string;
}

export interface BrandingSettingsDto {
  logoUrl: string | null;
  faviconUrl: string | null;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  storeTitle: string;
  supportEmail: string;
  supportPhone: string | null;
  footerText: string | null;
  revision: number;
  publishedRevision: number;
  publishedAt: IsoDateTime | null;
}

export interface FeatureSettingsDto {
  commerceEnabled: boolean;
  agentProgramEnabled: boolean;
  binaryNetworkEnabled: boolean;
  binaryPairingEnabled: boolean;
  walletEnabled: boolean;
  payoutEnabled: boolean;
  reviewsEnabled: boolean;
  couponsEnabled: boolean;
}

export interface CommerceSettingsDto {
  allowGuestCheckout: boolean;
  requireShippingAddress: boolean;
  requireBillingAddress: boolean;
  inventoryReservationMinutes: number;
}

export interface NetworkSettingsDto {
  defaultPlacementStrategy: number;
  allowAgentPreferredLeg: boolean;
  maxQueryDepth: number;
  autoPlacementEnabled: boolean;
  restrictPlacementChangesAfterActivation: boolean;
}

export interface OrganizationDomainDto {
  id: Guid;
  hostName: string;
  isPrimary: boolean;
  isVerified: boolean;
  createdAt: IsoDateTime;
  verifiedAt: IsoDateTime | null;
}

export interface AdminOrganizationSettingsDto {
  profile: OrganizationProfileDto;
  branding: BrandingSettingsDto;
  features: FeatureSettingsDto;
  commerce: CommerceSettingsDto;
  network: NetworkSettingsDto;
  domains: OrganizationDomainDto[];
}

export interface WalletSettingsDto {
  commissionReleaseTrigger: number;
  releaseDelayDays: number;
  returnWindowDays: number;
  minimumPayoutAmount: number;
  allowNegativeRecoverableBalance: boolean;
  maximumNegativeBalance: number;
}
