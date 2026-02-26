using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Table
{
    public class GetTableManagementRequest
    {
        // Phân trang
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 30;

        // Bộ lọc từ giao diện
        public string? Search { get; set; }
        public uint? ZoneId { get; set; }
        public uint? TypeId { get; set; }
        public uint? StatusId { get; set; }
        public bool? IsOnline { get; set; }
    }
}
