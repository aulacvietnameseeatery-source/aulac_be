using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.LookUpValue
{
    public class TranslateLookupRequest
    {
        public string SourceLang { get; set; } = null!;
        public LookupDto Data { get; set; } = null!;
    }

    public class LookupDto
    {
        public string ValueName { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
