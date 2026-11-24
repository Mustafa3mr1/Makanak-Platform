# Makanak Platform — API Contract & Specification

This document serves as the single source of truth for frontend and mobile engineering teams integrating with the Makanak Platform API.

---

## 1. Global Conventions

- **Transport & Format**: HTTPS, `Content-Type: application/json` (except image upload which uses `multipart/form-data`).
- **Naming Policy**: `camelCase` for all JSON properties.
- **Enums**: Always serialized as **strings** (e.g., `"Apartment"`, `"Sale"`, `"Pending"`), matching Swagger schemas.
- **Monetary Values**: Decimal numbers with 2 fractional digits (e.g., `2500000.00`). Currency is implicitly **EGP** (Egyptian Pound).
- **Date & Time**: ISO 8601 UTC strings (e.g., `"2026-11-24T16:35:26.1234567Z"`).
- **Hybrid ID Pattern**:
  - The internal database surrogate key (`long Id`) is **never** returned or accepted by the API.
  - All routes, DTOs, and JWT claims expose the external identifier `Guid PublicId`.
  - Route constraints use `{publicId:guid}`.
- **Pagination (`PagedResult<T>`)**:
  ```json
  {
    "items": [],
    "page": 1,
    "pageSize": 12,
    "totalCount": 42,
    "totalPages": 4,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
  ```
  - Default `page`: 1, default `pageSize`: 12, max `pageSize`: 50.
- **Optimistic Concurrency**:
  - `PropertyDetailsDto` and `OfferDetailsDto` include a `rowVersion` (base64 string).
  - Mutating operations (e.g. `PUT /api/properties/{publicId}`) accept `rowVersion`.
  - A concurrency conflict returns HTTP 409 with code `concurrency_conflict`.
- **IDOR Protection**:
  - Endpoints accessing private resources (e.g. non-available properties, offer details) return **404 Not Found** (not 403 Forbidden) if the caller is not the owner or a party to the negotiation.

---

## 2. Standard ProblemDetails Error Responses (RFC 7807)

