import { cva, type VariantProps } from "class-variance-authority";
import { LoaderCircle } from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";

const buttonVariants = cva("ds-button", {
  variants: {
    variant: {
      primary: "ds-button--primary",
      secondary: "ds-button--secondary",
      danger: "ds-button--danger",
      ghost: "ds-button--ghost",
    },
    size: {
      default: "ds-button--default",
      sm: "ds-button--sm",
      icon: "ds-button--icon",
    },
  },
  defaultVariants: { variant: "primary", size: "default" },
});

export type ButtonVariant = NonNullable<
  VariantProps<typeof buttonVariants>["variant"]
>;

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: VariantProps<typeof buttonVariants>["size"];
  isLoading?: boolean;
  loadingLabel?: string;
  children: ReactNode;
}

export function Button({
  variant = "primary",
  size = "default",
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
      className={`${buttonVariants({ variant, size })} ${className}`.trim()}
      disabled={disabled || isLoading}
      aria-busy={isLoading}
    >
      {isLoading && <LoaderCircle className="ds-spinner" aria-hidden />}
      <span>{isLoading ? loadingLabel : children}</span>
    </button>
  );
}
