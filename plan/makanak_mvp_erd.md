# Makanak MVP — Entity Relationship Diagram

> Generated strictly from MVP scope entities only.
> All FKs are `Guid`. Crow's foot notation via Mermaid `erDiagram`.

```mermaid
erDiagram

    ApplicationUser {
        Guid Id PK
        string FullName
        string Email
        string PasswordHash
        UserType UserType
        DateTime CreatedAtUtc
    }

    Property {
        Guid Id PK
        Guid OwnerId FK
        string Title
        string Description
        decimal Price
        PropertyStatus Status
        string City
        string Address
        byte[] RowVersion
        DateTime CreatedAtUtc
        DateTime UpdatedAtUtc
    }

    PropertyImage {
        Guid Id PK
        Guid PropertyId FK
        string ImageUrl
        bool IsPrimary
    }

    Offer {
        Guid Id PK
        Guid PropertyId FK
        Guid SeekerId FK
        decimal CurrentPrice
        OfferStatus Status
        int RoundCount
        byte[] RowVersion
        DateTime CreatedAtUtc
        DateTime UpdatedAtUtc
    }

    NegotiationHistory {
        Guid Id PK
        Guid OfferId FK
        Guid ActionByUserId FK
        NegotiationActionType ActionType
        decimal ProposedPrice
        DateTime Timestamp
    }

    %% ── Relationships ────────────────────────────────────────────────────────

    ApplicationUser ||--o{ Property          : "owns (OwnerId)"
    ApplicationUser ||--o{ Offer             : "submits (SeekerId)"
    ApplicationUser ||--o{ NegotiationHistory: "acts on (ActionByUserId)"

    Property        ||--o{ PropertyImage     : "has images (PropertyId)"
    Property        ||--o{ Offer             : "receives offers (PropertyId)"

    Offer           ||--o{ NegotiationHistory: "tracked by (OfferId)"
```

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| `Property.City` + `Property.Address` | Replaces `Point Location` (NetTopologySuite removed from MVP) |
| `PropertyImage.IsPrimary` | Enables single-query primary image loading via EF filtered `Include` |
| `NegotiationHistory` — `DeleteBehavior.Restrict` | Immutable audit log — rows must never cascade-delete |
| `RowVersion` on `Property` and `Offer` | Optimistic concurrency control (NFR-03) |
| `NegotiationHistory.ActionByUserId` FK → `ApplicationUser` | Supports both Owner and Seeker as actors |
| Max 2 `RoundCount` enforced in `Offer.Counter()` domain method | Business invariant lives in the Domain layer, not the DB |