Every client/domain failure returns standard RFC 7807 `application/problem+json` containing a localized `code` key:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "Detailed explanation of the business violation.",
  "instance": "/api/offers",
  "code": "duplicate_active_offer"
}
```

### Machine-Readable Error Codes:
| Code | HTTP Status | Description |
|---|---|---|
| `validation_failed` | 400 | FluentValidation rule failures. Contains additional `"errors"` map. |
| `not_found` | 404 | Resource does not exist, is soft-deleted, or unauthorized (IDOR-safe). |
| `property_not_available` | 400 | Attempted to make an offer on a property that is Pending or Sold. |
| `negotiation_limit_exceeded` | 400 | Attempted to counter beyond the 2-round negotiation limit. |
| `invalid_state_transition` | 400 | Attempted an invalid transition (e.g. accepting an already rejected offer). |
| `duplicate_active_offer` | 400 | Seeker already has an active (Pending/Countered) offer on this property. |
| `own_property_offer` | 400 | Owner attempted to make an offer on their own property. |
| `forbidden_action` | 400 / 403 | Action is not permitted for the caller's role in the current state. |
| `concurrency_conflict` | 409 | Another request modified the resource concurrently (`DbUpdateConcurrencyException`). |
| `unauthorized` | 401 | Missing, malformed, or expired JWT bearer token. |
| `server_error` | 500 | Unhandled internal server exception. |

---

## 3. Endpoints Specification

### 3.1 Authentication & Profile (`/api/auth`, `/api/users`)

#### `POST /api/auth/register`
Registers a new user as `Owner` or `Seeker`. Admin self-registration is rejected.
- **Request Body**:
  ```json
  {
    "fullName": "Ahmed Hassan",
    "email": "ahmed@example.com",
    "password": "Password123!",
    "userType": "Owner", // "Owner" | "Seeker"
    "phoneNumber": "+201001234567" // optional
  }
  ```
- **Response**: `200 OK`
  ```json
  {
    "token": "eyJhbGciOiJIUzI1...",
    "expiresAt": "2026-11-25T16:35:26Z",
    "user": {
      "publicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Ahmed Hassan",
      "email": "ahmed@example.com",
      "userType": "Owner",
      "phoneNumber": "+201001234567"
    }
  }
  ```

#### `POST /api/auth/login`
- **Request Body**:
  ```json
  {
    "email": "ahmed@example.com",
    "password": "Password123!"
  }
  ```
- **Response**: `200 OK` (same shape as register response).

#### `GET /api/users/me` `[Authorize]`
Returns profile of the currently authenticated user.
- **Response**: `200 OK` -> `UserProfileDto`

#### `PUT /api/users/me` `[Authorize]`
Updates full name and phone number.
- **Request Body**:
  ```json
  {
    "fullName": "Ahmed M. Hassan",
    "phoneNumber": "+201099999999"
  }
  ```
- **Response**: `200 OK` -> `UserProfileDto`

---

### 3.2 Properties (`/api/properties`)

#### `GET /api/properties` `[Public]`
Searches publicly available properties.
- **Query Parameters**:
  - `q`: Search string (matches Title, City, or Address)
  - `city`: Egyptian city name (e.g. Cairo, Giza, Alexandria)
  - `propertyType`: Repeatable filter (`Apartment`, `Villa`, `Office`, `Duplex`, `Townhouse`)
  - `listingType`: `Sale` | `Rent`
  - `minPrice`, `maxPrice`: Numeric range
  - `sort`: `newest` (default) | `priceAsc` | `priceDesc`
  - `page`: default 1
  - `pageSize`: default 12 (max 50)
- **Response**: `200 OK` -> `PagedResult<PropertyListItemDto>`

#### `GET /api/properties/{publicId:guid}` `[Public]`
Returns full property details.
- Returns `404 Not Found` if status is Pending/Sold and caller is not the owner.
- **Response**: `200 OK` -> `PropertyDetailsDto`

#### `GET /api/properties/mine` `[Authorize(Owner)]`
Retrieves properties belonging to the authenticated Owner.
- **Query Parameters**: `status` (`Available` | `Pending` | `Sold`), `page`, `pageSize`.
- **Response**: `200 OK` -> `PagedResult<PropertyListItemDto>`

#### `POST /api/properties` `[Authorize(Owner)]`
Creates a new property listing.
- **Request Body**:
  ```json
  {
    "title": "Luxury Apartment overlooking Nile",
    "description": "3-bedroom furnished apartment with panoramic views.",
    "price": 4500000.00,
    "listingType": "Sale",
    "propertyType": "Apartment",
    "city": "Cairo",
    "address": "Zamalek, Gezira St.",
    "bedrooms": 3,
    "bathrooms": 2,
    "areaSqm": 185.00,
    "latitude": 30.0626,
    "longitude": 31.2197,
    "features": ["Balcony", "Elevator", "Security", "AirConditioning"]
  }
  ```
- **Response**: `201 Created` with `Location` header -> `PropertyDetailsDto`

#### `PUT /api/properties/{publicId:guid}` `[Authorize(Owner)]`
Updates an existing listing.
- **Request Body**: Same as Create plus `"status"` (`Available` or `Pending`) and `"rowVersion"`. Note: `Sold` cannot be set via PUT; it is terminal and set only by accepting an offer.
- **Response**: `200 OK` -> `PropertyDetailsDto`

#### `DELETE /api/properties/{publicId:guid}` `[Authorize(Owner)]`
Soft deletes the property listing. Preserves negotiation history.
- **Response**: `204 NoContent`

#### Image Management `[Authorize(Owner)]`
- `POST /api/properties/{publicId}/images`: `multipart/form-data` with up to 10 files (max 5 MB each, JPEG/PNG/WebP with magic byte validation). First uploaded image automatically becomes primary.
- `PUT /api/properties/{publicId}/images/{imageId}/primary`: Promotes image to primary, demoting previous primary.
- `DELETE /api/properties/{publicId}/images/{imageId}`: Deletes image from disk and DB. If primary, next image is auto-promoted.

---

### 3.3 Negotiation Engine & Offers (`/api/offers`)

#### Negotiation Role & State Matrix:
| Offer Status | Round | Property Status | Allowed Owner Actions | Allowed Seeker Actions |
|---|---|---|---|---|
| **Pending** | 1 | Available | `accept`, `counter`, `reject` | `reject` (withdraw) |
| **Countered** | 2 (Final) | Available / Pending | *none* | `accept`, `reject` |
| **Accepted** | — | Sold | *none* | *none* |
| **Rejected** | — | — | *none* | *none* |
| Any | Any | Sold / Deleted | *none* | *none* |

*Note*: The frontend dynamically receives the exact allowed actions for the current user in `allowedActions` array on `OfferDetailsDto`.

#### `GET /api/offers/mine` `[Authorize(Seeker)]`
Paged list of offers submitted by the current seeker.
- **Query Parameters**: `status`, `page`, `pageSize`.
- **Response**: `200 OK` -> `PagedResult<OfferListItemDto>`

#### `GET /api/offers/incoming` `[Authorize(Owner)]`
Paged list of incoming offers across the owner's properties.
- **Query Parameters**: `status`, `propertyPublicId`, `page`, `pageSize`.
- **Response**: `200 OK` -> `PagedResult<OfferListItemDto>`

#### `GET /api/offers/{publicId:guid}` `[Authorize]`
Returns offer details, computed `allowedActions`, and immutable `history` log.
- Returns `404 Not Found` to callers who are neither the seeker nor the owner.
- **Response**: `200 OK` -> `OfferDetailsDto`

#### `POST /api/offers` `[Authorize(Seeker)]`
Submits an initial round-1 offer on an Available property.
- **Request Body**:
  ```json
  {
    "propertyPublicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "initialPrice": 4200000.00
  }
  ```
- **Response**: `201 Created` -> `OfferDetailsDto`

#### `PUT /api/offers/{publicId:guid}/counter` `[Authorize(Owner)]`
Submits counter-offer (Round 2, Final round).
- **Request Body**:
  ```json
  {
    "counterPrice": 4350000.00
  }
  ```
- **Response**: `200 OK` -> `OfferDetailsDto`

#### `PUT /api/offers/{publicId:guid}/accept` `[Authorize]`
Accepts the active offer.
- **Atomically marks property as Sold.**
- **Bulk-rejects all competing pending/countered offers on the same property.**
- **Logs `Accepted` and `Rejected` actions into `NegotiationHistory`.**
- **Response**: `200 OK` -> `OfferDetailsDto`

#### `PUT /api/offers/{publicId:guid}/reject` `[Authorize]`
Rejects or withdraws the offer.
- **Logs `Rejected` action into `NegotiationHistory`.**
- **Response**: `200 OK` -> `OfferDetailsDto`

---

### 3.4 Dashboards (`/api/dashboard`)

#### `GET /api/dashboard/owner` `[Authorize(Roles = "Owner")]`
Returns aggregated performance metrics for the authenticated Owner.
- **Response**: `200 OK`
  ```json
  {
    "totalProperties": 5,
    "availableProperties": 4,
    "pendingOffers": 2,
    "totalOffers": 8
  }
  ```

#### `GET /api/dashboard/seeker` `[Authorize(Roles = "Seeker")]`
Returns negotiation pipeline metrics for the authenticated Seeker.
- **Response**: `200 OK`
  ```json
  {
    "totalOffers": 6,
    "pending": 2,
    "countered": 1,
    "accepted": 1,
    "rejected": 2
  }
  ```

---

## 4. Phase 2 Out-of-Scope Items

The following features were intentionally excluded from Phase 2 scope as approved:
1. Admin management endpoints and dashboards.
2. Favorites / Wishlist / Bookmarking.
3. Property view counter / click tracking.
4. Phone-number login or SMS OTP.
5. JWT Refresh tokens (sliding session expiration).
6. Email confirmation flow.
7. Real-time web socket / SignalR notifications and in-app chat.
