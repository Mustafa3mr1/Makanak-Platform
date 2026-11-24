import React from 'react';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  hoverable?: boolean;
}

export const Card: React.FC<CardProps> = ({ children, hoverable = false, className = '', ...props }) => {
  return (
    <div
      className={`bg-surface border border-line rounded-card p-5 shadow-soft transition-all duration-200
        ${hoverable ? 'hover:-translate-y-1 hover:shadow-hover' : ''}
        ${className}`}
      {...props}
    >
      {children}
    </div>
  );
};
