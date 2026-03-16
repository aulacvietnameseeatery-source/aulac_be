using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Customer
{
    public class CustomerListDTO
    {
        public long CustomerId { get; set; }

        public string? FullName { get; set; }

        public string Phone { get; set; } = null!;

        public string? Email { get; set; }

        public bool? IsMember { get; set; }

        public int? LoyaltyPoints { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int OrderCount { get; set; }

        public int ReservationCount { get; set; }

        public DateTime? LastOrderTime { get; set; }
    }

    public class CustomerListQueryDTO
    {
        public string? Search { get; set; }

        public bool? IsMember { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
