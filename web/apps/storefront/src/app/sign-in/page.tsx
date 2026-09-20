import { StorefrontSignInPage } from "../../components/StorefrontSignInPage";

interface SignInPageProps {
  searchParams: Promise<{
    registered?: string;
    email?: string;
  }>;
}

export default async function SignInPage({ searchParams }: SignInPageProps) {
  const parameters = await searchParams;
  return (
    <StorefrontSignInPage
      accountCreated={parameters.registered === "true"}
      registeredEmail={parameters.email ?? ""}
    />
  );
}
