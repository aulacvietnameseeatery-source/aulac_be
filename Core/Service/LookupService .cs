using Core.DTO.LookUpValue;
using Core.DTO.Table;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.LookUp;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Service;

public class LookupService : ILookupService
{
    private readonly ILookupRepo _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupResolver _lookupResolver;

    public LookupService(ILookupRepo repo, IUnitOfWork unitOfWork, ILookupResolver lookupResolver)
    {
        _repo = repo;
     _unitOfWork = unitOfWork;
        _lookupResolver = lookupResolver;
    }

    #region ── Queries ──

    /// <inheritdoc />
    public async Task<List<LookupValueI18nDto>> GetAllActiveByTypeAsync(ushort typeId, CancellationToken ct = default)
    {
        var values = await _repo.GetAllActiveByTypeAsync(typeId, ct);

        return values.Select(v => new LookupValueI18nDto
        {
            ValueId = v.ValueId,
         ValueCode = v.ValueCode,
        SortOrder = v.SortOrder,
    I18n = MapTranslations(v.ValueNameText)
        }).ToList();
    }

    #endregion

    #region ── Commands ──

    /// <inheritdoc />
    public async Task<LookupValueI18nDto> CreateAsync(ushort typeId, CreateLookupValueRequest request, CancellationToken ct = default)
  {
        if (string.IsNullOrWhiteSpace(request.ValueName))
       throw new ValidationException("Value name is required");

        var valueCode = !string.IsNullOrWhiteSpace(request.ValueCode)
     ? request.ValueCode.Trim().ToUpperInvariant()
  : GenerateValueCode(request.ValueName);

        if (await _repo.ValueNameExistsAsync(typeId, request.ValueName.Trim(), ct: ct))
        throw new ConflictException($"A value with name '{request.ValueName}' already exists");

        if (await _repo.ValueCodeExistsAsync(typeId, valueCode, ct: ct))
   throw new ConflictException($"A value with code '{valueCode}' already exists");

        var sortOrder = request.SortOrder
    ?? (short)(await _repo.GetMaxSortOrderAsync(typeId, ct) + 1);

        var entity = new LookupValue
        {
            TypeId = typeId,
   ValueCode = valueCode,
        ValueName = request.ValueName.Trim(),
   SortOrder = sortOrder,
            Description = request.Description?.Trim(),
            IsActive = true,
  IsSystem = false,
   Locked = false,
          UpdateAt = DateTime.UtcNow
    };

        _repo.Add(entity);
        await _unitOfWork.SaveChangesAsync(ct);
        await _lookupResolver.RefreshIfChangedAsync(ct);

return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<LookupValueI18nDto> UpdateAsync(ushort typeId, uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default)
 {
        var entity = await _repo.GetByIdAsync(valueId, ct)
    ?? throw new NotFoundException("Lookup value not found");

        if (entity.TypeId != typeId)
          throw new ValidationException("Lookup value does not belong to the expected type");

      if (entity.Locked == true)
            throw new ValidationException("This lookup value is locked and cannot be modified");

        if (request.ValueName is not null)
        {
         var trimmed = request.ValueName.Trim();
 if (string.IsNullOrWhiteSpace(trimmed))
      throw new ValidationException("Value name cannot be blank");

       if (await _repo.ValueNameExistsAsync(typeId, trimmed, excludeId: valueId, ct: ct))
     throw new ConflictException($"A value with name '{trimmed}' already exists");

        entity.ValueName = trimmed;
        }

        if (request.SortOrder.HasValue)
         entity.SortOrder = request.SortOrder.Value;

        if (request.Description is not null)
     entity.Description = request.Description.Trim();

        entity.UpdateAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _lookupResolver.RefreshIfChangedAsync(ct);

  return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ushort typeId, uint valueId, string typeLabel, CancellationToken ct = default)
    {
     var entity = await _repo.GetByIdAsync(valueId, ct)
         ?? throw new NotFoundException($"{typeLabel} not found");

if (entity.TypeId != typeId)
            throw new ValidationException($"Lookup value does not belong to the expected {typeLabel} type");

        if (entity.Locked == true)
         throw new ValidationException($"This {typeLabel} is locked and cannot be deleted");

        var tableCount = await _repo.CountTablesUsingLookupValueAsync(valueId, ct);
        if (tableCount > 0)
  throw new ConflictException($"Cannot delete {typeLabel}: it is used by {tableCount} table(s)");

        entity.DeletedAt = DateTime.UtcNow;
        entity.IsActive = false;
   entity.UpdateAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _lookupResolver.RefreshIfChangedAsync(ct);
    }

    #endregion

    #region ── Private helpers ──

    private static LookupValueI18nDto ToDto(LookupValue entity) => new()
    {
        ValueId = entity.ValueId,
        ValueCode = entity.ValueCode,
        SortOrder = entity.SortOrder,
        I18n = new LookupValueTranslationDto
        {
            Vi = entity.ValueName,
    En = entity.ValueName,
            Fr = entity.ValueName
      }
    };

    private static LookupValueTranslationDto MapTranslations(I18nText? text)
    {
        if (text is null)
            return new LookupValueTranslationDto();

var translations = text.I18nTranslations;

        return new LookupValueTranslationDto
        {
            Vi = translations.FirstOrDefault(x => x.LangCode == "vi")?.TranslatedText ?? text.SourceText,
            En = translations.FirstOrDefault(x => x.LangCode == "en")?.TranslatedText ?? text.SourceText,
     Fr = translations.FirstOrDefault(x => x.LangCode == "fr")?.TranslatedText ?? text.SourceText
 };
  }

    /// <summary>
    /// Converts a display name to SCREAMING_SNAKE_CASE, stripping diacritics.
    /// E.g. "Garden View" → "GARDEN_VIEW", "Phòng VIP" → "PHONG_VIP"
    /// </summary>
    internal static string GenerateValueCode(string valueName)
    {
        var normalized = valueName.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
  if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
      sb.Append(c);
   }

        var code = Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"[^a-zA-Z0-9]+", "_")
    .Trim('_')
             .ToUpperInvariant();

 return string.IsNullOrWhiteSpace(code) ? "VALUE" : code;
    }

    #endregion
}
