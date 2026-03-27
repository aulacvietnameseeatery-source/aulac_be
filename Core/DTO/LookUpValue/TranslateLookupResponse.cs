using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.LookUpValue
{
    public class TranslateLookupResponse
    {
        public Dictionary<string, LookupDto> Translations { get; set; } = new();
    }
}
