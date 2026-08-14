"use client";

import type { UserRole } from "@/lib/types";
import { cn, initials } from "@/lib/utils";
import {
  BookOpen,
  CalendarDays,
  ClipboardList,
  GraduationCap,
  LayoutDashboard,
  Library,
  LogOut,
  Menu,
  Settings,
  UserCheck,
  Users,
  X,
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import { RoleBadge } from "./ui/badge";

export const SCHOOL_NAME = "গাজীপুর ক্যান্টনমেন্ট বোর্ড উচ্চ বিদ্যালয়";
export const SCHOOL_SHORT = "জিসিবিএইচএস";

interface NavItem {
  href: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /** Match only the exact path — for index routes that everything else nests under. */
  exact?: boolean;
}

const NAV: Record<UserRole, NavItem[]> = {
  Admin: [
    { href: "/admin", label: "সারসংক্ষেপ", icon: LayoutDashboard, exact: true },
    { href: "/admin/users", label: "ব্যবহারকারী", icon: Users },
    { href: "/admin/classes", label: "শ্রেণি", icon: Library },
    { href: "/admin/subjects", label: "বিষয়", icon: BookOpen },
    { href: "/admin/courses", label: "কোর্স ও শিক্ষক", icon: GraduationCap },
    { href: "/admin/enrollments", label: "ভর্তি", icon: UserCheck },
    { href: "/admin/routine", label: "রুটিন", icon: CalendarDays },
    { href: "/admin/assignments", label: "অ্যাসাইনমেন্ট", icon: ClipboardList },
    { href: "/admin/settings", label: "সেটিংস", icon: Settings },
  ],
  Teacher: [
    { href: "/teacher", label: "অ্যাসাইনমেন্ট", icon: ClipboardList, exact: true },
    { href: "/teacher/courses", label: "আমার কোর্স", icon: GraduationCap },
    { href: "/teacher/submissions", label: "জমা ও মূল্যায়ন", icon: UserCheck },
    { href: "/teacher/routine", label: "আমার রুটিন", icon: CalendarDays },
  ],
  Student: [
    { href: "/student", label: "অ্যাসাইনমেন্ট", icon: ClipboardList, exact: true },
    { href: "/student/submissions", label: "আমার জমা", icon: BookOpen },
    { href: "/student/routine", label: "ক্লাস রুটিন", icon: CalendarDays },
  ],
};

export function AppShell({
  user,
  children,
}: {
  user: { fullName: string; email: string; role: UserRole };
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);

  const items = NAV[user.role];

  const isActive = (item: NavItem) =>
    item.exact ? pathname === item.href : pathname.startsWith(item.href);

  async function signOut() {
    setSigningOut(true);
    await fetch("/api/auth/logout", { method: "POST" });
    router.replace("/login");
    router.refresh();
  }

  const brand = (
    <div className="flex items-center gap-2.5 px-1">
      <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-primary text-primary-foreground">
        <GraduationCap className="size-5" />
      </div>
      <div className="min-w-0">
        <p className="truncate text-sm font-semibold leading-tight">
          {SCHOOL_NAME}
        </p>
        <p className="text-xs text-muted">অ্যাসাইনমেন্ট ব্যবস্থাপনা</p>
      </div>
    </div>
  );

  const nav = (
    <nav className="flex flex-col gap-1">
      {items.map((item) => {
        const Icon = item.icon;
        return (
          <Link
            key={item.href}
            href={item.href}
            onClick={() => setMenuOpen(false)}
            aria-current={isActive(item) ? "page" : undefined}
            className={cn(
              "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
              isActive(item)
                ? "bg-primary-soft text-primary"
                : "text-muted hover:bg-surface-muted hover:text-foreground",
            )}
          >
            <Icon className="size-4 shrink-0" />
            {item.label}
          </Link>
        );
      })}
    </nav>
  );

  const identity = (
    <div className="flex items-center gap-3 rounded-lg border border-border bg-surface-muted p-3">
      <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-semibold text-primary-foreground">
        {initials(user.fullName)}
      </div>
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{user.fullName}</p>
        <p className="truncate text-xs text-muted">{user.email}</p>
      </div>
    </div>
  );

  return (
    <div className="flex min-h-screen">
      {/* Sidebar — desktop */}
      <aside className="hidden w-64 shrink-0 flex-col justify-between border-r border-border bg-surface p-4 lg:flex">
        <div className="flex flex-col gap-6">
          <div className="flex flex-col gap-2">
            {brand}
            <div className="px-1">
              <RoleBadge role={user.role} />
            </div>
          </div>
          {nav}
        </div>

        <div className="flex flex-col gap-2">
          {identity}
          <button
            onClick={signOut}
            disabled={signingOut}
            className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-muted hover:bg-surface-muted hover:text-danger disabled:opacity-50"
          >
            <LogOut className="size-4" />
            {signingOut ? "সাইন আউট হচ্ছে…" : "সাইন আউট"}
          </button>
        </div>
      </aside>

      {/* Mobile drawer */}
      {menuOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/45 lg:hidden"
          onClick={() => setMenuOpen(false)}
        >
          <aside
            className="flex h-full w-72 flex-col justify-between border-r border-border bg-surface p-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex flex-col gap-6">
              <div className="flex items-start justify-between gap-2">
                {brand}
                <button
                  onClick={() => setMenuOpen(false)}
                  aria-label="মেনু বন্ধ করুন"
                  className="rounded-md p-1 text-muted hover:bg-surface-muted"
                >
                  <X className="size-5" />
                </button>
              </div>
              {nav}
            </div>
            <div className="flex flex-col gap-2">
              {identity}
              <button
                onClick={signOut}
                disabled={signingOut}
                className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-muted hover:bg-surface-muted hover:text-danger"
              >
                <LogOut className="size-4" />
                সাইন আউট
              </button>
            </div>
          </aside>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Top bar — mobile only */}
        <header className="flex items-center justify-between gap-2 border-b border-border bg-surface px-4 py-3 lg:hidden">
          <button
            onClick={() => setMenuOpen(true)}
            aria-label="মেনু খুলুন"
            className="rounded-md p-1.5 text-muted hover:bg-surface-muted"
          >
            <Menu className="size-5" />
          </button>
          <span className="truncate text-sm font-semibold">{SCHOOL_SHORT}</span>
          <RoleBadge role={user.role} />
        </header>

        <main className="min-w-0 flex-1 p-4 sm:p-6">{children}</main>
      </div>
    </div>
  );
}

/** Consistent page title block used at the top of every route. */
export function PageHeader({
  title,
  description,
  action,
}: {
  title: string;
  description?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
      <div className="min-w-0">
        <h1 className="text-xl font-semibold">{title}</h1>
        {description && <p className="mt-1 text-sm text-muted">{description}</p>}
      </div>
      {action}
    </div>
  );
}
