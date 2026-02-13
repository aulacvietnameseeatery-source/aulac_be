using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Role
{
    public class UpdateRoleRequestDto
    {
        /// <summary>
        /// The name of the role to update.
        /// </summary>
        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(100, ErrorMessage = "Role name must not exceed 100 characters.")]
        public string RoleName { get; set; } = null!;

        /// <summary>
        /// Indicates whether the role should be active.
        /// </summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>
        /// List of permission IDs to assign to the role.
        /// </summary>
        public List<long> PermissionIds { get; set; } = new List<long>();
    }
}
