using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.LookUpValue
{
    public class LookupValueI18nDto
    {
        public uint ValueId { get; set; }
        public string ValueCode { get; set; } = null!;
        public short SortOrder { get; set; }

        public LookupValueTranslationDto I18n { get; set; } = new();
    }

    public class LookupValueTranslationDto
    {
        public string? Vi { get; set; }
        public string? En { get; set; }
        public string? Fr { get; set; }
    }

}
