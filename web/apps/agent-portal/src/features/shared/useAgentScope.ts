import { useAuthentication } from "@modular-mlm/auth";
import { useOrganization } from "@modular-mlm/organization-context";

export function useAgentScope() {
  const { user } = useAuthentication();
  const { organization } = useOrganization();
  return {
    organizationId: organization?.id ?? "",
    agentId: user?.agentId ?? "",
    currency: organization?.currencyCode ?? "PHP",
    isReady: Boolean(organization?.id && user?.agentId),
  };
}
