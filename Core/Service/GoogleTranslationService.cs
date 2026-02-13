using Core.Data;
using Core.DTO.Dish;
using Core.DTO.Google;
using Core.Interface.Service.Others;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class GoogleTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleTranslateOptions _options;

        public GoogleTranslationService(
            HttpClient httpClient,
            IOptions<GoogleTranslateOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<Dictionary<string, DishI18nDto>> TranslateDishAsync(
            string sourceLang,
            DishI18nDto sourceData)
        {
            var targetLangs = GetTargetLanguages(sourceLang);

            // 1️⃣ Gom field thành array
            var fields = new List<string?>
        {
            sourceData.DishName,
            sourceData.Description,
            sourceData.Slogan,
            sourceData.Note,
            sourceData.ShortDescription
        };

            // Replace null bằng empty để giữ index
            var normalized = fields.Select(x => x ?? string.Empty).ToList();

            // 2️⃣ Gọi song song 2 API call
            var tasks = targetLangs
                .Select(lang => TranslateBatch(normalized, sourceLang, lang))
                .ToList();

            var results = await Task.WhenAll(tasks);

            var response = new Dictionary<string, DishI18nDto>();

            for (int i = 0; i < targetLangs.Count; i++)
            {
                var translatedList = results[i];

                response[targetLangs[i]] = new DishI18nDto
                {
                    DishName = translatedList[0],
                    Description = translatedList[1],
                    Slogan = translatedList[2],
                    Note = translatedList[3],
                    ShortDescription = translatedList[4]
                };
            }

            return response;
        }

        private async Task<List<string>> TranslateBatch(
            List<string> texts,
            string source,
            string target)
        {
            var url = $"https://translation.googleapis.com/language/translate/v2?key={_options.ApiKey}";

            var body = new
            {
                q = texts,
                source = source,
                target = target,
                format = "text"
            };

            var response = await _httpClient.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GoogleResponse>();

            return result!.Data.Translations
                .Select(x => x.TranslatedText)
                .ToList();
        }

        private List<string> GetTargetLanguages(string source)
        {
            var all = new List<string> { "vi", "en", "fr" };
            return all.Where(x => x != source).ToList();
        }
    }

}
