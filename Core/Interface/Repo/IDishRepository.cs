using Core.Entity;

namespace Core.Interface.Repo;

public interface IDishRepository
{
    Task<Dish?> GetDishByIdAsync(long dishId, CancellationToken cancellationToken = default);
}
