using RestApiDdd.Domain.Common;

namespace RestApiDdd.Service.Abstractions;

public interface IReadOnlyRepository<TEntity>
    where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
