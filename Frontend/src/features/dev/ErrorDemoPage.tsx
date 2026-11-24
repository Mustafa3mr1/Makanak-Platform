import React, { useState } from 'react';
import { Card, Button, Badge, useToast } from '@/shared/ui';
import { ProblemDetails } from '@/shared/api/types';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { Link } from 'react-router-dom';

const sampleErrors: Record<string, { status: number; problem: ProblemDetails; explanation: string }> = {
  validation_failed: {
    status: 400,
    explanation: 'FluentValidation failure returned by .NET backend.',
    problem: {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
      title: 'One or more validation errors occurred.',
      status: 400,
      code: 'validation_failed',
      errors: {
        Price: ['The Price field must be greater than 0.'],
        Bedrooms: ['Bedrooms count must be at least 1.'],
      },
    },
  },
  duplicate_active_offer: {
    status: 400,
    explanation: 'Domain invariant: Seeker already has a pending offer on this property.',
    problem: {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
      title: 'Active offer already exists',
      status: 400,
      detail: 'You already have an active offer on this property. You cannot submit another.',
      code: 'duplicate_active_offer',
    },
  },
  negotiation_limit_exceeded: {
    status: 400,
    explanation: 'Domain invariant: Strict 2-round engine reached maximum counters.',
    problem: {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
      title: 'Negotiation limit exceeded',
      detail: 'Cannot counter: negotiation is already at Round 2 (maximum allowed).',
      code: 'negotiation_limit_exceeded',
    },
  },
  not_found: {
    status: 404,
    explanation: 'IDOR-safe: unauthorized or non-existent entity returns 404.',
    problem: {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4',
      title: 'Resource not found',
      detail: 'The requested property or offer could not be found.',
      code: 'not_found',
    },
  },
  concurrency_conflict: {
    status: 409,
    explanation: 'RowVersion mismatch (DbUpdateConcurrencyException) mapped to HTTP 409.',
    problem: {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
      title: 'Conflict',
      detail: 'The record was modified by another user.',
      code: 'concurrency_conflict',
    },
  },
};

export const ErrorDemoPage: React.FC = () => {
  const { error, warning } = useToast();
  const [selectedKey, setSelectedKey] = useState<string>('concurrency_conflict');

  const current = sampleErrors[selectedKey];

  const handleSimulateToast = () => {
    if (current.status === 409) {
      warning('Update Conflict (409)', 'This item was just updated by another user. Please refresh.');
    } else {
      error(
        current.problem.title || 'Error',
        current.problem.detail || (current.problem.errors ? JSON.stringify(current.problem.errors) : 'An error occurred.')
      );
    }
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-10 space-y-8">
      <div className="flex items-center justify-between pb-4 border-b border-slate-200">
        <div>
          <h1 className="text-2xl font-extrabold text-slate-900">
            RFC 7807 ProblemDetails Error Simulator
          </h1>
          <p className="text-sm text-slate-500">
            Tests client handling of standardized HTTP errors and concurrency conflicts (409)
          </p>
        </div>
        <Link to="/_kit">
          <Button variant="secondary" size="sm">
            ← Back to UI Kit
          </Button>
        </Link>
      </div>

      <div className="flex flex-wrap gap-2">
        {Object.keys(sampleErrors).map((key) => (
          <button
            key={key}
            onClick={() => setSelectedKey(key)}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors ${
              selectedKey === key
                ? 'bg-brand-primary text-white shadow-soft'
                : 'bg-white border border-slate-200 text-slate-700 hover:bg-slate-50'
            }`}
          >
            {key} ({sampleErrors[key].status})
          </button>
        ))}
      </div>

      {/* Details Box */}
      <Card className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge variant={current.status === 409 ? 'warning' : 'error'}>
              HTTP {current.status}
            </Badge>
            <span className="font-mono text-xs text-slate-500">code: {current.problem.code}</span>
          </div>
          <Button
            variant="secondary"
            size="sm"
            leftIcon={<AlertCircle className="w-4 h-4" />}
            onClick={handleSimulateToast}
          >
            Trigger Toast Mapping
          </Button>
        </div>

        <p className="text-sm text-slate-600 bg-slate-50 p-3 rounded-lg border border-slate-100">
          <strong>Behavior:</strong> {current.explanation}
        </p>

        {/* 409 Banner preview if concurrency */}
        {current.status === 409 && (
          <div className="p-4 rounded-xl bg-amber-50 border border-amber-200 text-amber-900 flex items-start gap-3">
            <RefreshCw className="w-5 h-5 flex-shrink-0 mt-0.5 text-amber-600 animate-spin" />
            <div className="flex-1">
              <h4 className="text-sm font-bold">This item was just updated by another user.</h4>
              <p className="text-xs text-amber-700 mt-1">
                Please refresh the page to see the latest round price and action state.
              </p>
            </div>
            <Button variant="secondary" size="sm" onClick={() => window.location.reload()}>
              Refresh
            </Button>
          </div>
        )}

        <div>
          <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2">
            Raw ProblemDetails JSON:
          </h4>
          <pre className="p-4 bg-slate-900 text-emerald-400 rounded-xl text-xs font-mono overflow-x-auto">
            {JSON.stringify(current.problem, null, 2)}
          </pre>
        </div>
      </Card>
    </div>
  );
};
