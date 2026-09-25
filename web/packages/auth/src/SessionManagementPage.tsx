"use client";

import type { AuthenticationSessionDto } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  EmptyState,
  PageHeader,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useEffect, useState } from "react";
import { useAuthentication } from "./AuthenticationProvider";

export function SessionManagementPage({
  signInPath,
  showHeader = true,
}: {
  signInPath: string;
  showHeader?: boolean;
}) {
  const authentication = useAuthentication();
  const { confirm } = useConfirmation();
  const [sessions, setSessions] = useState<AuthenticationSessionDto[]>([]);
  const [isLoading, setLoading] = useState(true);
  const [actingId, setActingId] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  async function load(signal?: AbortSignal) {
    setLoading(true);
    setError("");
    try {
      setSessions(await authentication.listSessions(signal));
    } catch (requestError) {
      if (!signal?.aborted)
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Sessions could not be loaded.",
        );
    } finally {
      if (!signal?.aborted) setLoading(false);
    }
  }

  useEffect(() => {
    const controller = new AbortController();
    void authentication
      .listSessions(controller.signal)
      .then((result) => setSessions(result))
      .catch((requestError: unknown) => {
        if (!controller.signal.aborted)
          setError(
            requestError instanceof Error
              ? requestError.message
              : "Sessions could not be loaded.",
          );
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [authentication]);

  async function revoke(session: AuthenticationSessionDto) {
    if (
      !(await confirm({
        title: session.isCurrent ? "Sign out this session?" : "Revoke session?",
        description: session.isCurrent
          ? "This browser will be signed out immediately."
          : `Revoke the session created ${formatDate(session.createdAt)}? That browser must sign in again.`,
        confirmLabel: session.isCurrent ? "Sign out" : "Revoke session",
      }))
    )
      return;
    setActingId(session.id);
    setError("");
    try {
      await authentication.revokeSession(session.id);
      if (session.isCurrent) {
        authentication.clear();
        window.location.assign(signInPath);
        return;
      }
      setMessage("Session revoked.");
      await load();
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "The session could not be revoked.",
      );
    } finally {
      setActingId(null);
    }
  }

  return (
    <div className="content-stack">
      {showHeader ? (
        <PageHeader
          title="Sign-in sessions"
          description="Review where your account is signed in and revoke sessions you no longer trust."
        />
      ) : (
        <h2>Sign-in sessions</h2>
      )}
      {error && (
        <Alert title="Session operation failed" tone="danger">
          <p>{error}</p>
          <Button variant="secondary" onClick={() => void load()}>
            Try again
          </Button>
        </Alert>
      )}
      {message && <Alert title={message} tone="success" />}
      {error ? null : isLoading ? (
        <p role="status">Loading sign-in sessions…</p>
      ) : !sessions.length ? (
        <EmptyState
          title="No sessions found"
          description="No recorded sign-in sessions are available for this account."
        />
      ) : (
        sessions.map((session) => (
          <Card key={session.id}>
            <div className="action-row">
              <div>
                <h2>
                  {session.isCurrent ? "This browser" : "Signed-in browser"}
                </h2>
                <p>
                  Created {formatDate(session.createdAt)} · expires{" "}
                  {formatDate(session.expiresAt)}
                </p>
                {session.revokedAt && (
                  <p>
                    Revoked {formatDate(session.revokedAt)} ·{" "}
                    {session.revocationReason ?? "revoked"}
                  </p>
                )}
              </div>
              {!session.revokedAt && (
                <Button
                  variant="danger"
                  disabled={actingId !== null}
                  isLoading={actingId === session.id}
                  onClick={() => void revoke(session)}
                >
                  {session.isCurrent ? "Sign out" : "Revoke"}
                </Button>
              )}
            </div>
          </Card>
        ))
      )}
    </div>
  );
}

const formatDate = (value: string) =>
  new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
