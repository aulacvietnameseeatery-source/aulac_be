using Core.DTO.LookUpValue;

namespace Core.Interface.Service.LookUp;

public interface ILookupService
{
    /// <summary>
    /// Gets all active lookup values for the given type, ordered by sort order.
    /// Returns full i18n and descriptionI18n maps.
    /// </summary>
    Task<List<LookupValueI18nDto>> GetAllActiveByTypeAsync(ushort typeId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new lookup value for the given type.
    /// Auto-generates <c>ValueCode</c> from <c>ValueName</c> (SCREAMING_SNAKE_CASE) when not supplied.
    /// Persists i18n and descriptionI18n translations.
    /// Throws <c>ConflictException</c> on duplicate name or code.
    /// </summary>
    Task<LookupValueI18nDto> CreateAsync(ushort typeId, CreateLookupValueRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing lookup value. Validates it belongs to <paramref name="typeId"/>.
    /// Replaces i18n and descriptionI18n maps when provided.
    /// Throws <c>ValidationException</c> if the value is locked.
    /// Throws <c>ConflictException</c> on duplicate name.
    /// </summary>
    Task<LookupValueI18nDto> UpdateAsync(ushort typeId, uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a lookup value. Validates it belongs to <paramref name="typeId"/> and is not locked.
    /// Throws <c>ConflictException</c> if non-deleted tables still reference it.
    /// </summary>
    Task DeleteAsync(ushort typeId, uint valueId, string typeLabel, CancellationToken ct = default);
}
