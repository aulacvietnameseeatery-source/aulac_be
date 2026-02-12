using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Role
{
    public class PermissionGroupDto
    {
        public string ScreenCode { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public List<PermissionItemDto> Permissions { get; set; } = new List<PermissionItemDto>();
    }
}
