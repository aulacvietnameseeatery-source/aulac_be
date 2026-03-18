using Core.DTO.Shift;

namespace Core.Interface.Service.Shift;

public interface IShiftTemplateService
{
    /// <summary>Returns all templates, optionally filtered by active state.</summary>
    Task<List<ShiftTemplateListDto>> GetAllAsync(bool? isActive = null, CancellationToken ct = default);

    /// <summary>Returns full detail for a single template.</summary>
    Task<ShiftTemplateDetailDto> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Creates a new shift template. Name must be unique.</summary>
    Task<ShiftTemplateDetailDto> CreateAsync(
        CreateShiftTemplateRequest request, long createdByStaffId, CancellationToken ct = default);

    /// <summary>Updates an existing shift template.</summary>
    Task<ShiftTemplateDetailDto> UpdateAsync(
        long id, UpdateShiftTemplateRequest request, long updatedByStaffId, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a template. Throws <c>ConflictException</c> if DRAFT or PUBLISHED schedules
    /// still reference it.
    /// </summary>
    Task DeactivateAsync(long id, CancellationToken ct = default);
}
