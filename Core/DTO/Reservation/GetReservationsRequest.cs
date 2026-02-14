using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class GetReservationsRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; } // Tìm theo tên, SĐT

        public DateTime? Date { get; set; } // Lọc theo ngày cụ thể (Today, Tomorrow...)

        public long? StatusId { get; set; } // Lọc theo Tab (Pending, Confirmed...)
    }
}
