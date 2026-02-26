using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Table
{
    public class TableManagementDto
    {
        public long TableId { get; set; }
        public string TableCode { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsOnline { get; set; }

        // Trạng thái 
        public uint StatusId { get; set; }
        public string StatusCode { get; set; } = string.Empty; 
        public string StatusName { get; set; } = string.Empty;

        // Loại bàn (Regular, Booth, VIP...)
        public uint TypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;

        // Khu vực (Indoor, Outdoor...)
        public uint ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
    }
}
