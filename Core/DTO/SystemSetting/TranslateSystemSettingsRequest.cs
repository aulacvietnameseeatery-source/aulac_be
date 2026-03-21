using System.Collections.Generic;

namespace Core.DTO.SystemSetting
{
    public class TranslateSystemSettingsRequest
    {
        public string SourceLang { get; set; } = null!;
        public Dictionary<string, string> Data { get; set; } = new();
    }
}
