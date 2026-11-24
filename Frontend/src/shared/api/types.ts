import type { components, paths } from './generated-types';

export type Schemas = components['schemas'];

// Domain Enums
export type PropertyStatus = Schemas['PropertyStatus'];
export type PropertyType = Schemas['PropertyType'];
export type ListingType = Schemas['ListingType'];
export type OfferStatus = Schemas['OfferStatus'];
export type UserType = Schemas['UserType'];

// Auth DTOs
export type RegisterRequestDto = Schemas['RegisterRequestDto'];
export type LoginRequestDto = Schemas['LoginRequestDto'];
export type AuthResponseDto = Schemas['AuthResponseDto'];
export type UserProfileDto = Schemas['UserProfileDto'];
export type UpdateUserProfileRequestDto = Schemas['UpdateUserProfileRequestDto'];

// Property DTOs
export type PropertyDetailsDto = Schemas['PropertyDetailsDto'];
export type PropertyListItemDto = Schemas['PropertyListItemDto'];
export type PropertyListItemDtoPagedResult = Schemas['PropertyListItemDtoPagedResult'];
export type PropertyImageItemDto = Schemas['PropertyImageItemDto'];
export type CreatePropertyRequestDto = Schemas['CreatePropertyRequestDto'];
export type UpdatePropertyRequestDto = Schemas['UpdatePropertyRequestDto'];

// Offer & Negotiation DTOs
export type OfferDetailsDto = Schemas['OfferDetailsDto'];
export type OfferListItemDto = Schemas['OfferListItemDto'];
export type OfferListItemDtoPagedResult = Schemas['OfferListItemDtoPagedResult'];
export type NegotiationHistoryItemDto = Schemas['NegotiationHistoryItemDto'];
export type CreateOfferRequestDto = Schemas['CreateOfferRequestDto'];
export type CounterOfferRequestDto = Schemas['CounterOfferRequestDto'];

// Dashboard DTOs
export type OwnerDashboardDto = Schemas['OwnerDashboardDto'];
export type SeekerDashboardDto = Schemas['SeekerDashboardDto'];

// RFC 7807 ProblemDetails
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export { paths };
