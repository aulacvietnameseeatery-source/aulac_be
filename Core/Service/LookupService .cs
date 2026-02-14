using Core.DTO.LookUpValue;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.LookUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class LookupService : ILookupService
    {
        private readonly ILookupRepo _repo;

        public LookupService(ILookupRepo repo)
        {
            _repo = repo;
        }

        public async Task<List<LookupValueI18nDto>> GetAllActiveByTypeAsync(
            ushort typeId,
            CancellationToken ct)
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

        private LookupValueTranslationDto MapTranslations(I18nText? text)
        {
            if (text == null)
                return new LookupValueTranslationDto();

            var translations = text.I18nTranslations;

            return new LookupValueTranslationDto
            {
                Vi = translations
                        .FirstOrDefault(x => x.LangCode == "vi")
                        ?.TranslatedText ?? text.SourceText,

                En = translations
                        .FirstOrDefault(x => x.LangCode == "en")
                        ?.TranslatedText ?? text.SourceText,

                Fr = translations
                        .FirstOrDefault(x => x.LangCode == "fr")
                        ?.TranslatedText ?? text.SourceText
            };
        }
    }

}
