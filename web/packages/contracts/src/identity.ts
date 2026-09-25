import type { Guid } from "./common";

export const Roles = {
  customer: "Customer",
  agent: "Agent",
  administrator: "Administrator",
  platformAdministrator: "PlatformAdministrator",
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];

export interface CurrentUserDto {
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
  organizationId: Guid | null;
  customerId: Guid | null;
  agentId: Guid | null;
  emailVerified: boolean;
  mfaEnabled: boolean;
}

export interface RevokeCurrentSessionRequest {
  sessionId: Guid;
}

export interface AuthenticationSessionDto {
  id: Guid;
  createdAt: string;
  lastRotatedAt: string;
  expiresAt: string;
  revokedAt: string | null;
  revocationReason: string | null;
  isCurrent: boolean;
}

export interface IdentityInfoDto {
  email: string;
  isEmailConfirmed: boolean;
}

export interface UpdateIdentityInfoRequest {
  newEmail: string | null;
  newPassword: string | null;
  oldPassword: string | null;
}

export interface TwoFactorRequest {
  enable?: boolean;
  twoFactorCode?: string;
  resetSharedKey?: boolean;
  resetRecoveryCodes?: boolean;
  forgetMachine?: boolean;
}

export interface TwoFactorResponse {
  sharedKey: string;
  recoveryCodesLeft: number;
  recoveryCodes: string[] | null;
  isTwoFactorEnabled: boolean;
  isMachineRemembered: boolean;
}

export interface AntiforgeryTokenResponse {
  requestToken: string;
  headerName: string;
}

export interface AuthTokenResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}
