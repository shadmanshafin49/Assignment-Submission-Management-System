import { NextRequest, NextResponse } from "next/server";
import { API_BASE_URL, REFRESH_COOKIE, clearSession } from "@/lib/session";

export const dynamic = "force-dynamic";

export async function POST(req: NextRequest) {
  const refreshToken = req.cookies.get(REFRESH_COOKIE)?.value;

  // Best-effort revocation server-side; the cookies get cleared either way so
  // a failing API call can never leave the user stuck signed in.
  if (refreshToken) {
    try {
      await fetch(`${API_BASE_URL}/api/auth/logout`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
        cache: "no-store",
      });
    } catch {
      // ignored — see above
    }
  }

  const res = NextResponse.json({ ok: true });
  clearSession(res.cookies);
  return res;
}
