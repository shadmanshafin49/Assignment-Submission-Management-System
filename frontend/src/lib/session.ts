import { cookies } from "next/headers";
import { readClaims, type TokenClaims } from "./jwt";
import type { LoginResponse } from "./types";

export const ACCESS_COOKIE = "ams_at";
export const REFRESH_COOKIE = "ams_rt";

/**
 * Server-side base URL for the API. Inside docker-compose this is the service
 * name (`http://api:8080`); locally it is the Kestrel port. The browser never
 * uses this — it talks to the Next proxy at /api/proxy instead.
 */
export const API_BASE_URL =
  process.env.API_BASE_URL?.replace(/\/$/, "") ?? "http://localhost:5140";

const isProd = process.env.NODE_ENV === "production";

const baseCookie = {
  httpOnly: true,
  sameSite: "lax",
  secure: isProd,
  path: "/",
} as const;

type CookieJar = {
  set: (name: string, value: string, options?: Record<string, unknown>) => void;
  delete?: (name: string) => void;
};

/**
 * Tokens live in httpOnly cookies so no XSS payload can read them. The access
 * cookie's lifetime tracks the JWT; the refresh cookie outlives it so an
 * expired access token can be exchanged silently.
 */
export function writeSession(jar: CookieJar, auth: LoginResponse) {
  const accessMaxAge = Math.max(
    60,
    Math.floor((new Date(auth.accessTokenExpiresAt).getTime() - Date.now()) / 1000),
  );

  jar.set(ACCESS_COOKIE, auth.accessToken, {
    ...baseCookie,
    maxAge: accessMaxAge,
  });
  jar.set(REFRESH_COOKIE, auth.refreshToken, {
    ...baseCookie,
    maxAge: 60 * 60 * 24 * 7,
  });
}

export function clearSession(jar: CookieJar) {
  jar.set(ACCESS_COOKIE, "", { ...baseCookie, maxAge: 0 });
  jar.set(REFRESH_COOKIE, "", { ...baseCookie, maxAge: 0 });
}

export async function getAccessToken(): Promise<string | undefined> {
  return (await cookies()).get(ACCESS_COOKIE)?.value;
}

export async function getRefreshToken(): Promise<string | undefined> {
  return (await cookies()).get(REFRESH_COOKIE)?.value;
}

/** The signed-in user as far as the cookie knows. `null` when signed out. */
export async function getSessionUser(): Promise<TokenClaims | null> {
  return readClaims(await getAccessToken());
}

/**
 * Server Component data fetching: calls the API directly with the cookie's
 * bearer token, no proxy hop. Throws on non-2xx so error.tsx can catch it.
 */
export async function serverFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const token = await getAccessToken();

  const res = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`API ${res.status} on ${path}`);
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
