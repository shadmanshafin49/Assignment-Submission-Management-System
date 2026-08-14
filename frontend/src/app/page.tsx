import { ROLE_HOME } from "@/lib/jwt";
import { getSessionUser } from "@/lib/session";
import { redirect } from "next/navigation";

/**
 * The middleware normally redirects "/" before this renders; this is the
 * server-side fallback so the root is never a dead end.
 */
export default async function RootPage() {
  const claims = await getSessionUser();
  redirect(claims ? ROLE_HOME[claims.role] : "/login");
}
