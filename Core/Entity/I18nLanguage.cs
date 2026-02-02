using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class I18nLanguage
{
    public string LangCode { get; set; } = null!;

    public string LangName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<I18nText> I18nTexts { get; set; } = new List<I18nText>();

    public virtual ICollection<I18nTranslation> I18nTranslations { get; set; } = new List<I18nTranslation>();
}
