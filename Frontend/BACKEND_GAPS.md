# Backend Contract & Visual Design Alignment Gaps

This document tracks any fields or capabilities present in the UI/UX design (`UI_Design.pdf`) but either optional or omitted from the current backend OpenAPI specification.

---

## 1. Geographic Coordinates for Map Pinning
- **Design Requirement:** Property Details screen (Screen 3) features an interactive Leaflet/OpenStreetMap location pin showing the neighborhood location.
- **Backend Model:**
  - `PropertyDetailsDto` has `latitude` and `longitude` (`double?`), but they may be `null` for legacy or unpinned properties.
  - `PropertyListDto` does not include coordinates (only `address`, `city`).
- **Client Strategy:**
  - Map widget conditionally renders only when both `latitude` and `longitude` are present.
  - When absent, falls back to a formatted address pill and neighborhood badge.

---

## 2. Multi-image Gallery vs Single Primary Image
- **Design Requirement:** Property details displays a 5-image gallery/carousel with thumbnail selector.
- **Backend Model:**
  - Current mock data provides placeholder image strings via `images` array or placeholder URLs (`https://placehold.co/800x600?text=Property`).
  - Full multi-file upload is handled in Phase 3/4 via `IFileStorageService`.
- **Client Strategy:**
  - Uses `images` array with placeholder fallback when empty or during offline development.

---

## 3. Real-Time Status Push (WebSockets / SignalR)
- **Design Requirement:** Negotiation cards dynamically display status updates when counter-offers are submitted or accepted.
- **Backend Model:**
  - Current backend is standard REST with RowVersion concurrency detection (HTTP 409).
- **Client Strategy:**
  - TanStack Query polling / invalidation on window focus and optimistic action handlers.
  - 409 conflict automatically maps to user-friendly refresh prompts: *"This item was just updated by another user. Please refresh."*
