using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class I18nTranslation
{
    public long TextId { get; set; }

    public string LangCode { get; set; } = null!;

    public string TranslatedText { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public virtual I18nLanguage LangCodeNavigation { get; set; } = null!;

    public virtual I18nText Text { get; set; } = null!;
}
