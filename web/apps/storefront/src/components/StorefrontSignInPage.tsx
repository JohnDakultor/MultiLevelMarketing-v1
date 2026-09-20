"use client";

import { useApiClient, validationMessages } from "@modular-mlm/api-client";
import {
  safeReturnPath,
  SignInForm,
  useAuthentication,
} from "@modular-mlm/auth";
import { Alert, Button } from "@modular-mlm/design-system";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export function StorefrontSignInPage({
  accountCreated = false,
  registeredEmail = "",
}: {
  accountCreated?: boolean;
  registeredEmail?: string;
}) {
  const router = useRouter();
  const authentication = useAuthentication();

  function returnPath(): string {
    return safeReturnPath(
      new URLSearchParams(window.location.search).get("returnTo"),
      "/account",
    );
  }

  useEffect(() => {
    if (authentication.user) router.replace(returnPath());
  }, [authentication.user, router]);

  return (
    <div className="auth-page">
      {accountCreated && (
        <Alert tone="success" title="Account created">
          Confirm your email address if a confirmation message was sent, then
          sign in with the password you created.
        </Alert>
      )}
      {accountCreated && registeredEmail && (
        <ResendConfirmationAction email={registeredEmail} />
      )}
      <SignInForm
        title="Sign in to your account"
        description="Manage your addresses, orders, cancellations, and refunds."
        initialEmail={registeredEmail}
        onSuccess={() => router.replace(returnPath())}
      />
      <p>
        <a href="/forgot-password">Forgot your password?</a>
      </p>
      <p>
        New customer? <a href="/register">Create an account</a>
      </p>
    </div>
  );
}

function ResendConfirmationAction({ email }: { email: string }) {
  const api = useApiClient();
  const [isSending, setSending] = useState(false);
  const [message, setMessage] = useState("");

  return (
    <div className="auth-confirmation-action">
      <p>Did not receive the confirmation email?</p>
      <Button
        type="button"
        variant="secondary"
        isLoading={isSending}
        disabled={isSending}
        onClick={async () => {
          setSending(true);
          setMessage("");
          try {
            await api.request<void>("/api/Users/resendConfirmationEmail", {
              method: "POST",
              anonymous: true,
              skipAntiforgery: true,
              body: { email },
            });
            setMessage("A new confirmation email has been requested.");
          } catch (error) {
            setMessage(validationMessages(error).join(" "));
          } finally {
            setSending(false);
          }
        }}
      >
        Resend confirmation email
      </Button>
      {message && <p role="status">{message}</p>}
    </div>
  );
}
