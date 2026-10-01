"use client";

import { useApiClient, validationMessages } from "@modular-mlm/api-client";
import { Button } from "@modular-mlm/design-system";
import { useState } from "react";

export function ResendConfirmationAction({ email }: { email: string }) {
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
