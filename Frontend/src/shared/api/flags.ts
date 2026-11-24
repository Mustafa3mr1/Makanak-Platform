export interface ApiFeatureFlags {
  useMockAuth: boolean;
  useMockProperties: boolean;
  useMockOffers: boolean;
  useMockDashboard: boolean;
  useMockAdmin: boolean;
}

// Single source of truth for toggling real backend vs client mock implementations
export const apiFlags: ApiFeatureFlags = {
  useMockAuth: false,
  useMockProperties: false,
  useMockOffers: false,
  useMockDashboard: false,
  useMockAdmin: true, // Admin is mock-only as agreed in architecture decisions
};
