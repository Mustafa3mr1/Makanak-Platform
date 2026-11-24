import React from 'react';

export type BadgeVariant = 'sale' | 'rent' | 'pending' | 'success' | 'warning' | 'error' | 'neutral';

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  size?: 'sm' | 'md';
}

export const Badge: React.FC<BadgeProps> = ({
  children,
  variant = 'neutral',
  size = 'md',
  className = '',
  ...props
}) => {
  const variants: Record<BadgeVariant, string> = {
    sale: 'bg-primary-soft text-primary-dark border-transparent',
    rent: 'bg-success-soft text-success-text border-transparent',
    pending: 'bg-warning-soft text-warning-text border-transparent',
    success: 'bg-success-soft text-success-text border-transparent',
    warning: 'bg-warning-soft text-warning-text border-transparent',
    error: 'bg-error-soft text-error-text border-transparent',
    neutral: 'bg-secondary-light text-text-secondary border-line',
  };

  const sizes = {
    sm: 'px-2 py-0.5 text-[11px] font-semibold',
    md: 'px-3 py-1 text-xs font-semibold',
  };

  return (
    <span
      className={`inline-flex items-center justify-center rounded-full border leading-none tracking-wide ${variants[variant]} ${sizes[size]} ${className}`}
      {...props}
    >
      {children}
    </span>
  );
};
