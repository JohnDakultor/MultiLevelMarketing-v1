export interface FrontendHeader {
  key: string;
  value: string;
}

export function frontendSecurityHeaders(): FrontendHeader[];
