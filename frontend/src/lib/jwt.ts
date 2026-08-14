import { decodeJwt } from "jose";
import type { UserRole } from "./types";

/**
 * ASP.NET Core emits role/name as the long WS-Federation claim URIs rather than
 * short names, so we read those exact keys.
 */
const ROLE_CLAIM =
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
const NAME_CLAIM =
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

export interface TokenClaims {
  userId: string;
  email: string;
  fullName: string;
  role: UserRole;
  expiresAt: number;
}

/**
 * Decodes without verifying the signature — deliberate. This is only used for
 * client-side routing decisions; the API verifies every token on every request,
 * so a forged cookie buys nothing but a redirect to a page that returns 403.
 */
export function readClaims(token: string | undefined): TokenClaims | null {
  if (!token) return null;

  try {
    const payload = decodeJwt(token);
    const role = payload[ROLE_CLAIM] as UserRole | undefined;
    if (!role) return null;

    return {
      userId: String(payload.sub ?? ""),
      email: String(payload.email ?? ""),
      fullName: String(payload[NAME_CLAIM] ?? ""),
      role,
      expiresAt: Number(payload.exp ?? 0) * 1000,
    };
  } catch {
    return null;
  }
}

export function isExpired(claims: TokenClaims | null): boolean {
  return !claims || claims.expiresAt <= Date.now();
}

/** Where each role lands after login, and what they get bounced to. */
export const ROLE_HOME: Record<UserRole, string> = {
  Admin: "/admin",
  Teacher: "/teacher",
  Student: "/student",
};
