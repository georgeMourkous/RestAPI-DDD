using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Entities;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class PackageRepository : EfRepository<Package>, IPackageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PackageRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Package?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Packages
            .Include(package => package.Frequencies)
            .Include(package => package.Services)
            .FirstOrDefaultAsync(package => package.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Package>> ListWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Packages
            .AsNoTracking()
            .Include(package => package.Frequencies)
            .Include(package => package.Services)
            .OrderBy(package => package.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludedPackageId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();

        return await _dbContext.Packages
            .AnyAsync(package =>
                package.Name == normalizedName
                && (!excludedPackageId.HasValue || package.Id != excludedPackageId.Value),
                cancellationToken);
    }
}
