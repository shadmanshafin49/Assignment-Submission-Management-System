import { AppShell } from "@/components/app-shell";
import { ReferenceProvider } from "@/lib/reference";
import { getSessionUser, serverFetch } from "@/lib/session";
import type { ReferenceDataDto } from "@/lib/types";
import { redirect } from "next/navigation";

/**
 * Shared shell for every signed-in route. The route guard already redirects
 * anonymous users, but this re-checks server-side so the layout never has to
 * render against a null user.
 *
 * The reference payload — every Bangla enum label and the school's bell times —
 * is fetched here once per navigation and handed down, so no badge deep in the
 * tree needs a loading state to name a submission status.
 */
export default async function AppLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const claims = await getSessionUser();
  if (!claims) redirect("/login");

  const reference = await serverFetch<ReferenceDataDto>("/api/reference");

  return (
    <ReferenceProvider value={reference}>
      <AppShell
        user={{
          fullName: claims.fullName,
          email: claims.email,
          role: claims.role,
        }}
      >
        {children}
      </AppShell>
    </ReferenceProvider>
  );
}
