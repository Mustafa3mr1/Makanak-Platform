# Makanak — Design Notes

## 1. ERD differences / schema extensions
| Change | Reason |
|---|---|
| `Property.PropertyType` (Apartment/Villa/Office), `Property.ListingType` (Sale/Rent) | Required by filtering/listing requirements; absent from the ERD. Stored as strings. |
| `ApplicationUser.IsActive` | Admin deactivation/reactivation without deleting data. |
| `ApplicationUser` inherits `IdentityUser<Guid>` | Email/PasswordHash/Id come from Identity (no duplicate storage). Table is `AspNetUsers`. |
| Filtered unique index `UX_PropertyImages_OnePrimaryPerProperty` | One primary image per property (`WHERE IsPrimary = 1`). |
| Filtered unique index `UX_Offers_OneOpenOfferPerSeekerPerProperty` | One open negotiation per seeker per property (blocks duplicate/racing submissions). |
| Check constraints on Price, CurrentPrice (> 0), RoundCount (≥ 1) | Data integrity. |
| `UpdatedAtUtc` maintained in `MakanakDbContext.SaveChanges` | Consistent timestamps. |

All IDs, attributes and the six relationships follow the ERD. Foreign keys to users and Property→Offer, Offer→NegotiationHistory use **Restrict**; only Property→PropertyImage cascades.

## 2. Enums
- **UserType**: BuyerTenant, Owner, Admin (each has an identically named Identity role).
- **PropertyStatus**: Available, Sold, Rented, Inactive.
- **OfferStatus**: PendingOwnerResponse (owner's turn), PendingBuyerResponse (buyer's turn), Accepted, Rejected, Closed (another offer won).
- **NegotiationActionType**: InitialOffer, OwnerAccepted, OwnerRejected, OwnerCounterOffer, BuyerAccepted, BuyerRejected, BuyerCounterOffer, ClosedBecausePropertyUnavailable.
- JSON enums are strings; numeric values are rejected (`allowIntegerValues: false`); validators use `IsInEnum()` for query-string enums.

## 3. Offer state machine
```
submit (buyer) ─▶ PendingOwnerResponse ──owner counter──▶ PendingBuyerResponse ──buyer counter──▶ PendingOwnerResponse …
PendingOwnerResponse ──owner accept/reject──▶ Accepted / Rejected
PendingBuyerResponse ──buyer accept/reject──▶ Accepted / Rejected
any open state ──another offer accepted──▶ Closed
Accepted, Rejected, Closed are terminal (no endpoint reopens them).
```
- **RoundCount** = number of price proposals: 1 for the initial offer, +1 per counter-offer (max 10).
- Counter-offers reuse `Offer.CurrentPrice` + `NegotiationHistory`; there is no CounterOffer table.
- Accept/reject records the price that was accepted/rejected in `ProposedPrice`.

## 4. Concurrency strategy (accepting an offer)
`OfferService.AcceptAsync` runs in one SQL transaction:
1. Load the offer; check participant and state (right party, offer open).
2. **Claim the property** with a single conditional statement  
   `UPDATE Properties SET Status = 'Sold'|'Rented' WHERE Id = @id AND OwnerId = @owner AND Status = 'Available'`.  
   SQL Server takes an exclusive row lock, so simultaneous acceptances serialize on the Property row; only the caller that sees *1 row affected* continues. Everyone else gets **409**.
3. Mark the offer Accepted and append history. `Offer.RowVersion` (rowversion) makes a concurrent counter/reject on the same offer fail with 409.
4. Close every other open offer on the property (`Closed` + history entry) — nothing is deleted.
5. Commit. Any exception rolls back everything.

`Property.RowVersion` additionally protects owner edits (clients send the RowVersion from their last GET; a stale one → 409). Two limitations are documented: an offer submitted in the instant between a sale's commit and the next read can remain open on a non-available property (it can never be accepted — step 2 fails) and the property-level check on counter-offers is a read, not a lock.
Not verified: no SQL Server was available, so these guarantees are **designed, not demonstrated**.

## 5. Deletion & retention policy
- Properties are never physically deleted. `DELETE /api/properties/{id}` sets `Inactive` (owner or admin). Sold/Rented properties cannot be removed.
- Inactive properties are hidden from public search/detail (visible to owner and admin).
- Users are never deleted; admin deactivation blocks login and invalidates existing tokens on the next request, and sets the owner's Available listings to Inactive. Reactivation does not re-list.
- Offers and negotiation history are retained. History is append-only: the DbContext throws on any attempt to modify/delete it, no endpoint exposes it, and the FK is Restrict.

## 6. Security notes
- Roles: Identity roles are assigned at registration from `UserType`; the JWT `role` claim is derived server-side. Login fails closed if `UserType` and the role ever diverge. No endpoint changes roles.
- Admin cannot be self-registered (validator + service check); the first admin comes from user-secrets/environment seeding.
- OwnerId / SeekerId / actor ids always come from the validated token (`sub`), never from request bodies.
- JWT: HMAC-SHA256, issuer/audience/lifetime validated, 1-minute skew, key length enforced at startup, no refresh tokens.
- Lockout: 5 failed logins → 15 minutes.
- Images: content sniffed by magic bytes (JPEG/PNG/WebP only), 5 MB limit, 10 per property, server-generated file names, path-traversal guard, `nosniff` header, files stored on disk (URL in DB). If the DB write fails the file is deleted; file deletion happens after DB commit.
- Responses use ProblemDetails; unexpected errors return a generic 500 and are logged.

## 7. Search
`GET /api/properties` builds an `IQueryable` (Available only) with keyword (title/description/city/address), city, min/max price, property type, listing type; sort by CreatedAt/Price/Title (Asc/Desc) with `Id` tiebreaker; max page size 50; returns total count and pages; projection DTOs, `AsNoTracking`.
