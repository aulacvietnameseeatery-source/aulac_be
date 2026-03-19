using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Customer
{
    public class CustomerOrderDTO
    {
        public long OrderId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal? TipAmount { get; set; }

        public string OrderType { get; set; } = null!;

        public string Status { get; set; } = null!;
    }

    public class CustomerOrderQueryDTO
    {
        public long CustomerId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public uint? OrderTypeLvId { get; set; }
        public OrderSourceCode? OrderType { get; set; }

        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
