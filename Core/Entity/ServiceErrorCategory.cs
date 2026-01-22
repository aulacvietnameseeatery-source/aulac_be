using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class ServiceErrorCategory
{
    public long CategoryId { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();
}
