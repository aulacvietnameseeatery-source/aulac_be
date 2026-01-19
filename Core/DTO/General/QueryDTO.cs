using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.General
{
    public class QueryDTO
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string Columns { get; set; } = null!;
        public string? ColumnsSummary { get; set; }

        public string? Filter { get; set; }
        public object? QuickSearch { get; set; }
        public string? Sort { get; set; }

        public Dictionary<string, object>? CustomParam { get; set; }
        public string? CustomFilter { get; set; }

        public bool UseSp { get; set; }
    }

}
