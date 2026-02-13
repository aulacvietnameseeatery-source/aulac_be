using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Role
{
    public class RoleDetailDto
    {
        public long RoleId { get; set; }
        public string RoleCode { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public uint RoleStatusLvId { get; set; }
        public bool IsActive { get; set; }
        public List<PermissionGroupDto> PermissionGroups { get; set; } = new List<PermissionGroupDto>();
    }
}
