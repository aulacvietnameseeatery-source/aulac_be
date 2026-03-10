using Core.Entity;

namespace Core.Interface.Repo;

public interface ILoginActivityRepository
{
    void Add(LoginActivity entity);

    Task<List<LoginActivity>> GetByStaffIdAsync(long staffId, int limit = 50, CancellationToken ct = default);
}
