using Core.DTO.LookUpValue;
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
    private readonly II18nRepository _i18nRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupResolver _lookupResolver;

    private static readonly string[] SupportedLangs = ["vi", "en", "fr"];

    public LookupService(
   ILookupRepo repo,
        II18nRepository i18nRepo,
        IUnitOfWork unitOfWork,
        ILookupResolver lookupResolver)
    {
        _repo = repo;
        _i18nRepo = i18nRepo;
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
            I18n = MapTranslations(v.ValueNameText, v.ValueName),
            DescriptionI18n = MapTranslations(v.ValueDescText, v.Description)
        }).ToList();
    }

    #endregion

    #region ── Commands ──

    /// <inheritdoc />
    public async Task<LookupValueI18nDto> CreateAsync(ushort typeId, CreateLookupValueRequest request, CancellationToken ct = default)
    {
        var isConfigurable = await _repo.IsTypeConfigurableAsync(typeId, ct);
        if (!isConfigurable)
            throw new ValidationException("Cannot add new values to a non-configurable lookup type");

        if (string.IsNullOrWhiteSpace(request.ValueName))
            throw new ValidationException("Value name is required");

        var trimmedName = request.ValueName.Trim();

        var valueCode = !string.IsNullOrWhiteSpace(request.ValueCode)? request.ValueCode.Trim().ToUpperInvariant() : GenerateValueCode(trimmedName);

        if (await _repo.ValueNameExistsAsync(typeId, trimmedName, ct: ct))
            throw new ConflictException($"A value with name '{trimmedName}' already exists");

        if (await _repo.ValueCodeExistsAsync(typeId, valueCode, ct: ct))
            throw new ConflictException($"A value with code '{valueCode}' already exists");

        var sortOrder = request.SortOrder
            ?? (short)(await _repo.GetMaxSortOrderAsync(typeId, ct) + 1);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // Create i18n text for name
            var nameText = await CreateI18nTextAsync(
                    trimmedName, request.I18n, $"lookup_value_name_{valueCode}", ct);

            // Create i18n text for description (optional)
            I18nText? descText = null;
            if (request.DescriptionI18n is not null && HasAnyValue(request.DescriptionI18n))
            {
                var descSource = request.DescriptionI18n.En
              ?? request.DescriptionI18n.Vi
                 ?? request.DescriptionI18n.Fr
             ?? "";
                descText = await CreateI18nTextAsync(
                         descSource, request.DescriptionI18n, $"lookup_value_desc_{valueCode}", ct);
            }

            var entity = new LookupValue
            {
                TypeId = typeId,
                ValueCode = valueCode,
                ValueName = trimmedName,
                SortOrder = sortOrder,
                Description = descText is not null ? (request.DescriptionI18n?.En ?? request.DescriptionI18n?.Vi ?? ""): null,
                IsActive = true,
                IsSystem = false,
                Locked = false,
                ValueNameTextId = nameText.TextId,
                ValueDescTextId = descText?.TextId,
                UpdateAt = DateTime.UtcNow
            };

            _repo.Add(entity);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);
            await _lookupResolver.RefreshIfChangedAsync(ct);

            // Re-fetch with i18n for accurate DTO
            var saved = await _repo.GetByIdWithI18nAsync(entity.ValueId, ct);
            return ToDto(saved ?? entity);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
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

        // Update i18n name translations if provided
        if (request.I18n is not null)
        {
            if (entity.ValueNameTextId.HasValue)
            {
                await ReplaceI18nTranslationsAsync(entity.ValueNameTextId.Value, request.I18n, entity.ValueName, ct);
            }
            else
            {
                var nameText = await CreateI18nTextAsync(entity.ValueName, request.I18n, $"lookup_value_name_{entity.ValueCode}", ct);
                entity.ValueNameTextId = nameText.TextId;
            }
        }

        // Update i18n description translations if provided
        if (request.DescriptionI18n is not null)
        {
            var descSource = request.DescriptionI18n.En
                      ?? request.DescriptionI18n.Vi
            ?? request.DescriptionI18n.Fr;
            entity.Description = descSource;

            if (entity.ValueDescTextId.HasValue)
            {
                await ReplaceI18nTranslationsAsync(entity.ValueDescTextId.Value, request.DescriptionI18n, descSource ?? "", ct);
            }
            else if (HasAnyValue(request.DescriptionI18n))
            {
                var descText = await CreateI18nTextAsync(descSource ?? "", request.DescriptionI18n, $"lookup_value_desc_{entity.ValueCode}", ct);
                entity.ValueDescTextId = descText.TextId;
            }
        }

        entity.UpdateAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await _lookupResolver.RefreshIfChangedAsync(ct);

        // Re-fetch with i18n for accurate DTO
        var updated = await _repo.GetByIdWithI18nAsync(valueId, ct);
        return ToDto(updated ?? entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ushort typeId, uint valueId, string typeLabel, CancellationToken ct = default)
    {
        var isConfigurable = await _repo.IsTypeConfigurableAsync(typeId, ct);
        if (!isConfigurable)
            throw new ValidationException($"Cannot delete values from a non-configurable {typeLabel} type");

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

    /// <summary>
    /// Creates a new I18nText with translations from a LookupI18nMap.
    /// Falls back to <paramref name="sourceText"/> for any locale not provided.
    /// </summary>
    private async Task<I18nText> CreateI18nTextAsync(
        string sourceText, LookupI18nMap? i18nMap, string context, CancellationToken ct)
    {
        var sourceLang = "en";
        var text = await _i18nRepo.CreateTextAsync(sourceLang, sourceText, context, ct);

        if (i18nMap is null) return text;

        var translations = BuildTranslationDictionary(i18nMap, sourceLang);
        if (translations.Count > 0)
            await _i18nRepo.AddTranslationsAsync(text.TextId, translations, sourceLang, ct);

        return text;
    }

    /// <summary>
    /// Replaces all translations for an existing I18nText with the new map.
    /// </summary>
    private async Task ReplaceI18nTranslationsAsync(
        long textId, LookupI18nMap i18nMap, string sourceText, CancellationToken ct)
    {
        var existing = await _i18nRepo.GetTextWithTranslationsAsync(textId, ct);
        if (existing is null) return;

        // Update source text
        existing.SourceText = sourceText;
        existing.UpdatedAt = DateTime.UtcNow;

        // Build new translation set
        var newTranslations = new Dictionary<string, string>();
        if (i18nMap.Vi is not null) newTranslations["vi"] = i18nMap.Vi;
        if (i18nMap.En is not null) newTranslations["en"] = i18nMap.En;
        if (i18nMap.Fr is not null) newTranslations["fr"] = i18nMap.Fr;

        // Update existing or add new translations
        foreach (var lang in SupportedLangs)
        {
            var existingTranslation = existing.I18nTranslations.FirstOrDefault(t => t.LangCode == lang);
            if (newTranslations.TryGetValue(lang, out var newValue))
            {
                if (existingTranslation is not null)
                {
                    existingTranslation.TranslatedText = newValue;
                    existingTranslation.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    await _i18nRepo.AddTranslationAsync(new I18nTranslation
                    {
                        TextId = textId,
                        LangCode = lang,
                        TranslatedText = newValue,
                        UpdatedAt = DateTime.UtcNow
                    }, ct);
                }
            }
        }
    }

    private static Dictionary<string, string> BuildTranslationDictionary(LookupI18nMap map, string sourceLang)
    {
        var dict = new Dictionary<string, string>();
        if (map.Vi is not null) dict["vi"] = map.Vi;
        if (map.En is not null) dict["en"] = map.En;
        if (map.Fr is not null) dict["fr"] = map.Fr;
        return dict;
    }

    private static bool HasAnyValue(LookupI18nMap? map) =>
      map is not null && (map.Vi is not null || map.En is not null || map.Fr is not null);

    private static LookupValueI18nDto ToDto(LookupValue entity) => new()
    {
        ValueId = entity.ValueId,
        ValueCode = entity.ValueCode,
        SortOrder = entity.SortOrder,
        I18n = MapTranslations(entity.ValueNameText, entity.ValueName),
        DescriptionI18n = MapTranslations(entity.ValueDescText, entity.Description)
    };

    private static LookupValueTranslationDto MapTranslations(I18nText? text, string? fallback)
    {
        if (text is null)
        {
            return fallback is not null
                  ? new LookupValueTranslationDto { Vi = fallback, En = fallback, Fr = fallback }
                          : new LookupValueTranslationDto();
        }

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

        var code = Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC),@"[^a-zA-Z0-9]+", "_")
            .Trim('_')
            .ToUpperInvariant();

        return string.IsNullOrWhiteSpace(code) ? "VALUE" : code;
    }

    #endregion
}
