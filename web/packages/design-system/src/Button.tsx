import type { ButtonHTMLAttributes, ReactNode } from "react";

export type ButtonVariant = "primary" | "secondary" | "danger" | "ghost";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  isLoading?: boolean;
  loadingLabel?: string;
  children: ReactNode;
}

export function Button({
  variant = "primary",
  isLoading = false,
  loadingLabel = "Working",
  children,
  className = "",
  disabled,
  ...props
}: ButtonProps) {
  return (
    <button
      {...props}
      className={`ds-button ds-button--${variant} ${className}`.trim()}
      disabled={disabled || isLoading}
      aria-busy={isLoading}
    >
      {isLoading && <span className="ds-spinner" aria-hidden />}
      <span>{isLoading ? loadingLabel : children}</span>
    </button>
  );
}
