using Core.Entity;

namespace Core.Extensions;

/// <summary>
/// Extension methods for I18nText to support multi-language translation
/// </summary>
public static class I18nTextExtensions
{
    /// <summary>
    /// Get translated text for specified language code
    /// </summary>
    /// <param name="text">I18nText entity</param>
    /// <param name="langCode">Language code (e.g., "en", "vi")</param>
    /// <returns>Translated text if available, otherwise source text</returns>
    public static string GetTranslation(this I18nText? text, string langCode)
    {
        if (text == null)
            return string.Empty;

        // If requested language is the source language, return source text
        if (text.SourceLangCode.Equals(langCode, StringComparison.OrdinalIgnoreCase))
            return text.SourceText;

        // Find translation for requested language
        var translation = text.I18nTranslations
            .FirstOrDefault(t => t.LangCode.Equals(langCode, StringComparison.OrdinalIgnoreCase));

        // Return translated text if found, otherwise fallback to source text
        return translation?.TranslatedText ?? text.SourceText;
    }
}
