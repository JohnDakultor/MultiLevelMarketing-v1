const hopByHopHeaders = [
  "connection",
  "content-length",
  "host",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
];

function backendOrigin(): string {
  return (process.env.BACKEND_API_BASE_URL ?? "http://localhost:5154").replace(
    /\/$/,
    "",
  );
}

export async function proxyBackendRequest(
  request: Request,
  path: readonly string[],
): Promise<Response> {
  const encodedPath = path.map(encodeURIComponent).join("/");
  const target = new URL(`/api/${encodedPath}`, backendOrigin());
  target.search = new URL(request.url).search;

  const requestHeaders = new Headers(request.headers);
  for (const header of hopByHopHeaders) requestHeaders.delete(header);

  const init: RequestInit = {
    method: request.method,
    headers: requestHeaders,
    cache: "no-store",
    redirect: "manual",
  };
  if (request.method !== "GET" && request.method !== "HEAD") {
    init.body = await request.arrayBuffer();
  }

  const upstream = await fetch(target, init);
  const responseHeaders = new Headers(upstream.headers);
  for (const header of hopByHopHeaders) responseHeaders.delete(header);

  return new Response(upstream.body, {
    status: upstream.status,
    statusText: upstream.statusText,
    headers: responseHeaders,
  });
}
