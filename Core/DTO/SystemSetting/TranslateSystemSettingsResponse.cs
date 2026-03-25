using System.Collections.Generic;

namespace Core.DTO.SystemSetting
{
    public class TranslateSystemSettingsResponse
    {
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; } = new();
    }
}
