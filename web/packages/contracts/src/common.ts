export type Guid = string;

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  traceId?: string;
  errors?: Record<string, string[]> | string[];
}

export interface Page<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
  totalPages?: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}

export type IsoDateTime = string;

export interface StoredObjectDto {
  objectKey: string;
  url: string;
  contentType: string;
  contentLength: number;
  eTag: string | null;
  versionId: string | null;
}

export type Severity = "info" | "success" | "warning" | "danger";
