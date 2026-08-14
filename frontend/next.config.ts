import type { NextConfig } from "next";

/**
 * Headers applied to every response the web tier serves.
 *
 * `frame-ancestors 'none'` is carried by both `X-Frame-Options` and a one-directive
 * CSP: the CSP form is the one modern browsers honour, the header is the one older
 * ones do. The CSP is deliberately *only* that directive — a full `script-src`
 * policy needs per-request nonces threaded through Next's inline bootstrap scripts,
 * which is a bigger change than it looks and is listed in the README's known
 * limitations rather than half-done here.
 */
const securityHeaders = [
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "X-Frame-Options", value: "DENY" },
  { key: "Content-Security-Policy", value: "frame-ancestors 'none'" },
  { key: "Referrer-Policy", value: "no-referrer" },
];

const nextConfig: NextConfig = {
  // Emits a self-contained server bundle so the Docker image only needs the
  // Node runtime plus .next/standalone — no node_modules copy.
  output: "standalone",

  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
};

export default nextConfig;
