import { AgentSignInPage } from "../../components/AgentSignInPage";

export default async function SignInPage({
  searchParams,
}: {
  searchParams: Promise<{ registered?: string; email?: string }>;
}) {
  const parameters = await searchParams;
  return (
    <AgentSignInPage
      accountCreated={parameters.registered === "true"}
      registeredEmail={parameters.email ?? ""}
    />
  );
}
