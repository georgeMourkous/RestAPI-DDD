using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Entities;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class PackageRepository : EfRepository<Package>, IPackageRepository
{
    private const string PackageCategoriesCacheKey = "package-categories:list";
    private static readonly TimeSpan PackageCategoriesCacheDuration = TimeSpan.FromMinutes(30);

    private readonly ApplicationDbContext _dbContext;
    private readonly ICacheProvider _cacheProvider;

    public PackageRepository(ApplicationDbContext dbContext, ICacheProvider cacheProvider)
        : base(dbContext)
    {
        _dbContext = dbContext;
        _cacheProvider = cacheProvider;
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

    public async Task<PackageCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var categories = await ListCategoriesAsync(cancellationToken);
        return categories.FirstOrDefault(category => category.Id == id);
    }

    public async Task<IReadOnlyList<PackageCategory>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheProvider.GetOrCreateAsync(
            PackageCategoriesCacheKey,
            async token => await _dbContext.PackageCategories
                .AsNoTracking()
                .OrderBy(category => category.SortOrder)
                .ThenBy(category => category.Name)
                .ToListAsync(token),
            PackageCategoriesCacheDuration,
            cancellationToken) ?? [];
    }

    public async Task<bool> CategoryExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetCategoryByIdAsync(id, cancellationToken) is not null;
    }

    public async Task AddCategoryAsync(PackageCategory category, CancellationToken cancellationToken = default)
    {
        await _dbContext.PackageCategories.AddAsync(category, cancellationToken);
        InvalidatePackageCategoryCache();
    }

    public void UpdateCategory(PackageCategory category)
    {
        _dbContext.PackageCategories.Update(category);
        InvalidatePackageCategoryCache();
    }

    public void RemoveCategory(PackageCategory category)
    {
        _dbContext.PackageCategories.Remove(category);
        InvalidatePackageCategoryCache();
    }

    private void InvalidatePackageCategoryCache()
    {
        _cacheProvider.Remove(PackageCategoriesCacheKey);
    }
}
