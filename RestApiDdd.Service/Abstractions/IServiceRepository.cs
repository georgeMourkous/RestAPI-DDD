using ServiceAggregate = RestApiDdd.Domain.Entities.Service;

namespace RestApiDdd.Service.Abstractions;

public interface IServiceRepository : IRepository<ServiceAggregate>
{
}
