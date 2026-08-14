import { cn } from "@/lib/utils";
import { Loader2 } from "lucide-react";
import type { ButtonHTMLAttributes } from "react";

type Variant = "primary" | "secondary" | "ghost" | "danger" | "success";
type Size = "sm" | "md";

const VARIANTS: Record<Variant, string> = {
  primary:
    "bg-primary text-primary-foreground hover:bg-primary-hover border-transparent",
  secondary:
    "bg-surface text-foreground hover:bg-surface-muted border-border",
  ghost:
    "bg-transparent text-muted hover:text-foreground hover:bg-surface-muted border-transparent",
  danger: "bg-danger text-white hover:opacity-90 border-transparent",
  success: "bg-success text-white hover:opacity-90 border-transparent",
};

const SIZES: Record<Size, string> = {
  sm: "h-8 px-3 text-xs gap-1.5",
  md: "h-10 px-4 text-sm gap-2",
};

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
}

export function Button({
  variant = "primary",
  size = "md",
  loading = false,
  disabled,
  className,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      // A loading button stays disabled so a double-click cannot double-submit.
      disabled={disabled || loading}
      className={cn(
        "inline-flex items-center justify-center rounded-lg border font-medium",
        "transition-colors disabled:pointer-events-none disabled:opacity-50",
        VARIANTS[variant],
        SIZES[size],
        className,
      )}
      {...props}
    >
      {loading && <Loader2 className="size-4 animate-spin" aria-hidden />}
      {children}
    </button>
  );
}
