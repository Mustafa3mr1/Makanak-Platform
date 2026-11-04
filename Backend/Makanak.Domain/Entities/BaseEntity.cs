namespace Makanak.Domain.Entities;

/// <summary>
/// Hybrid ID base entity.
/// - <see cref="Id"/>       : Internal clustered PK (long) — used only inside DB for FK joins and indexes.
/// - <see cref="PublicId"/> : External GUID — used in every API route/DTO; prevents IDOR attacks.
/// No external NuGet dependency; EF Core index is configured via IEntityTypeConfiguration.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Internal auto-increment primary key. Never exposed in API responses or routes.</summary>
    public long Id { get; private set; }

    /// <summary>
    /// External public identifier. Globally unique, indexed, non-sequential.
    /// All API endpoints, DTOs, and foreign key references in the API surface use this value.
    /// </summary>
    public Guid PublicId { get; private set; } = Guid.NewGuid();

    // EF Core parameterless constructor support (for hydration from DB).
    protected BaseEntity() { }
}

