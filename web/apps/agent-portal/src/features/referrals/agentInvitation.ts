export function createAgentInvitationUrl(
  origin: string,
  sponsorReferralCode: string,
): string {
  const url = new URL("/register", origin);
  url.searchParams.set("sponsor", sponsorReferralCode.trim().toUpperCase());
  return url.toString();
}

export function createAgentOnboardingPath(
  sponsorReferralCode: string | null | undefined,
): string {
  const code = sponsorReferralCode?.trim();
  return code ? `/?sponsor=${encodeURIComponent(code)}` : "/";
}
