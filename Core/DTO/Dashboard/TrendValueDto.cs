using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dashboard
{
    public class TrendValueDto
    {
        public decimal Value { get; set; }
        public decimal Trend { get; set; }
        public bool IsUp { get; set; }
    }
}
