import { NextRequest, NextResponse } from "next/server";
import { API_BASE_URL, writeSession } from "@/lib/session";
import type { LoginResponse } from "@/lib/types";

export const dynamic = "force-dynamic";

export async function POST(req: NextRequest) {
  const credentials = await req.text();

  let upstream: Response;
  try {
    upstream = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: credentials,
      cache: "no-store",
    });
  } catch {
    return NextResponse.json(
      { title: "Cannot reach the API. Is the backend running?", status: 502 },
      { status: 502 },
    );
  }

  if (!upstream.ok) {
    const problem = await upstream.text();
    return new NextResponse(problem || null, {
      status: upstream.status,
      headers: {
        "Content-Type":
          upstream.headers.get("content-type") ?? "application/json",
      },
    });
  }

  const auth = (await upstream.json()) as LoginResponse;

  // Only the user object crosses back to the browser — the tokens stay in
  // httpOnly cookies set here.
  const res = NextResponse.json({ user: auth.user });
  writeSession(res.cookies, auth);
  return res;
}
