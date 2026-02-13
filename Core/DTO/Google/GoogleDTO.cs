using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Google
{
    public class GoogleResponse
    {
        public GoogleData Data { get; set; } = null!;
    }

    public class GoogleData
    {
        public List<GoogleTranslation> Translations { get; set; } = new();
    }

    public class GoogleTranslation
    {
        public string TranslatedText { get; set; } = null!;
    }

}
