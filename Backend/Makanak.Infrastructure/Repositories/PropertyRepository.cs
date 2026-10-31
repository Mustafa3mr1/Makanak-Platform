using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Infrastructure.Repositories;

public class PropertyRepository : GenericRepository<Property>, IPropertyRepository
{
    public PropertyRepository(MakanakDbContext context) : base(context) { }

    public async Task ClearPrimaryImagesAsync(Guid propertyId)
        => await Context.PropertyImages
            .Where(i => i.PropertyId == propertyId && i.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsPrimary, false));

    public async Task<bool> SetPrimaryImageAsync(Guid propertyId, Guid imageId)
    {
        var rows = await Context.PropertyImages
            .Where(i => i.Id == imageId && i.PropertyId == propertyId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsPrimary, true));
        return rows == 1;
    }
}
