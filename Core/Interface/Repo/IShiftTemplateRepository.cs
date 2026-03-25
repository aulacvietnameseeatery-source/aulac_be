using Core.Entity;

namespace Core.Interface.Repo;

public interface IShiftTemplateRepository
{
    void Add(ShiftTemplate entity);

    Task<ShiftTemplate?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<List<ShiftTemplate>> GetAllActiveAsync(CancellationToken ct = default);

    Task<List<ShiftTemplate>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns true if another template (excluding <paramref name="excludeId"/>) already uses the given name.</summary>
    Task<bool> ExistsByNameAsync(string templateName, long? excludeId = null, CancellationToken ct = default);

    /// <summary>Returns true if any active shift assignments reference this template.</summary>
    Task<bool> HasActiveAssignmentsAsync(long templateId, CancellationToken ct = default);
}
