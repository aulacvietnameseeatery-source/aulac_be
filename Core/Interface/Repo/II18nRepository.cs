using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface II18nRepository
    {
        Task<I18nText> CreateTextAsync(
        string sourceLang,
        string sourceText,
        string context,
        CancellationToken ct);

        Task AddTranslationsAsync(
            long textId,
            Dictionary<string, string> translations,
            string sourceLang,
            CancellationToken ct);

        Task AddAsync(I18nText text, CancellationToken ct);
    }
}
