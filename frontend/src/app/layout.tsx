import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "./providers";

export const metadata: Metadata = {
  title: "অ্যাসাইনমেন্ট ব্যবস্থাপনা — গাজীপুর ক্যান্টনমেন্ট বোর্ড উচ্চ বিদ্যালয়",
  description:
    "ষষ্ঠ থেকে অষ্টম শ্রেণির অ্যাসাইনমেন্ট, জমা ও মূল্যায়ন ব্যবস্থাপনা।",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  // lang="bn" is load-bearing, not decoration: it picks the Bengali font stack
  // and gives Intl the right locale for dates and relative times.
  return (
    <html lang="bn">
      <body className="min-h-screen antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
