"use client";

import { useApiClient, useApiQuery } from "@modular-mlm/api-client";
import type { NotificationPageDto } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  EmptyState,
  PageHeader,
  Skeleton,
} from "@modular-mlm/design-system";
import { useState } from "react";

export function NotificationCenter({
  organizationId,
}: {
  organizationId: string;
}) {
  const api = useApiClient();
  const [page, setPage] = useState(1);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [workingId, setWorkingId] = useState<string | null>(null);
  const [message, setMessage] = useState("");
  const notifications = useApiQuery(
    (client, signal) =>
      client.request<NotificationPageDto>(
        `/api/organizations/${organizationId}/me/notifications?page=${page}&pageSize=20&unreadOnly=${unreadOnly}`,
        { signal },
      ),
    [organizationId, page, unreadOnly],
    Boolean(organizationId),
  );

  if (notifications.isLoading)
    return (
      <div className="content-stack">
        <Skeleton height="5rem" />
        <Skeleton height="12rem" />
      </div>
    );
  if (notifications.error)
    return (
      <div className="content-stack">
        <Alert title="Notifications could not be loaded" tone="danger">
          {notifications.error.message}
        </Alert>
        <Button variant="secondary" onClick={notifications.reload}>
          Try again
        </Button>
      </div>
    );

  return (
    <div className="content-stack">
      <PageHeader
        title="Notifications"
        description={`${notifications.data?.unreadCount ?? 0} unread notification(s).`}
        action={
          <Button
            variant="secondary"
            onClick={() => {
              setPage(1);
              setUnreadOnly((value) => !value);
            }}
          >
            {unreadOnly ? "Show all" : "Unread only"}
          </Button>
        }
      />
      {message && <p role="status">{message}</p>}
      {!notifications.data?.items.length ? (
        <EmptyState
          title={unreadOnly ? "No unread notifications" : "No notifications"}
          description={
            unreadOnly
              ? "You have read everything in this organization."
              : "Updates about your account and activity will appear here."
          }
        />
      ) : (
        notifications.data.items.map((notification) => (
          <Card key={notification.id}>
            <div className="summary-line">
              <div>
                <h2>{notification.title}</h2>
                <p>{notification.plainTextBody}</p>
                <small>
                  {new Date(notification.createdAt).toLocaleString()}
                </small>
              </div>
              {!notification.isRead && (
                <Button
                  variant="secondary"
                  isLoading={workingId === notification.id}
                  disabled={workingId !== null}
                  onClick={async () => {
                    setWorkingId(notification.id);
                    setMessage("");
                    try {
                      await api.request<void>(
                        `/api/organizations/${organizationId}/me/notifications/${notification.id}/read`,
                        { method: "PUT" },
                      );
                      setMessage(`Marked “${notification.title}” as read.`);
                      notifications.reload();
                    } catch (error) {
                      setMessage(
                        error instanceof Error
                          ? error.message
                          : "The notification could not be updated.",
                      );
                    } finally {
                      setWorkingId(null);
                    }
                  }}
                >
                  Mark as read
                </Button>
              )}
            </div>
            {notification.actionPath && (
              <a href={safeActionPath(notification.actionPath)}>View details</a>
            )}
          </Card>
        ))
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={!notifications.data?.hasPreviousPage}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>Page {notifications.data?.page ?? page}</span>
        <Button
          variant="secondary"
          disabled={!notifications.data?.hasNextPage}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}

function safeActionPath(path: string): string {
  return path.startsWith("/") && !path.startsWith("//") ? path : "/";
}
