using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Customer
{
    public class CustomerDto
    {
        public long CustomerId { get; set; }
        public string? FullName { get; set; }
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public bool? IsMember { get; set; }
        public int? LoyaltyPoints { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
