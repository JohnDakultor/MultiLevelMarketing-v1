import {
  isLocalReturnPath,
  normalizeOrganizationSlug,
  organizationSelectionCookie,
} from "@modular-mlm/organization-context";
import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";

export function GET(request: NextRequest): NextResponse {
  const slug = normalizeOrganizationSlug(
    request.nextUrl.searchParams.get("slug"),
  );
  if (!slug)
    return NextResponse.json(
      { title: "A valid organization slug is required." },
      { status: 400 },
    );

  const requestedReturnTo = request.nextUrl.searchParams.get("returnTo") ?? "/";
  const returnTo = isLocalReturnPath(requestedReturnTo)
    ? requestedReturnTo
    : "/";
  const response = new NextResponse(null, {
    status: 303,
    headers: { Location: returnTo },
  });
  response.cookies.set(organizationSelectionCookie, slug, {
    httpOnly: true,
    maxAge: 60 * 60 * 24 * 365,
    path: "/",
    sameSite: "lax",
    secure: request.nextUrl.protocol === "https:",
  });
  return response;
}
