import type { ProblemDetails } from "./types";

/**
 * Thrown for any non-2xx response. Carries the parsed RFC 7807 body so forms
 * can surface the API's own validation messages instead of a generic error.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(
      problem?.detail ||
        problem?.title ||
        `Request failed with status ${status}`,
    );
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** Flattens `errors: { Field: ["msg"] }` into one readable line. */
  get fieldMessages(): string[] {
    if (!this.problem?.errors) return [];
    return Object.entries(this.problem.errors).flatMap(([field, msgs]) =>
      msgs.map((m) => (field === "" ? m : `${field}: ${m}`)),
    );
  }
}

/** Turns any thrown value into one line fit for a toast. */
export function describeError(err: unknown): string {
  if (err instanceof ApiError) {
    return [err.message, ...err.fieldMessages].join(" ");
  }
  return "কিছু একটা ভুল হয়েছে। আবার চেষ্টা করুন।";
}

const PROXY = "/api/proxy";

/** Reads the RFC 7807 body off a failed response and throws it as an ApiError. */
async function fail(res: Response): Promise<never> {
  let problem: ProblemDetails | null = null;
  try {
    problem = (await res.json()) as ProblemDetails;
  } catch {
    // non-JSON error body — leave problem null
  }

  // The proxy already cleared the cookies; a reload lands on /login.
  if (res.status === 401 && typeof window !== "undefined") {
    window.location.href = "/login";
  }
  throw new ApiError(res.status, problem);
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
  const res = await fetch(`${PROXY}${path}`, {
    method,
    headers: body !== undefined ? { "Content-Type": "application/json" } : {},
    body: body !== undefined ? JSON.stringify(body) : undefined,
    credentials: "same-origin",
  });

  if (!res.ok) return fail(res);

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/**
 * Multipart POST for attachments. `Content-Type` is deliberately left unset —
 * the browser has to write it itself so the multipart boundary matches the body.
 */
async function upload<T>(path: string, form: FormData): Promise<T> {
  const res = await fetch(`${PROXY}${path}`, {
    method: "POST",
    body: form,
    credentials: "same-origin",
  });

  if (!res.ok) return fail(res);

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/**
 * Downloads an attachment through the proxy and hands it to the browser as a
 * save. It cannot be a plain `<a href>`: the bearer token lives in an httpOnly
 * cookie that the proxy exchanges, and a 404 has to surface as a message rather
 * than as a browser error page.
 */
async function download(path: string, fileName: string): Promise<void> {
  const res = await fetch(`${PROXY}${path}`, { credentials: "same-origin" });
  if (!res.ok) return fail(res);

  const blob = await res.blob();
  const url = URL.createObjectURL(blob);

  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();

  // Revoked on the next tick: revoking synchronously races the download in Safari.
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/**
 * Appends only the params that are actually set. `false` is a meaningful value
 * (`?isActive=false`), so only `undefined`/`null`/`""` are dropped.
 */
export function qs(
  params: Record<string, string | number | boolean | undefined | null>,
) {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  }
  const s = search.toString();
  return s ? `?${s}` : "";
}

export const api = {
  get: <T>(path: string) => request<T>("GET", path),
  post: <T>(path: string, body?: unknown) => request<T>("POST", path, body),
  put: <T>(path: string, body?: unknown) => request<T>("PUT", path, body),
  delete: <T>(path: string) => request<T>("DELETE", path),
  upload: <T>(path: string, form: FormData) => upload<T>(path, form),
  download,
};
