import { NextRequest, NextResponse } from "next/server";
import {
  ACCESS_COOKIE,
  API_BASE_URL,
  REFRESH_COOKIE,
  clearSession,
  writeSession,
} from "@/lib/session";
import type { LoginResponse } from "@/lib/types";

export const dynamic = "force-dynamic";

const METHODS_WITH_BODY = new Set(["POST", "PUT", "PATCH"]);

/** Response headers worth carrying back — the rest are Next's to set. */
const PASS_THROUGH = ["content-type", "content-disposition", "content-length"];

function forward(
  target: string,
  method: string,
  token: string | undefined,
  body: ArrayBuffer | undefined,
  contentType: string | null,
) {
  return fetch(`${API_BASE_URL}${target}`, {
    method,
    headers: {
      // Forwarded verbatim rather than forced to JSON: a multipart upload carries
      // its own boundary in this header, and rewriting it corrupts the body.
      ...(contentType ? { "Content-Type": contentType } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body,
    cache: "no-store",
  });
}

/** Exchanges a refresh token for a fresh pair. Returns null if it is spent. */
async function tryRefresh(refreshToken: string): Promise<LoginResponse | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    });
    return res.ok ? ((await res.json()) as LoginResponse) : null;
  } catch {
    return null;
  }
}

/**
 * Same-origin proxy to the ASP.NET API.
 *
 * Everything the browser fetches goes through here, which buys three things:
 * the bearer token stays in an httpOnly cookie (unreadable by any script),
 * CORS never enters the picture, and a 401 can be transparently retried after
 * a token refresh instead of bouncing the user to /login mid-task.
 *
 * Bodies are buffered rather than streamed — the retry after a refresh has to
 * replay the same body, and a stream can only be read once. The API caps a
 * request at 32 MB, so the buffer is bounded.
 */
async function handler(
  req: NextRequest,
  ctx: { params: Promise<{ path: string[] }> },
) {
  const { path } = await ctx.params;
  const target = `/api/${path.join("/")}${req.nextUrl.search}`;

  const accessToken = req.cookies.get(ACCESS_COOKIE)?.value;
  const refreshToken = req.cookies.get(REFRESH_COOKIE)?.value;

  const hasBody = METHODS_WITH_BODY.has(req.method);
  const body = hasBody ? await req.arrayBuffer() : undefined;
  const contentType = hasBody ? req.headers.get("content-type") : null;

  let upstream: Response;
  try {
    upstream = await forward(target, req.method, accessToken, body, contentType);
  } catch {
    return NextResponse.json(
      { title: "API-এর সাথে সংযোগ করা যায়নি।", status: 502 },
      { status: 502 },
    );
  }

  let refreshed: LoginResponse | null = null;
  if (upstream.status === 401 && refreshToken) {
    refreshed = await tryRefresh(refreshToken);
    if (refreshed) {
      upstream = await forward(
        target,
        req.method,
        refreshed.accessToken,
        body,
        contentType,
      );
    }
  }

  // Read as bytes, not text: attachment downloads come back as PDFs and images.
  const payload = await upstream.arrayBuffer();

  const headers = new Headers();
  for (const name of PASS_THROUGH) {
    const value = upstream.headers.get(name);
    if (value) headers.set(name, value);
  }
  if (!headers.has("content-type")) headers.set("content-type", "application/json");

  const res = new NextResponse(payload.byteLength > 0 ? payload : null, {
    status: upstream.status,
    headers,
  });

  if (refreshed) {
    writeSession(res.cookies, refreshed);
  } else if (upstream.status === 401) {
    // Both tokens are dead — drop them so the guard sends the user to /login.
    clearSession(res.cookies);
  }

  return res;
}

export {
  handler as GET,
  handler as POST,
  handler as PUT,
  handler as PATCH,
  handler as DELETE,
};
