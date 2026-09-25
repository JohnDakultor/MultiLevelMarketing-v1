import type { Guid, IsoDateTime, Page } from "./common";

export interface NotificationDto {
  id: Guid;
  kind: number;
  title: string;
  plainTextBody: string;
  actionPath: string | null;
  createdAt: IsoDateTime;
  deliveredAt: IsoDateTime | null;
  readAt: IsoDateTime | null;
  isRead: boolean;
}

export interface NotificationPageDto extends Page<NotificationDto> {
  unreadCount: number;
}
