import React, { useState } from 'react';
import {
  Button,
  Input,
  Select,
  Badge,
  Card,
  Modal,
  Tabs,
  Pagination,
  CardSkeleton,
  EmptyState,
  useToast,
} from '@/shared/ui';
import { formatEgp, formatArea, formatDate } from '@/shared/lib/formatters';
import { Search, Send } from 'lucide-react';
import { Link } from 'react-router-dom';

export const UiKitDemoPage: React.FC = () => {
  const { success, error, warning, info } = useToast();
  const [modalOpen, setModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState('tab1');
  const [page, setPage] = useState(1);
  const [inputValue, setInputValue] = useState('');
  const [selectValue, setSelectValue] = useState('option1');

  return (
    <div className="max-w-6xl mx-auto px-4 py-10 space-y-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between pb-6 border-b border-slate-200 gap-4">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-900">Makanak UI Kit</h1>
          <p className="text-sm text-slate-500 mt-1">
            Design tokens, primitives, accessible interactive components & state showcases
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Link to="/_kit/error-demo">
            <Button variant="secondary" size="sm">
              RFC 7807 Error Demo →
            </Button>
          </Link>
        </div>
      </div>

      {/* Buttons */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold text-slate-900">1. Buttons & Actions</h2>
        <Card className="flex flex-wrap items-center gap-4">
          <Button variant="primary">Primary</Button>
          <Button variant="secondary">Secondary</Button>
          <Button variant="text">Text Button</Button>
          <Button variant="danger">Danger</Button>
          <Button variant="primary" isLoading>
            Loading
          </Button>
          <Button variant="primary" leftIcon={<Send className="w-4 h-4" />}>
            With Icon
          </Button>
          <Button variant="secondary" size="sm">
            Small
          </Button>
          <Button variant="secondary" size="lg">
            Large
          </Button>
        </Card>
      </section>

      {/* Form Controls */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold text-slate-900">2. Form Controls</h2>
        <Card className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Input
            label="Search Property"
            placeholder="e.g. New Cairo, Maadi"
            leftIcon={<Search className="w-4 h-4 text-slate-400" />}
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
          />
          <Input
            label="Password Input"
            isPassword
            placeholder="••••••••"
            helperText="At least 6 characters"
          />
          <Input
            label="With Validation Error"
            defaultValue="invalid-email"
            error="Please enter a valid email address"
          />
          <Select
            label="Listing Type"
            value={selectValue}
            onChange={(e) => setSelectValue(e.target.value)}
            options={[
              { label: 'Sale', value: 'option1' },
              { label: 'Rent', value: 'option2' },
            ]}
          />
          <Select
            label="Property Status"
            options={[
              { label: 'Available', value: 'avail' },
              { label: 'Pending', value: 'pend' },
              { label: 'Sold', value: 'sold' },
            ]}
          />
        </Card>
      </section>

      {/* Badges & Formatters */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold text-slate-900">3. Badges & Currency/Area Formatting</h2>
        <Card className="space-y-4">
          <div className="flex flex-wrap gap-3">
            <Badge variant="sale">Sale</Badge>
            <Badge variant="rent">Rent</Badge>
            <Badge variant="pending">Pending Offer</Badge>
            <Badge variant="success">Available</Badge>
            <Badge variant="warning">Countered</Badge>
            <Badge variant="error">Sold / Rented</Badge>
            <Badge variant="neutral">Draft</Badge>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 border-t border-slate-100 text-sm">
            <div>
              <span className="text-slate-400 block text-xs">EGP Currency:</span>
              <strong className="text-brand-primary text-lg">{formatEgp(3500000)}</strong>
            </div>
            <div>
              <span className="text-slate-400 block text-xs">Area Format:</span>
              <strong className="text-slate-800 text-lg">{formatArea(185)}</strong>
            </div>
            <div>
              <span className="text-slate-400 block text-xs">Date Format:</span>
              <strong className="text-slate-800 text-lg">{formatDate(new Date().toISOString())}</strong>
            </div>
          </div>
        </Card>
      </section>

      {/* Tabs & Pagination */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold text-slate-900">4. Tabs & Pagination</h2>
        <Card className="space-y-6">
          <Tabs
            activeTab={activeTab}
            onChange={setActiveTab}
            tabs={[
              { id: 'tab1', label: 'All Offers', count: 12 },
              { id: 'tab2', label: 'Pending Counters', count: 3 },
              { id: 'tab3', label: 'Finalized', count: 9 },
            ]}
          />
          <p className="text-xs text-slate-500">Currently showing: {activeTab}</p>
          <Pagination currentPage={page} totalPages={8} onPageChange={setPage} />
        </Card>
      </section>

      {/* Toasts & Modal */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold text-slate-900">5. Toasts & Modal Dialog</h2>
        <Card className="flex flex-wrap gap-4">
          <Button variant="primary" onClick={() => setModalOpen(true)}>
            Open Sample Modal
          </Button>
          <Button variant="secondary" onClick={() => success('Offer Accepted!', 'Counter offer has been approved.')}>
            Success Toast
          </Button>
          <Button variant="danger" onClick={() => error('Action Failed', 'Round limit exceeded.')}>
            Error Toast
          </Button>
          <Button variant="secondary" onClick={() => warning('Attention', 'This counter offer is final.')}>
            Warning Toast
          </Button>
          <Button variant="text" onClick={() => info('Information', 'Property status updated to Pending.')}>
            Info Toast
          </Button>
        </Card>
      </section>

      {/* Skeletons */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold text-slate-900">6. Skeletons & Empty State</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <CardSkeleton />
          <EmptyState
            title="No properties listed yet"
            description="You have not added any properties to your portfolio. Start by clicking the button below."
            actionLabel="Add Property"
            onAction={() => alert('Add Property clicked')}
          />
        </div>
      </section>

      {/* Modal Dialog */}
      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Submit Counter Offer"
        description="This will be Round 2 of 2. After this step, no further counter offers are allowed."
        footer={
          <>
            <Button variant="text" onClick={() => setModalOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={() => {
                setModalOpen(false);
                success('Counter Offer Submitted', 'Seeker will review your final price.');
              }}
            >
              Confirm Counter
            </Button>
          </>
        }
      >
        <div className="space-y-4 py-2">
          <p className="text-sm text-slate-600">
            Current offer is <strong>{formatEgp(2400000)}</strong>. Enter your proposed final price:
          </p>
          <Input label="Proposed Counter Price (EGP)" type="number" placeholder="2550000" />
        </div>
      </Modal>
    </div>
  );
};
