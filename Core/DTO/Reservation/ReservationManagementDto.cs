using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class ReservationManagementDto
    {
        /// <summary>
        /// DTO hiển thị danh sách đặt bàn cho quản lý (Admin/Staff)
        /// </summary>
        public long ReservationId { get; set; }

        // Thời gian đặt (Gồm cả ngày và giờ để hiển thị cột Time)
        public DateTime ReservedTime { get; set; }

        // Thông tin khách hàng
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }

        // Số lượng khách (Pax)
        public int Pax { get; set; }

        // Trạng thái (ID để map màu, Name để hiển thị)
        public long StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;

        // Pre-order: Hiện tại chưa có món cụ thể, có thể để boolean hoặc string mô tả ngắn
        public string? PreOrderSummary { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
