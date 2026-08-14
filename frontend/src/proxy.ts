import { NextRequest, NextResponse } from "next/server";
import { ROLE_HOME, readClaims } from "@/lib/jwt";
import { ACCESS_COOKIE } from "@/lib/session";
import type { UserRole } from "@/lib/types";

/** Route prefix → roles permitted to see it. */
const ROUTE_ROLES: [prefix: string, roles: UserRole[]][] = [
  ["/admin", ["Admin"]],
  ["/teacher", ["Teacher"]],
  ["/student", ["Student"]],
];

/**
 * Role-aware routing guard. (Next.js 16 renamed `middleware` to `proxy`; this is
 * the same edge-run guard under its current name.)
 *
 * This is a UX layer, not a security boundary — it decides which page to show,
 * while the API independently enforces every rule on every request. A user who
 * forges a cookie just reaches a page whose data calls all return 403.
 */
export function proxy(req: NextRequest) {
  const { pathname } = req.nextUrl;
  const claims = readClaims(req.cookies.get(ACCESS_COOKIE)?.value);

  // Signed in and back at /login? Send them to their dashboard.
  if (pathname === "/login") {
    if (claims) {
      return NextResponse.redirect(new URL(ROLE_HOME[claims.role], req.url));
    }
    return NextResponse.next();
  }

  if (!claims) {
    const login = new URL("/login", req.url);
    // Preserve where they were headed so login can bounce them back.
    if (pathname !== "/") login.searchParams.set("next", pathname);
    return NextResponse.redirect(login);
  }

  if (pathname === "/") {
    return NextResponse.redirect(new URL(ROLE_HOME[claims.role], req.url));
  }

  const rule = ROUTE_ROLES.find(([prefix]) => pathname.startsWith(prefix));
  if (rule && !rule[1].includes(claims.role)) {
    return NextResponse.redirect(new URL(ROLE_HOME[claims.role], req.url));
  }

  return NextResponse.next();
}

export const config = {
  // Everything except Next internals, the route handlers, and static assets.
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|.*\\.svg).*)"],
};
