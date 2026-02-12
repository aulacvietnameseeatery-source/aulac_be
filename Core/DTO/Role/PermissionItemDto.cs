using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Role
{
    public class PermissionItemDto
    {
        public long PermissionId { get; set; }
        public string ScreenCode { get; set; } = null!;
        public string ActionCode { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public bool IsAssigned { get; set; }
    }
}
